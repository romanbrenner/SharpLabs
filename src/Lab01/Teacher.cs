namespace Lab01;

public enum QuestionRule
{
    Random,
    Copy,
    ExclusiveOr
}

public sealed class Teacher
{
    private readonly QuestionRule rule;
    private readonly int subjectA;
    private readonly int subjectB;

    public Teacher(QuestionRule rule, int subjectA = 0, int subjectB = 1)
    {
        if (!Enum.IsDefined(rule))
            throw new ArgumentOutOfRangeException(nameof(rule));
        if (subjectA < 0 || subjectA >= Semester.SubjectCount)
            throw new ArgumentOutOfRangeException(nameof(subjectA));
        if (subjectB < 0 || subjectB >= Semester.SubjectCount)
            throw new ArgumentOutOfRangeException(nameof(subjectB));
        if (rule == QuestionRule.ExclusiveOr && subjectA == subjectB)
            throw new ArgumentException("Предметы A и Б должны различаться.");

        this.rule = rule;
        this.subjectA = subjectA;
        this.subjectB = subjectB;
    }

    public static Teacher CreateRandom(Random random)
    {
        QuestionRule rule = (QuestionRule)random.Next(3);
        int subjectA = random.Next(Semester.SubjectCount);
        // Равномерно выбираем один из пяти предметов, отличных от A.
        int subjectB = random.Next(Semester.SubjectCount - 1);
        if (subjectB >= subjectA)
            subjectB++;

        return new Teacher(rule, subjectA, subjectB);
    }

    public bool WillAsk(IReadOnlyList<bool> yesterday, Random random)
    {
        return rule switch
        {
            QuestionRule.Random => random.Next(2) == 0,
            QuestionRule.Copy => yesterday[subjectA],
            QuestionRule.ExclusiveOr => yesterday[subjectA] ^ yesterday[subjectB],
            _ => throw new InvalidOperationException("Неизвестное правило.")
        };
    }
}
