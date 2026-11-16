namespace Lab01;

public sealed class Semester
{
    public const int DayCount = 100;
    public const int SubjectCount = 6;

    public static IReadOnlyList<string> SubjectNames { get; } = Array.AsReadOnly(new[]
    {
        "Матанализ", "Линейная алгебра", "Дискретная математика",
        "Программирование", "Физика", "Английский язык"
    });

    private readonly Teacher[] teachers;
    private readonly Random random;

    public Semester(IReadOnlyList<Teacher> teachers, Random random)
    {
        if (teachers.Count != SubjectCount)
            throw new ArgumentException("В семестре должно быть шесть преподавателей.");

        this.teachers = teachers.ToArray();
        this.random = random;
    }

    public SemesterResult Run(IStudentStrategy strategy)
    {
        IReadOnlyList<bool> yesterday = Array.AsReadOnly(new bool[SubjectCount]);
        int pleasure = 0;

        for (int day = 1; day <= DayCount; day++)
        {
            bool[] today = new bool[SubjectCount];
            for (int subject = 0; subject < SubjectCount; subject++)
                today[subject] = teachers[subject].WillAsk(yesterday, random);

            for (int subject = 0; subject < SubjectCount; subject++)
            {
                int remainingLessons = (DayCount - day) * SubjectCount + SubjectCount - subject;
                int remainingPies = DayCount - day + 1;
                int maximumFinalPleasure = pleasure + remainingLessons + remainingPies;

                if (strategy.ShouldSkip(subject, yesterday, maximumFinalPleasure))
                {
                    if (today[subject])
                        return new SemesterResult(0, day - 1, day, subject);

                    pleasure++;
                }
            }

            pleasure++; // Пирожок после успешного учебного дня.
            IReadOnlyList<bool> observedToday = Array.AsReadOnly(today);
            strategy.ObserveDay(yesterday, observedToday);
            yesterday = observedToday;
        }

        return new SemesterResult(pleasure, DayCount, null, null);
    }
}

public sealed record SemesterResult(
    int TotalPleasure,
    int CompletedDays,
    int? ExpelledDay,
    int? ExpelledSubject)
{
    public bool IsExpelled => ExpelledDay.HasValue;
    public double AveragePerDay => (double)TotalPleasure / Semester.DayCount;
}
