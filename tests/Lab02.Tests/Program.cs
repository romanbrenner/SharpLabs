using System.Globalization;
using System.Threading.Channels;
using Lab02;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpLabs.Core;

(string Name, Func<Task> Run)[] tests =
{
    ("Бесконечная симуляция и стратегия проходят 250 дней без сброса", () => Sync(ContinuousSimulation)),
    ("Среднее считается по прошедшим дням", () => Sync(RunningAverage)),
    ("При отчислении очки обнуляются и дальнейшие дни запрещены", () => Sync(Expulsion)),
    ("Накопленное удовольствие поддерживает значения больше int", () => Sync(LargeScore)),
    ("Снимок истории скрывает будущие дни и пропуски", () => Sync(HistorySnapshot)),
    ("Настройки по умолчанию и сервисы DI согласованы", () => Sync(DefaultsAndDi)),
    ("Некорректные настройки останавливают запуск хоста", InvalidOptions),
    ("Один сигнал таймера — один день; после 100 дней хост продолжает работу", ScheduledDays),
    ("При отчислении хост завершается без ввода с клавиатуры", ExpulsionStopsHost),
    ("Нажатие клавиши прерывает ожидание следующего дня", KeyStopsHost),
    ("Остановка хоста отменяет ожидание таймера", StopDuringWait),
    ("Перенаправленный stdin не вызывает чтение клавиатуры", RedirectedInput),
    ("Ожидание настоящего таймера поддерживает отмену", CancelRealTimer),
    ("В отчёте после отчисления выводятся нулевые показатели", () => Sync(ReportOutput))
};

int failures = 0;
foreach ((string name, Func<Task> run) in tests)
{
    try
    {
        await run();
        Console.WriteLine($"OK: {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL: {name}: {exception.Message}");
    }
}
Console.WriteLine($"Успешно: {tests.Length - failures}/{tests.Length}");
return failures == 0 ? 0 : 1;

static Task Sync(Action action) { action(); return Task.CompletedTask; }

static void Check(bool condition, string message = "Условие не выполнено")
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Near(double actual, double expected) => Check(Math.Abs(actual - expected) < 1e-10);

static bool[] All(bool value) => Enumerable.Repeat(value, Subjects.Names.Count).ToArray();

static SimulationSettings Settings() => new() { DayCount = 100, SkipPleasure = 1, PiePleasure = 1 };

static TeacherService SilentTeachers() => new(Enum.GetValues<Subject>()
    .Select(subject => new Teacher(QuestionRule.Copy, subject)).ToArray(), new Random(1));

static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Ожидалось исключение {typeof(T).Name}");
}

static void ContinuousSimulation()
{
    SimulationSettings settings = Settings();
    IDaySimulation simulation = new DaySimulation(SilentTeachers(), new StudentHistoryStore(),
        new BayesianStrategy(settings, continuous: true), settings);
    long previous = 0;
    for (int day = 1; day <= 250; day++)
    {
        DayResult result = simulation.NextDay();
        Check(result.Day == day && !result.IsExpelled && result.TotalPleasure > previous);
        Near(result.AveragePerDay, (double)result.TotalPleasure / day);
        previous = result.TotalPleasure;
    }
}

static void RunningAverage()
{
    FixedStrategy strategy = new((day, _) => All(day != 1));
    IDaySimulation simulation = new DaySimulation(SilentTeachers(), new StudentHistoryStore(), strategy, Settings());
    DayResult first = simulation.NextDay();
    Check(first.TotalPleasure == 7);
    Near(first.AveragePerDay, 7);
    DayResult second = simulation.NextDay();
    Check(second.TotalPleasure == 8);
    Near(second.AveragePerDay, 4);
}

