using System.Text.Json;
using Lab01;
using SharpLabs.Core;

(string Name, Action Run)[] tests =
{
    ("Названия и значения Subject согласованы", SubjectOrder),
    ("Правила Copy и XOR для всех вчерашних комбинаций", TeacherRules),
    ("Случайное правило использует оба исхода", RandomRule),
    ("XOR запрещает одинаковые A и Б", DifferentSubjects),
    ("Настройки загружаются из appsettings.json", LoadSettings),
    ("Некорректные настройки отклоняются", InvalidSettings),
    ("Все пары посещены: начисляются только пирожки", AllAttended),
    ("Все безопасные пары пропущены: максимальное удовольствие", AllSkipped),
    ("Настроенные длительность и награды используются в расчётах", CustomSettings),
    ("Отчисление в первый день", FirstDayExpulsion),
    ("Отчисление обнуляет и прогулы, и пирожки", LaterExpulsion),
    ("Среднее делится на настроенное число дней без округления до целого", FractionalAverage),
    ("Решение вызывается один раз в день до генерации опросов", DailyDecision),
    ("История различает прогул, отсутствие опроса и неизвестное будущее", HistoryVisibility),
    ("Опросы зависят от вчера, включая пропущенные пары", PreviousDayOnly),
    ("Стратегия должна вернуть решение для каждого предмета", InvalidDecision),
    ("В начале семестра стратегия посещает все пары", FirstDecision),
    ("Наблюдения позволяют начать прогулы", StartsSkipping),
    ("Неизвестное вчера не подменяется отсутствием опроса", UnknownYesterday),
    ("Неизвестные результаты не используются для обучения", UnknownObservations),
    ("Случайный преподаватель остаётся непредсказуемым", LearnRandom),
    ("Стратегия изучает Copy и XOR из истории", LearnRules),
    ("Длинная история не исключает случайную гипотезу из-за округления", LongHistory),
    ("Новый семестр сбрасывает обучение стратегии", StrategyReuse),
    ("Одинаковый seed воспроизводит результат", Reproducibility),
    ("Результаты 1000 семестров находятся в допустимых границах", SemesterBounds)
};

