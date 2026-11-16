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
    private readonly Subject subjectA;
    private readonly Subject subjectB;

    public Teacher(QuestionRule rule, Subject subjectA = Subject.Calculus,
        Subject subjectB = Subject.LinearAlgebra)
    {
        if (!Enum.IsDefined(rule))
            throw new ArgumentOutOfRangeException(nameof(rule));
        if (!Enum.IsDefined(subjectA))
            throw new ArgumentOutOfRangeException(nameof(subjectA));
        if (!Enum.IsDefined(subjectB))
            throw new ArgumentOutOfRangeException(nameof(subjectB));
        if (rule == QuestionRule.ExclusiveOr && subjectA == subjectB)
            throw new ArgumentException("Предметы A и Б должны различаться.");

        this.rule = rule;
        this.subjectA = subjectA;
        this.subjectB = subjectB;
    }

    public static Teacher CreateRandom(Random random)
    {
        QuestionRule rule = (QuestionRule)random.Next(Enum.GetValues<QuestionRule>().Length);
        int subjectA = random.Next(Subjects.Names.Count);
        // Равномерно выбираем предмет, отличный от A.
        int subjectB = random.Next(Subjects.Names.Count - 1);
        if (subjectB >= subjectA)
            subjectB++;

        return new Teacher(rule, (Subject)subjectA, (Subject)subjectB);
    }

    public bool WillAsk(IReadOnlyList<bool> yesterday, Random random)
    {
        return rule switch
        {
            QuestionRule.Random => random.Next(2) == 0,
            QuestionRule.Copy => yesterday[(int)subjectA],
            QuestionRule.ExclusiveOr => yesterday[(int)subjectA] ^ yesterday[(int)subjectB],
            _ => throw new InvalidOperationException("Неизвестное правило.")
        };
    }
}