static void Expulsion()
{
    bool[] asked = All(false);
    asked[(int)Subject.Physics] = true;
    IDaySimulation simulation = new DaySimulation(new ScriptedTeachers(All(false), asked),
        new StudentHistoryStore(), new FixedStrategy((_, _) => All(false)), Settings());
    Check(simulation.NextDay().TotalPleasure == 7);
    DayResult result = simulation.NextDay();
    Check(result.IsExpelled && result.ExpelledSubject == Subject.Physics && result.TotalPleasure == 0);
    Near(result.AveragePerDay, 0);
    try { simulation.NextDay(); }
    catch (InvalidOperationException) { return; }
    throw new InvalidOperationException("Симуляция продолжилась после отчисления");
}

static void LargeScore()
{
    SimulationSettings settings = Settings() with { DayCount = 1, PiePleasure = int.MaxValue - Subjects.Names.Count };
    IDaySimulation simulation = new DaySimulation(SilentTeachers(), new StudentHistoryStore(),
        new FixedStrategy((_, _) => All(true)), settings);
    simulation.NextDay();
    DayResult result = simulation.NextDay();
    Check(result.TotalPleasure == 2L * settings.PiePleasure && result.TotalPleasure > int.MaxValue);
}

static void HistorySnapshot()
{
    IStudentHistoryStore store = new StudentHistoryStore();
    IReadOnlyStudentHistory before = store.Snapshot();
    bool[] attendance = All(true);
    attendance[(int)Subject.English] = false;
    store.AddDay(attendance, All(false));
    attendance[(int)Subject.English] = true;
    IReadOnlyStudentHistory after = store.Snapshot();
    Check(!before.Attended(1, Subject.Calculus) && before.WasAsked(1, Subject.Calculus) is null);
    Check(after.Attended(1, Subject.Calculus) && after.WasAsked(1, Subject.Calculus) == false);
    Check(!after.Attended(1, Subject.English) && after.WasAsked(1, Subject.English) is null);
    store.AddDay(All(true), All(true));
    Check(after.WasAsked(2, Subject.Calculus) is null);
}

static void DefaultsAndDi()
{
    IConfiguration configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json").Build();
    Lab02Options options = configuration.GetRequiredSection("Simulation").Get<Lab02Options>()!;
    Check(options.IsValid() && options.DayIntervalSeconds == 5 && options.PlanningDays == 100);
    Check(options.SkipPleasure == 1 && options.PiePleasure == 1 && options.Seed is null);

    HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
    builder.Services.AddSimulationServices(configuration);
    using IHost host = builder.Build();
    IServiceProvider services = host.Services;
    Check(services.GetRequiredService<ISkipStrategy>() is BayesianStrategy);
    Check(ReferenceEquals(services.GetRequiredService<IDaySimulation>(), services.GetRequiredService<IDaySimulation>()));
    Check(ReferenceEquals(services.GetRequiredService<IStudentHistoryStore>(), services.GetRequiredService<IStudentHistoryStore>()));
    Check(ReferenceEquals(services.GetRequiredService<ITeacherService>(), services.GetRequiredService<ITeacherService>()));
}

static async Task InvalidOptions()
{
    foreach ((string key, string value) in new[]
    {
        ("DayIntervalSeconds", "0"), ("DayIntervalSeconds", "NaN"),
        ("KeyboardPollMilliseconds", "0"), ("PlanningDays", "0"), ("SkipPleasure", "-1")
    })
    {
        using IHost host = BuildHost(new ManualClock(), new FakeKeyInput(false), new CaptureReporter(),
            overrides: new Dictionary<string, string?> { ["Simulation:" + key] = value });
        await ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }
}

static async Task ScheduledDays()
{
    await WithHost(async run =>
    {
        await run.Clock.WaitUntilWaitingAsync();
        Check(run.Reporter.Count == 0, "День не должен начинаться до сигнала таймера");
        for (int day = 1; day <= 101; day++)
        {
            run.Clock.Tick();
            DayResult result = await run.Reporter.NextAsync();
            Check(result.Day == day && !result.IsExpelled);
            await run.Clock.WaitUntilWaitingAsync();
        }
        Check(!run.Shutdown.IsCompleted, "После сотого дня программа не должна завершаться");
        Check(run.Reporter.Count == 101);
    });
}

