using Lab01;

(string Name, Action Run)[] tests =
{
    ("Правила Copy и XOR для всех вчерашних комбинаций", TeacherRules),
    ("Случайное правило использует оба равновероятных исхода", RandomRule),
    ("XOR запрещает одинаковые A и Б", DifferentSubjects),
    ("Все пары посещены: 100 пирожков", AllAttended),
    ("Все безопасные пары пропущены: 700 очков", AllSkipped),
    ("Отчисление в первый день", FirstDayExpulsion),
    ("Отчисление обнуляет и прогулы, и пирожки", LaterExpulsion),
    ("Среднее считается за 100 дней без целочисленного деления", FractionalAverage),
    ("Опросы зависят от вчера, а не от предыдущей пары", PreviousDayOnly),
    ("Начальные вероятности и обучение на нулевой истории", BayesianWeights),
    ("Несовместимые правила исключаются отдельно для каждого предмета", RandomHypothesis),
    ("Стратегия изучает Copy и XOR на разнообразной истории", LearnRules),
    ("Накопление наблюдений позволяет начать прогулы", StartsSkipping),
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

static bool[] Bits(int mask) => Enumerable.Range(0, Semester.SubjectCount)
    .Select(bit => (mask & (1 << bit)) != 0).ToArray();

static Teacher[] SilentTeachers() => Enumerable.Range(0, Semester.SubjectCount)
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

static void TeacherRules()
{
    Random random = new Random(1);
    for (int mask = 0; mask < 64; mask++)
    {
        bool[] yesterday = Bits(mask);
        for (int a = 0; a < Semester.SubjectCount; a++)
        {
            Check(new Teacher(QuestionRule.Copy, a).WillAsk(yesterday, random) == yesterday[a]);
            for (int b = 0; b < Semester.SubjectCount; b++)
                if (a != b)
                    Check(new Teacher(QuestionRule.ExclusiveOr, a, b).WillAsk(yesterday, random)
                          == (yesterday[a] ^ yesterday[b]));
        }
    }
}

static void RandomRule()
{
    Teacher teacher = new Teacher(QuestionRule.Random);
    Random random = new ScriptedRandom(0, 1);
    Check(teacher.WillAsk(Bits(0), random));
    Check(!teacher.WillAsk(Bits(63), random));
}

static void DifferentSubjects()
{
    try
    {
        _ = new Teacher(QuestionRule.ExclusiveOr, 2, 2);
    }
    catch (ArgumentException)
    {
        return;
    }
    throw new InvalidOperationException("Одинаковые A и Б были приняты");
}

static void AllAttended()
{
    Teacher[] teachers = Enumerable.Range(0, 6)
        .Select(_ => new Teacher(QuestionRule.Random)).ToArray();
    SemesterResult result = new Semester(teachers, new Random(1)).Run(new TestStrategy(false));
    Check(!result.IsExpelled && result.CompletedDays == 100 && result.TotalPleasure == 100);
    Near(result.AveragePerDay, 1);
}

static void AllSkipped()
{
    SemesterResult result = new Semester(SilentTeachers(), new Random(1)).Run(new TestStrategy(true));
    Check(!result.IsExpelled && result.CompletedDays == 100 && result.TotalPleasure == 700);
    Near(result.AveragePerDay, 7);
}

static void FirstDayExpulsion()
{
    Teacher[] teachers = SilentTeachers();
    teachers[0] = new Teacher(QuestionRule.Random);
    SemesterResult result = new Semester(teachers, new ScriptedRandom(0)).Run(new TestStrategy(true));
    Check(result.IsExpelled && result.ExpelledDay == 1 && result.ExpelledSubject == 0);
    Check(result.CompletedDays == 0 && result.TotalPleasure == 0);
    Near(result.AveragePerDay, 0);
}

static void LaterExpulsion()
{
    Teacher[] teachers = SilentTeachers();
    teachers[0] = new Teacher(QuestionRule.Random);
    int observedDays = 0;
    TestStrategy strategy = new TestStrategy(true)
    {
        OnObservation = (_, _) => observedDays++
    };
    SemesterResult result = new Semester(teachers, new ScriptedRandom(1, 0)).Run(strategy);
    Check(result.IsExpelled && result.ExpelledDay == 2 && result.CompletedDays == 1);
    Check(result.TotalPleasure == 0 && observedDays == 1);
    Near(result.AveragePerDay, 0);
}

static void FractionalAverage()
{
    int decisions = 0;
    TestStrategy strategy = new TestStrategy(false)
    {
        Decide = (_, _, _) => ++decisions == 6
    };
    SemesterResult result = new Semester(SilentTeachers(), new Random(1)).Run(strategy);
    Check(result.TotalPleasure == 101 && decisions == 600);
    Near(result.AveragePerDay, 1.01);
}

static void PreviousDayOnly()
{
    Teacher[] teachers = SilentTeachers();
    teachers[0] = new Teacher(QuestionRule.Random);
    teachers[1] = new Teacher(QuestionRule.Copy, 0);
    teachers[2] = new Teacher(QuestionRule.ExclusiveOr, 0, 1);
    bool[] expectedYesterday = Bits(0);
    int observedDays = 0;
    TestStrategy strategy = new TestStrategy(false)
    {
        Decide = (_, yesterday, maximum) =>
        {
            Check(yesterday.SequenceEqual(expectedYesterday));
            Check(maximum >= 1 && maximum <= 700);
            return false;
        },
        OnObservation = (yesterday, today) =>
        {
            Check(yesterday.SequenceEqual(expectedYesterday));
            Check(today[1] == yesterday[0]);
            Check(today[2] == (yesterday[0] ^ yesterday[1]));
            expectedYesterday = today.ToArray();
            observedDays++;
        }
    };
    int[] alternating = Enumerable.Range(0, 100).Select(day => day % 2).ToArray();
    _ = new Semester(teachers, new ScriptedRandom(alternating)).Run(strategy);
    Check(observedDays == 100);
}

static void BayesianWeights()
{
    BayesianStrategy strategy = new BayesianStrategy();
    bool[] zeros = Bits(0);
    for (int subject = 0; subject < 6; subject++)
    {
        Near(strategy.GetQuestionProbability(subject, zeros), 1.0 / 6);
        Near(strategy.GetQuestionProbability(subject, Bits(63)), 0.5);
        Check(!strategy.ShouldSkip(subject, zeros, 700));
    }
    for (int observations = 1; observations <= 100; observations++)
    {
        strategy.ObserveDay(zeros, zeros);
        double expected = 0.5 / (1 + Math.Pow(2, observations + 1));
        double actual = strategy.GetQuestionProbability(0, zeros);
        Near(actual, expected, expected * 1e-12);
        Check(actual > 0, "Случайная гипотеза не должна исчезать");
    }
}

static void RandomHypothesis()
{
    BayesianStrategy strategy = new BayesianStrategy();
    strategy.ObserveDay(Bits(0), Bits(1));
    Near(strategy.GetQuestionProbability(0, Bits(0)), 0.5);
    Near(strategy.GetQuestionProbability(0, Bits(63)), 0.5);
    Near(strategy.GetQuestionProbability(1, Bits(0)), 0.1);
    Check(!strategy.ShouldSkip(0, Bits(0), 2)); // Равная нулю выгода: посетить.
}

static void LearnRules()
{
    BayesianStrategy strategy = new BayesianStrategy();
    for (int mask = 0; mask < 64; mask++)
    {
        bool[] yesterday = Bits(mask);
        bool[] today = Bits(0);
        today[0] = yesterday[2];
        today[1] = yesterday[1] ^ yesterday[5];
        strategy.ObserveDay(yesterday, today);
    }
    for (int mask = 0; mask < 64; mask++)
    {
        bool[] yesterday = Bits(mask);
        Near(strategy.GetQuestionProbability(0, yesterday), yesterday[2] ? 1 : 0);
        Near(strategy.GetQuestionProbability(1, yesterday), yesterday[1] ^ yesterday[5] ? 1 : 0);
    }
}

static void StartsSkipping()
{
    SemesterResult result = new Semester(SilentTeachers(), new Random(1)).Run(new BayesianStrategy());
    Check(!result.IsExpelled && result.TotalPleasure > 100 && result.TotalPleasure < 700);
}

static SemesterResult Simulate(int seed)
{
    Random random = new Random(seed);
    Teacher[] teachers = Enumerable.Range(0, 6).Select(_ => Teacher.CreateRandom(random)).ToArray();
    return new Semester(teachers, random).Run(new BayesianStrategy());
}

static void Reproducibility()
{
    Check(Simulate(42) == Simulate(42));
}

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
            Check(result.ExpelledSubject >= 0 && result.ExpelledSubject < 6);
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

sealed class TestStrategy(bool skip) : IStudentStrategy
{
    public Func<int, IReadOnlyList<bool>, int, bool>? Decide { get; init; }
    public Action<IReadOnlyList<bool>, IReadOnlyList<bool>>? OnObservation { get; init; }

    public bool ShouldSkip(int subject, IReadOnlyList<bool> yesterday, int maximumFinalPleasure)
        => Decide?.Invoke(subject, yesterday, maximumFinalPleasure) ?? skip;

    public void ObserveDay(IReadOnlyList<bool> yesterday, IReadOnlyList<bool> today)
        => OnObservation?.Invoke(yesterday, today);
}

sealed class ScriptedRandom(params int[] values) : Random
{
    private int index;

    public override int Next(int maxValue)
    {
        if (maxValue != 2 || index >= values.Length)
            throw new InvalidOperationException("Неожиданный запрос к генератору случайных чисел");
        return values[index++];
    }
}
