using SharpLabs.Core;

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
        IDaySimulation simulation = new DaySimulation(new TeacherService(teachers, random),
            new StudentHistoryStore(), strategy, settings);
        long pleasure = 0;

        for (int day = 1; day <= settings.DayCount; day++)
        {
            DayResult result = simulation.NextDay();
            if (result.IsExpelled)
                return new SemesterResult(0, day - 1, settings.DayCount, day, result.ExpelledSubject);
            pleasure = result.TotalPleasure;
        }

        return new SemesterResult(checked((int)pleasure), settings.DayCount, settings.DayCount, null, null);
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