static async Task ExpulsionStopsHost()
{
    await WithHost(async run =>
    {
        await run.Clock.WaitUntilWaitingAsync();
        run.Clock.Tick();
        DayResult result = await run.Reporter.NextAsync();
        Check(result.IsExpelled && result.TotalPleasure == 0 && result.AveragePerDay == 0);
        await run.Shutdown.WaitAsync(TimeSpan.FromSeconds(5));
        Check(run.Reporter.Count == 1 && run.Reporter.StopCount == 1);
        Check(run.Clock.WaitCount == 1, "После отчисления нельзя ожидать следующий день");
        Check(run.Host.Services.GetServices<IHostedService>().OfType<BackgroundService>()
            .All(service => service.ExecuteTask!.IsCompleted));
    }, teachers: new ScriptedTeachers(All(true)), strategy: new FixedStrategy((_, _) => All(false)),
        interactive: true);
}

static async Task KeyStopsHost()
{
    await WithHost(async run =>
    {
        await run.Clock.WaitUntilWaitingAsync();
        run.Input.Press();
        await run.Shutdown.WaitAsync(TimeSpan.FromSeconds(5));
        Check(run.Reporter.Count == 0 && run.Reporter.StopCount == 1);
    }, interactive: true);
}

static async Task StopDuringWait()
{
    await WithHost(async run =>
    {
        await run.Clock.WaitUntilWaitingAsync();
        run.Host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        await run.Shutdown.WaitAsync(TimeSpan.FromSeconds(5));
        Check(run.Reporter.Count == 0 && run.Reporter.StopCount == 1);
    });
}

static async Task RedirectedInput()
{
    await WithHost(async run =>
    {
        await run.Clock.WaitUntilWaitingAsync();
        run.Clock.Tick();
        await run.Reporter.NextAsync();
        await run.Clock.WaitUntilWaitingAsync();
        Check(!run.Shutdown.IsCompleted && run.Input.ReadCount == 0);
    }, interactive: false);
}

static async Task CancelRealTimer()
{
    using PeriodicDayClock clock = new(Options.Create(new Lab02Options { DayIntervalSeconds = 5 }));
    using CancellationTokenSource cancellation = new();
    Task<bool> wait = clock.WaitForNextDayAsync(cancellation.Token).AsTask();
    Check(!wait.IsCompleted);
    cancellation.Cancel();
    await ThrowsAsync<OperationCanceledException>(async () => await wait.WaitAsync(TimeSpan.FromSeconds(2)));
}

static void ReportOutput()
{
    using StringWriter output = new(CultureInfo.InvariantCulture);
    IOptions<Lab02Options> options = Options.Create(new Lab02Options { DayIntervalSeconds = 5, Seed = 42 });
    ConsoleProgressReporter reporter = new(output, new FixedStrategy((_, _) => All(true)),
        new SeededRandom(options), new FakeKeyInput(true), options);
    reporter.Started();
    reporter.WriteDay(new DayResult(2, 7, null));
    reporter.WriteDay(new DayResult(3, 0, Subject.Physics));
    reporter.Stopped();
    string text = output.ToString();
    Check(text.Contains("42") && text.Contains("3,50") && text.Contains("любую клавишу"));
    Check(text.Contains("День 3. Общее удовольствие: 0. Среднее удовольствие в день: 0,00."));
    Check(text.Contains("Студент отчислен: Физика."));
}

