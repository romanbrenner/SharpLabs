namespace Lab01;

// В историю передаются только завершённые дни и доступные студенту результаты.
internal sealed class StudentHistory : IReadOnlyStudentHistory
{
    private readonly DayObservation[] days;

    public StudentHistory(IReadOnlyList<DayObservation> days)
    {
        // Снимок не пополняется после передачи стратегии.
        this.days = days.ToArray();
    }

    public bool Attended(int day, Subject subject)
    {
        Validate(day, subject);
        return day <= days.Length && days[day - 1].Attendance[(int)subject];
    }

    public bool? WasAsked(int day, Subject subject)
    {
        Validate(day, subject);
        return day <= days.Length ? days[day - 1].Asked[(int)subject] : null;
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
