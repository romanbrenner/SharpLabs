namespace SharpLabs.Core;

public interface IStudentHistoryStore
{
    IReadOnlyStudentHistory Snapshot();
    void AddDay(IReadOnlyList<bool> attendance, IReadOnlyList<bool> asked);
}

public sealed class StudentHistoryStore : IStudentHistoryStore
{
    private readonly List<DayObservation> days = new();

    public IReadOnlyStudentHistory Snapshot() => new StudentHistory(days);

    public void AddDay(IReadOnlyList<bool> attendance, IReadOnlyList<bool> asked)
    {
        if (attendance.Count != Subjects.Names.Count || asked.Count != Subjects.Names.Count)
            throw new ArgumentException("Необходимо передать данные для всех предметов.");

        bool?[] knownResults = new bool?[asked.Count];
        for (int subject = 0; subject < asked.Count; subject++)
            knownResults[subject] = attendance[subject] ? asked[subject] : null;

        days.Add(new DayObservation(attendance.ToArray(), knownResults));
    }
}