int failures = 0;
foreach ((string name, Action run) in tests)
{
    try
    {
        run();
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

static bool[] Attendance(bool attend) => Enumerable.Repeat(attend, Subjects.Names.Count).ToArray();

static bool[] Bits(int mask) => Enumerable.Range(0, Subjects.Names.Count)
    .Select(bit => (mask & (1 << bit)) != 0).ToArray();

static Teacher[] SilentTeachers() => Enum.GetValues<Subject>()
    .Select(subject => new Teacher(QuestionRule.Copy, subject)).ToArray();

static void Check(bool condition, string message = "Условие не выполнено")
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void Near(double actual, double expected, double tolerance = 1e-12)
{
    Check(double.IsFinite(actual) && Math.Abs(actual - expected) <= tolerance,
        $"Ожидалось {expected}, получено {actual}");
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Ожидалось исключение {typeof(T).Name}");
}

static void SubjectOrder()
{
    Subject[] subjects = Enum.GetValues<Subject>();
    Check(subjects.Length == Subjects.Names.Count);
    Check(subjects.Select(subject => (int)subject).SequenceEqual(Enumerable.Range(0, subjects.Length)));
    Check(Subjects.Names[(int)Subject.Calculus] == "Матанализ");
    Check(Subjects.Names[(int)Subject.English] == "Английский язык");
}

static void TeacherRules()
{
    Random random = new Random(1);
    for (int mask = 0; mask < 1 << Subjects.Names.Count; mask++)
    {
        bool[] yesterday = Bits(mask);
        foreach (Subject a in Enum.GetValues<Subject>())
        {
            Check(new Teacher(QuestionRule.Copy, a).WillAsk(yesterday, random) == yesterday[(int)a]);
            foreach (Subject b in Enum.GetValues<Subject>())
                if (a != b)
                    Check(new Teacher(QuestionRule.ExclusiveOr, a, b).WillAsk(yesterday, random)
                          == (yesterday[(int)a] ^ yesterday[(int)b]));
        }
    }
}

static void RandomRule()
{
    Teacher teacher = new Teacher(QuestionRule.Random);
    Random random = new ScriptedRandom(0, 1);
    Check(teacher.WillAsk(Bits(0), random));
    Check(!teacher.WillAsk(Attendance(true), random));
}

static void DifferentSubjects() => Throws<ArgumentException>(() =>
    _ = new Teacher(QuestionRule.ExclusiveOr, Subject.Physics, Subject.Physics));

static void LoadSettings()
{
    SimulationSettings settings = SimulationSettings.Load(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
    Check(settings.DayCount == 100 && settings.SkipPleasure == 1 && settings.PiePleasure == 1);
    Check(settings.Seed is null);

    string path = Path.GetTempFileName();
    try
    {
        File.WriteAllText(path, """{"DayCount":7,"SkipPleasure":3,"PiePleasure":2,"Seed":42}""");
        settings = SimulationSettings.Load(path);
        Check(settings.DayCount == 7 && settings.SkipPleasure == 3 && settings.PiePleasure == 2);
        Check(settings.Seed == 42);
    }
    finally { File.Delete(path); }
}

static void InvalidSettings()
{
    foreach (SimulationSettings settings in new[]
    {
        TestData.Settings with { DayCount = 0 },
        TestData.Settings with { DayCount = -1 },
        TestData.Settings with { SkipPleasure = 0 },
        TestData.Settings with { PiePleasure = -1 },
        TestData.Settings with { DayCount = int.MaxValue },
        TestData.Settings with { SkipPleasure = int.MaxValue }
    })
        Throws<ArgumentException>(settings.Validate);

    string path = Path.GetTempFileName();
    try
    {
        foreach (string json in new[]
        {
            "null", "{",
            """{"DayCount":100}""",
            """{"DayCount":100,"SkipPleasure":1,"PiePleasure":1,"DayCont":100}"""
        })
        {
            File.WriteAllText(path, json);
            Throws<JsonException>(() => SimulationSettings.Load(path));
        }
    }
    finally { File.Delete(path); }
}

static void AllAttended()
{
    Teacher[] teachers = Enum.GetValues<Subject>().Select(_ => new Teacher(QuestionRule.Random)).ToArray();
    SemesterResult result = new Semester(teachers, new Random(1), TestData.Settings).Run(new TestStrategy(true));
    Check(!result.IsExpelled && result.CompletedDays == 100 && result.TotalPleasure == 100);
    Near(result.AveragePerDay, 1);
}

static void AllSkipped()
{
    SemesterResult result = new Semester(SilentTeachers(), new Random(1), TestData.Settings)
        .Run(new TestStrategy(false));
    Check(!result.IsExpelled && result.CompletedDays == 100 && result.TotalPleasure == 700);
    Near(result.AveragePerDay, 7);
}

static void CustomSettings()
{
    SimulationSettings settings = TestData.Settings with { DayCount = 3, SkipPleasure = 2, PiePleasure = 5 };
    SemesterResult result = new Semester(SilentTeachers(), new Random(1), settings).Run(new TestStrategy(false));
    Check(result.CompletedDays == 3 && result.TotalDays == 3 && result.TotalPleasure == 51);
    Near(result.AveragePerDay, 17);
    result = new Semester(SilentTeachers(), new Random(1), settings).Run(new TestStrategy(true));
    Check(result.TotalPleasure == 15);
}

static void FirstDayExpulsion()
{
    Teacher[] teachers = SilentTeachers();
    teachers[0] = new Teacher(QuestionRule.Random);
    TestStrategy strategy = new TestStrategy(false);
    SemesterResult result = new Semester(teachers, new ScriptedRandom(0), TestData.Settings).Run(strategy);
    Check(result.IsExpelled && result.ExpelledDay == 1 && result.ExpelledSubject == Subject.Calculus);
    Check(result.CompletedDays == 0 && result.TotalPleasure == 0 && strategy.Calls == 1);
    Near(result.AveragePerDay, 0);
}

static void LaterExpulsion()
{
    Teacher[] teachers = SilentTeachers();
    teachers[0] = new Teacher(QuestionRule.Random);
    TestStrategy strategy = new TestStrategy(false);
    SemesterResult result = new Semester(teachers, new ScriptedRandom(1, 0), TestData.Settings).Run(strategy);
    Check(result.IsExpelled && result.ExpelledDay == 2 && result.CompletedDays == 1);
    Check(result.TotalPleasure == 0 && strategy.Calls == 2);
    Near(result.AveragePerDay, 0);
}

static void FractionalAverage()
{
    SimulationSettings settings = TestData.Settings with { DayCount = 7 };
    TestStrategy strategy = new TestStrategy(true)
    {
        Decide = (day, _) =>
        {
            bool[] attendance = Attendance(true);
            attendance[0] = day != 1;
            return attendance;
        }
    };
    SemesterResult result = new Semester(SilentTeachers(), new Random(1), settings).Run(strategy);
    Check(result.TotalPleasure == 8 && strategy.Calls == 7);
    Near(result.AveragePerDay, 8.0 / 7);
}

static void DailyDecision()
{
    SimulationSettings settings = TestData.Settings with { DayCount = 4 };
    Teacher[] teachers = SilentTeachers();
    teachers[0] = new Teacher(QuestionRule.Random);
    ScriptedRandom random = new ScriptedRandom(0, 1, 0, 1);
    TestStrategy strategy = new TestStrategy(true)
    {
        Decide = (day, history) =>
        {
            Check(random.Calls == day - 1, "Опросы должны генерироваться после решения стратегии");
            foreach (Subject subject in Enum.GetValues<Subject>())
            {
                Check(!history.Attended(day, subject));
                Check(history.WasAsked(day, subject) is null);
            }
            return Attendance(true);
        }
    };
    _ = new Semester(teachers, random, settings).Run(strategy);
    Check(strategy.Calls == settings.DayCount && random.Calls == settings.DayCount);
}

static void HistoryVisibility()
{
    IReadOnlyStudentHistory? firstSnapshot = null;
    IReadOnlyStudentHistory? secondSnapshot = null;
    bool[] decision = Attendance(true);
    decision[(int)Subject.English] = false;
    TestStrategy strategy = new TestStrategy(true)
    {
        Decide = (day, history) =>
        {
            if (day == 1)
            {
                firstSnapshot = history;
                return decision;
            }

            // Изменение ранее возвращённого массива не должно изменять историю.
            decision[(int)Subject.English] = true;
            secondSnapshot = history;
            Check(history.Attended(1, Subject.Calculus));
            Check(history.WasAsked(1, Subject.Calculus) == false);
            Check(!history.Attended(1, Subject.English));
            Check(history.WasAsked(1, Subject.English) is null);
            Check(!history.Attended(day, Subject.Calculus));
            Check(history.WasAsked(day, Subject.Calculus) is null);
            Check(history.WasAsked(day + 1, Subject.Calculus) is null);
            return Attendance(true);
        }
    };
    _ = new Semester(SilentTeachers(), new Random(1), TestData.Settings with { DayCount = 2 }).Run(strategy);
    Check(firstSnapshot!.WasAsked(1, Subject.Calculus) is null);
    Check(!firstSnapshot.Attended(1, Subject.Calculus));
    Check(secondSnapshot!.WasAsked(2, Subject.Calculus) is null);
    Throws<ArgumentOutOfRangeException>(() => firstSnapshot.WasAsked(0, Subject.Calculus));
}

static void PreviousDayOnly()
{
    Teacher[] teachers = SilentTeachers();
    teachers[0] = new Teacher(QuestionRule.Random);
    teachers[1] = new Teacher(QuestionRule.Copy, Subject.Calculus);
    teachers[2] = new Teacher(QuestionRule.ExclusiveOr, Subject.Calculus, Subject.LinearAlgebra);
    TestStrategy strategy = new TestStrategy(true)
    {
        Decide = (day, history) =>
        {
            bool[] attendance = Attendance(true);
            if (day == 1)
                attendance[(int)Subject.LinearAlgebra] = false; // Вчера все false: безопасно.
            if (day == 2)
            {
                Check(history.WasAsked(1, Subject.Calculus) == true);
                Check(history.WasAsked(1, Subject.LinearAlgebra) is null);
                Check(history.WasAsked(1, Subject.DiscreteMath) == false);
            }
            if (day == 3)
            {
                // Для XOR использован реальный false пропущенной пары первого дня.
                Check(history.WasAsked(2, Subject.LinearAlgebra) == true);
                Check(history.WasAsked(2, Subject.DiscreteMath) == true);
            }
            return attendance;
        }
    };
    SemesterResult result = new Semester(teachers, new ScriptedRandom(0, 1, 0),
        TestData.Settings with { DayCount = 3 }).Run(strategy);
    Check(!result.IsExpelled && strategy.Calls == 3);
}

static void InvalidDecision()
{
    foreach (bool[]? decision in new bool[]?[] { null, Array.Empty<bool>(), new bool[Subjects.Names.Count + 1] })
    {
        TestStrategy strategy = new TestStrategy(true) { Decide = (_, _) => decision! };
        Throws<InvalidOperationException>(() =>
            new Semester(SilentTeachers(), new Random(1), TestData.Settings).Run(strategy));
    }
}

static TestHistory SilentHistory(int days)
{
    TestHistory history = new TestHistory();
    for (int day = 0; day < days; day++)
        history.Add(Attendance(true), Bits(0).Select(value => (bool?)value).ToArray());
    return history;
}

static void FirstDecision()
{
    ISkipStrategy strategy = new BayesianStrategy(TestData.Settings);
    Check(!string.IsNullOrWhiteSpace(strategy.Name));
    Check(strategy.DecideDay(1, new TestHistory()).All(attend => attend));
}

static void StartsSkipping()
{
    BayesianStrategy strategy = new BayesianStrategy(TestData.Settings);
    Check(strategy.DecideDay(21, SilentHistory(20)).All(attend => !attend));
    SemesterResult result = new Semester(SilentTeachers(), new Random(1), TestData.Settings).Run(strategy);
    Check(!result.IsExpelled && result.TotalPleasure > 100 && result.TotalPleasure < 700);
}

static void UnknownYesterday()
{
    TestHistory history = SilentHistory(20);
    history.Add(Attendance(false), new bool?[Subjects.Names.Count]);
    bool[] decision = new BayesianStrategy(TestData.Settings).DecideDay(22, history);
    Check(decision.All(attend => attend), "Неизвестные входы нельзя считать false");
}

static void UnknownObservations()
{
    TestHistory history = new TestHistory();
    for (int day = 1; day <= 20; day++)
        history.Add(Attendance(false), new bool?[Subjects.Names.Count]);
    history.Add(Attendance(true), Bits(0).Select(value => (bool?)value).ToArray());
    Check(new BayesianStrategy(TestData.Settings).DecideDay(22, history).All(attend => attend));
}

static void LearnRandom()
{
    TestHistory history = new TestHistory();
    history.Add(Attendance(true), Bits(1).Select(value => (bool?)value).ToArray());
    for (int day = 2; day <= 20; day++)
        history.Add(Attendance(true), Bits(0).Select(value => (bool?)value).ToArray());
    bool[] decision = new BayesianStrategy(TestData.Settings).DecideDay(21, history);
    Check(decision[(int)Subject.Calculus], "Опрос в первый день исключает все детерминированные правила");
    Check(decision.Skip(1).Any(attend => !attend));
}

static void LearnRules()
{
    for (int lastMask = 0; lastMask < 16; lastMask++)
    {
        TestHistory history = new TestHistory();
        Random random = new Random(15);
        bool[] previous = Bits(0);
        for (int day = 1; day <= 80; day++)
        {
            bool[] today = Bits(0);
            for (int subject = 2; subject < today.Length; subject++)
                today[subject] = day == 80 ? (lastMask & (1 << (subject - 2))) != 0 : random.Next(2) == 0;
            today[0] = previous[2];
            today[1] = previous[3] ^ previous[5];
            history.Add(Attendance(true), today.Select(value => (bool?)value).ToArray());
            previous = today;
        }
        bool[] decision = new BayesianStrategy(TestData.Settings).DecideDay(81, history);
        Check(decision[0] == previous[2], "Не изучено правило Copy");
        Check(decision[1] == (previous[3] ^ previous[5]), "Не изучено правило XOR");
    }
}

static void StrategyReuse()
{
    BayesianStrategy strategy = new BayesianStrategy(TestData.Settings);
    SemesterResult first = new Semester(SilentTeachers(), new Random(1), TestData.Settings).Run(strategy);
    SemesterResult second = new Semester(SilentTeachers(), new Random(1), TestData.Settings).Run(strategy);
    Check(first == second);
}

static void LongHistory()
{
    SimulationSettings settings = TestData.Settings with { DayCount = 1210 };
    TestHistory history = SilentHistory(1200);
    history.Add(Attendance(true), Bits(1).Select(value => (bool?)value).ToArray());
    bool[] decision = new BayesianStrategy(settings).DecideDay(1202, history);
    Check(decision[(int)Subject.Calculus], "Случайная гипотеза должна пережить длинную серию без опросов");
}

static SemesterResult Simulate(int seed)
{
    Random random = new Random(seed);
    Teacher[] teachers = Enum.GetValues<Subject>().Select(_ => Teacher.CreateRandom(random)).ToArray();
    return new Semester(teachers, random, TestData.Settings).Run(new BayesianStrategy(TestData.Settings));
}

static void Reproducibility() => Check(Simulate(42) == Simulate(42));

static void SemesterBounds()
{
    int expelled = 0;
    long total = 0;
    for (int seed = 0; seed < 1000; seed++)
    {
        SemesterResult result = Simulate(seed);
        Check(result.TotalPleasure >= 0 && result.TotalPleasure <= 700);
        Check(result.CompletedDays >= 0 && result.CompletedDays <= 100);
        if (result.IsExpelled)
        {
            Check(result.TotalPleasure == 0 && result.ExpelledDay == result.CompletedDays + 1);
            Check(result.ExpelledSubject.HasValue && Enum.IsDefined(result.ExpelledSubject.Value));
            expelled++;
        }
        else
        {
            Check(result.CompletedDays == 100 && result.TotalPleasure >= 100);
        }
        total += result.TotalPleasure;
    }
    Console.WriteLine($"  Seeds 0..999: среднее удовольствие {(double)total / 1000:F2}, " +
                      $"отчислений {expelled}/1000 (описательная статистика).");
}

static class TestData
{
    public static SimulationSettings Settings { get; } = new SimulationSettings
    {
        DayCount = 100, SkipPleasure = 1, PiePleasure = 1
    };
}

sealed class TestStrategy(bool attend) : ISkipStrategy
{
    public string Name => "Тестовая стратегия";
    public int Calls { get; private set; }
    public Func<int, IReadOnlyStudentHistory, bool[]>? Decide { get; init; }

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        Calls++;
        return Decide is null ? Enumerable.Repeat(attend, Subjects.Names.Count).ToArray()
            : Decide(day, history);
    }
}

sealed class TestHistory : IReadOnlyStudentHistory
{
    private readonly List<(bool[] Attendance, bool?[] Asked)> days = new();

    public void Add(bool[] attendance, bool?[] asked) => days.Add((attendance, asked));
    public bool Attended(int day, Subject subject) => days[day - 1].Attendance[(int)subject];
    public bool? WasAsked(int day, Subject subject) => days[day - 1].Asked[(int)subject];
}

sealed class ScriptedRandom(params int[] values) : Random
{
    public int Calls { get; private set; }

    public override int Next(int maxValue)
    {
        if (maxValue != 2 || Calls >= values.Length)
            throw new InvalidOperationException("Неожиданный запрос к генератору случайных чисел");
        return values[Calls++];
    }
}
