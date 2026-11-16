namespace Lab01;

public sealed class Semester
{
    private readonly Teacher[] teachers;
    private readonly Random random;
    private readonly SimulationSettings settings;

    public Semester(IReadOnlyList<Teacher> teachers, Random random, SimulationSettings settings)
    {
        settings.Validate();
        if (teachers.Count != Subjects.Names.Count)
            throw new ArgumentException("Количество преподавателей должно совпадать с количеством предметов.");

        this.teachers = teachers.ToArray();
        this.random = random;
        this.settings = settings;
    }

    public SemesterResult Run(ISkipStrategy strategy)
    {
        bool[] yesterday = new bool[Subjects.Names.Count];
        List<DayObservation> observations = new List<DayObservation>();
        int pleasure = 0;

        for (int day = 1; day <= settings.DayCount; day++)
        {
            bool[] decision = strategy.DecideDay(day, new StudentHistory(observations));
            if (decision is null || decision.Length != Subjects.Names.Count)
                throw new InvalidOperationException("Стратегия должна вернуть решение для каждого предмета.");

            // Копия защищает принятое решение от последующего изменения массива стратегией.
            bool[] attendance = decision.ToArray();
            bool[] today = new bool[Subjects.Names.Count];
            for (int subject = 0; subject < today.Length; subject++)
                today[subject] = teachers[subject].WillAsk(yesterday, random);

            for (int subject = 0; subject < today.Length; subject++)
            {
                if (!attendance[subject])
                {
                    if (today[subject])
                        return new SemesterResult(0, day - 1, settings.DayCount, day, (Subject)subject);

                    pleasure += settings.SkipPleasure;
                }
            }

            pleasure += settings.PiePleasure;
            bool?[] knownResults = new bool?[today.Length];
            for (int subject = 0; subject < today.Length; subject++)
                knownResults[subject] = attendance[subject] ? today[subject] : null;

            observations.Add(new DayObservation(attendance, knownResults));
            // Модель использует реальные опросы, включая пропущенные пары.
            yesterday = today;
        }

        return new SemesterResult(pleasure, settings.DayCount, settings.DayCount, null, null);
    }
}

public sealed record SemesterResult(
    int TotalPleasure,
    int CompletedDays,
    int TotalDays,
    int? ExpelledDay,
    Subject? ExpelledSubject)
{
    public bool IsExpelled => ExpelledDay.HasValue;
    public double AveragePerDay => (double)TotalPleasure / TotalDays;
}
