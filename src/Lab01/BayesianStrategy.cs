namespace Lab01;

public sealed class BayesianStrategy : IStudentStrategy
{
    private readonly List<Hypothesis>[] hypotheses = new List<Hypothesis>[Semester.SubjectCount];

    public BayesianStrategy()
    {
        for (int subject = 0; subject < Semester.SubjectCount; subject++)
        {
            List<Hypothesis> candidates = new List<Hypothesis>();
            candidates.Add(new Hypothesis(QuestionRule.Random, 0, 0, 1.0 / 3));

            for (int a = 0; a < Semester.SubjectCount; a++)
                candidates.Add(new Hypothesis(QuestionRule.Copy, a, 0,
                    1.0 / (3 * Semester.SubjectCount)));

            int pairCount = Semester.SubjectCount * (Semester.SubjectCount - 1) / 2;
            for (int a = 0; a < Semester.SubjectCount; a++)
                for (int b = a + 1; b < Semester.SubjectCount; b++)
                    candidates.Add(new Hypothesis(QuestionRule.ExclusiveOr, a, b,
                        1.0 / (3 * pairCount)));

            hypotheses[subject] = candidates;
        }
    }

    public double GetQuestionProbability(int subject, IReadOnlyList<bool> yesterday)
    {
        double probability = 0;
        foreach (Hypothesis hypothesis in hypotheses[subject])
            probability += hypothesis.Weight * hypothesis.Predict(yesterday);

        return probability;
    }

    public bool ShouldSkip(int subject, IReadOnlyList<bool> yesterday, int maximumFinalPleasure)
    {
        double probability = GetQuestionProbability(subject, yesterday);
        return probability * maximumFinalPleasure < 1;
    }

    public void ObserveDay(IReadOnlyList<bool> yesterday, IReadOnlyList<bool> today)
    {
        for (int subject = 0; subject < Semester.SubjectCount; subject++)
        {
            double totalWeight = 0;
            foreach (Hypothesis hypothesis in hypotheses[subject])
            {
                double probability = hypothesis.Predict(yesterday);
                hypothesis.Weight *= today[subject] ? probability : 1 - probability;
                totalWeight += hypothesis.Weight;
            }

            // Случайная гипотеза совместима с любой историей, поэтому сумма > 0.
            foreach (Hypothesis hypothesis in hypotheses[subject])
                hypothesis.Weight /= totalWeight;
        }
    }

    private sealed class Hypothesis
    {
        private readonly QuestionRule rule;
        private readonly int subjectA;
        private readonly int subjectB;
        public double Weight { get; set; }

        public Hypothesis(QuestionRule rule, int subjectA, int subjectB, double weight)
        {
            this.rule = rule;
            this.subjectA = subjectA;
            this.subjectB = subjectB;
            Weight = weight;
        }

        public double Predict(IReadOnlyList<bool> yesterday)
        {
            return rule switch
            {
                QuestionRule.Random => 0.5,
                QuestionRule.Copy => yesterday[subjectA] ? 1 : 0,
                QuestionRule.ExclusiveOr => yesterday[subjectA] ^ yesterday[subjectB] ? 1 : 0,
                _ => throw new InvalidOperationException("Неизвестная гипотеза.")
            };
        }
    }
}
