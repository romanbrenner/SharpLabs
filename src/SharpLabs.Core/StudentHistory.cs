namespace SharpLabs.Core;

// В историю передаются только завершённые дни и доступные студенту результаты.
internal sealed class StudentHistory : IReadOnlyStudentHistory
{
    private readonly IReadOnlyList<DayObservation> days;
    private readonly int visibleDays;

    public StudentHistory(IReadOnlyList<DayObservation> days)
    {
        // Снимок видит только дни, существовавшие на момент его создания.
        // Копировать всю накопленную историю на каждом шаге не требуется.
        this.days = days;
        visibleDays = days.Count;
    }

    public bool Attended(int day, Subject subject)
    {
        Validate(day, subject);
        return day <= visibleDays && days[day - 1].Attendance[(int)subject];
    }

    public bool? WasAsked(int day, Subject subject)
    {
        Validate(day, subject);
        return day <= visibleDays ? days[day - 1].Asked[(int)subject] : null;
    }

    private static void Validate(int day, Subject subject)
    {
        if (day < 1)
            throw new ArgumentOutOfRangeException(nameof(day), "Нумерация дней начинается с 1.");
        if (!Enum.IsDefined(subject))
            throw new ArgumentOutOfRangeException(nameof(subject));
    }
}

internal sealed record DayObservation(bool[] Attendance, bool?[] Asked);
