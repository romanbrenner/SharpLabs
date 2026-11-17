namespace SharpLabs.Core;

public interface IDaySimulation
{
    DayResult NextDay();
}

public sealed class DaySimulation : IDaySimulation
{
    private readonly ITeacherService teachers;
    private readonly IStudentHistoryStore history;
    private readonly ISkipStrategy strategy;
    private readonly SimulationSettings settings;
    private int day;
    private long pleasure;
    private bool expelled;

    public DaySimulation(ITeacherService teachers, IStudentHistoryStore history,
        ISkipStrategy strategy, SimulationSettings settings)
    {
        settings.Validate();
        this.teachers = teachers;
        this.history = history;
        this.strategy = strategy;
        this.settings = settings;
    }

    public DayResult NextDay()
    {
        if (expelled)
            throw new InvalidOperationException("После отчисления продолжать обучение нельзя.");

        int nextDay = checked(day + 1);
        bool[] decision = strategy.DecideDay(nextDay, history.Snapshot());
        if (decision is null || decision.Length != Subjects.Names.Count)
            throw new InvalidOperationException("Стратегия должна вернуть решение для каждого предмета.");

        bool[] attendance = decision.ToArray();
        IReadOnlyList<bool> asked = teachers.AskToday();
        if (asked.Count != Subjects.Names.Count)
            throw new InvalidOperationException("Необходимы результаты опросов по всем предметам.");

        day = nextDay;
        for (int subject = 0; subject < attendance.Length; subject++)
        {
            if (!attendance[subject])
            {
                if (asked[subject])
                {
                    expelled = true;
                    pleasure = 0;
                    return new DayResult(day, pleasure, (Subject)subject);
                }
                pleasure += settings.SkipPleasure;
            }
        }

        pleasure += settings.PiePleasure;
        history.AddDay(attendance, asked);
        return new DayResult(day, pleasure, null);
    }
}

public sealed record DayResult(int Day, long TotalPleasure, Subject? ExpelledSubject)
{
    public bool IsExpelled => ExpelledSubject.HasValue;
    public double AveragePerDay => (double)TotalPleasure / Day;
}