static IHost BuildHost(ManualClock clock, FakeKeyInput input, CaptureReporter reporter,
    ITeacherService? teachers = null, ISkipStrategy? strategy = null,
    Dictionary<string, string?>? overrides = null)
{
    Dictionary<string, string?> settings = new()
    {
        ["Simulation:DayIntervalSeconds"] = "5",
        ["Simulation:KeyboardPollMilliseconds"] = "5",
        ["Simulation:PlanningDays"] = "100",
        ["Simulation:SkipPleasure"] = "1",
        ["Simulation:PiePleasure"] = "1",
        ["Simulation:Seed"] = "42"
    };
    if (overrides is not null)
        foreach (var entry in overrides) settings[entry.Key] = entry.Value;

    HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
    builder.Configuration.AddInMemoryCollection(settings);
    builder.Logging.ClearProviders();
    builder.Services.AddSimulationServices(builder.Configuration);
    builder.Services.AddSingleton<IDayClock>(clock);
    builder.Services.AddSingleton<IKeyInput>(input);
    builder.Services.AddSingleton<IProgressReporter>(reporter);
    builder.Services.AddSingleton<ITeacherService>(teachers ?? SilentTeachers());
    if (strategy is not null) builder.Services.AddSingleton(strategy);
    return builder.Build();
}

static async Task WithHost(Func<TestRun, Task> test, ITeacherService? teachers = null,
    ISkipStrategy? strategy = null, bool interactive = false)
{
    ManualClock clock = new();
    FakeKeyInput input = new(interactive);
    CaptureReporter reporter = new();
    using IHost host = BuildHost(clock, input, reporter, teachers, strategy);
    await host.StartAsync();
    Task shutdown = host.WaitForShutdownAsync();
    try
    {
        await test(new TestRun(host, clock, input, reporter, shutdown));
    }
    finally
    {
        host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        await shutdown.WaitAsync(TimeSpan.FromSeconds(5));
    }
}

sealed record TestRun(IHost Host, ManualClock Clock, FakeKeyInput Input, CaptureReporter Reporter, Task Shutdown);

sealed class FixedStrategy(Func<int, IReadOnlyStudentHistory, bool[]> decide) : ISkipStrategy
{
    public string Name => "Тестовая стратегия";
    public bool[] DecideDay(int day, IReadOnlyStudentHistory history) => decide(day, history);
}

sealed class ScriptedTeachers(params bool[][] results) : ITeacherService
{
    private int index;
    public IReadOnlyList<bool> AskToday() => results[index++];
}

sealed class ManualClock : IDayClock
{
    private readonly Channel<bool> ticks = Channel.CreateUnbounded<bool>();
    private readonly Channel<bool> waits = Channel.CreateUnbounded<bool>();
    private int waitCount;
    public int WaitCount => Volatile.Read(ref waitCount);
    public void Tick() => ticks.Writer.TryWrite(true);

    public async ValueTask<bool> WaitForNextDayAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref waitCount);
        waits.Writer.TryWrite(true);
        return await ticks.Reader.ReadAsync(cancellationToken);
    }

    public Task<bool> WaitUntilWaitingAsync() => waits.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
}

sealed class FakeKeyInput(bool interactive) : IKeyInput
{
    private int pressed;
    private int readCount;
    public bool IsInteractive => interactive;
    public int ReadCount => Volatile.Read(ref readCount);
    public void Press() => Interlocked.Exchange(ref pressed, 1);
    public bool TryReadKey()
    {
        Interlocked.Increment(ref readCount);
        if (!interactive) throw new InvalidOperationException("Нельзя читать перенаправленный ввод как клавиатуру");
        return Interlocked.Exchange(ref pressed, 0) == 1;
    }
}

sealed class CaptureReporter : IProgressReporter
{
    private readonly Channel<DayResult> results = Channel.CreateUnbounded<DayResult>();
    private int count;
    private int stopCount;
    public int Count => Volatile.Read(ref count);
    public int StopCount => Volatile.Read(ref stopCount);
    public void Started() { }
    public void WriteDay(DayResult result)
    {
        Interlocked.Increment(ref count);
        results.Writer.TryWrite(result);
    }
    public void Stopped() => Interlocked.Increment(ref stopCount);
    public Task<DayResult> NextAsync() => results.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
}
