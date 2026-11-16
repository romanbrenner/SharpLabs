namespace Lab01;

public sealed class BayesianStrategy : ISkipStrategy
{
    private readonly SimulationSettings settings;
    private readonly List<Hypothesis>[] hypotheses = new List<Hypothesis>[Subjects.Names.Count];
    private int processedDays;
    private int pleasure;

    public string Name => "Обучение по истории опросов";

    public BayesianStrategy(SimulationSettings settings)
    {
        settings.Validate();
        this.settings = settings;
        Reset();
    }

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        if (day < 1 || day > settings.DayCount)
            throw new ArgumentOutOfRangeException(nameof(day));
        if (day == 1)
            Reset();
        if (day <= processedDays)
            throw new ArgumentException("Дни должны обрабатываться по порядку.", nameof(day));

        for (int observedDay = processedDays + 1; observedDay < day; observedDay++)
        {
            bool?[] yesterday = PreviousResults(observedDay, history);
            bool?[] today = ReadResults(observedDay, history);

            // Обновляем веса только по наблюдениям с известными входами.
            // Прогул и отсутствие опроса — разные события: null не заменяется на false.
            if (yesterday.All(result => result.HasValue))
                ObserveDay(yesterday, today);

            pleasure += settings.PiePleasure;
            for (int subject = 0; subject < Subjects.Names.Count; subject++)
                if (!history.Attended(observedDay, (Subject)subject))
                    pleasure += settings.SkipPleasure;

            processedDays = observedDay;
        }

        bool?[] previous = PreviousResults(day, history);
        int remainingDays = settings.DayCount - day + 1;
        int maximumFinalPleasure = pleasure + remainingDays *
            (Subjects.Names.Count * settings.SkipPleasure + settings.PiePleasure);
        bool[] attendance = new bool[Subjects.Names.Count];

        for (int subject = 0; subject < attendance.Length; subject++)
        {
            double questionRisk = 0;
            foreach (Hypothesis hypothesis in hypotheses[subject])
            {
                // Если вход гипотезы неизвестен, для решения берём худший исход — опрос.
                questionRisk += hypothesis.Weight * (hypothesis.Predict(previous) ?? 1);
            }

            attendance[subject] = questionRisk * maximumFinalPleasure >= settings.SkipPleasure;
            if (attendance[subject])
                maximumFinalPleasure -= settings.SkipPleasure;
        }

        return attendance;
    }

    private void Reset()
    {
        processedDays = 0;
        pleasure = 0;
        int subjectCount = Subjects.Names.Count;
        for (int subject = 0; subject < subjectCount; subject++)
        {
            List<Hypothesis> candidates = new List<Hypothesis>();
            candidates.Add(new Hypothesis(QuestionRule.Random, 0, 0, 1.0 / 3));

            for (int a = 0; a < subjectCount; a++)
                candidates.Add(new Hypothesis(QuestionRule.Copy, a, 0,
                    1.0 / (3 * subjectCount)));

            int pairCount = subjectCount * (subjectCount - 1) / 2;
            for (int a = 0; a < subjectCount; a++)
                for (int b = a + 1; b < subjectCount; b++)
                    candidates.Add(new Hypothesis(QuestionRule.ExclusiveOr, a, b,
                        1.0 / (3 * pairCount)));

            hypotheses[subject] = candidates;
        }
    }

    private static bool?[] PreviousResults(int day, IReadOnlyStudentHistory history)
    {
        // По условию перед первым днём никого не спрашивали.
        return day == 1 ? new bool[Subjects.Names.Count].Select(value => (bool?)value).ToArray()
            : ReadResults(day - 1, history);
    }

    private static bool?[] ReadResults(int day, IReadOnlyStudentHistory history)
    {
        bool?[] results = new bool?[Subjects.Names.Count];
        for (int subject = 0; subject < results.Length; subject++)
            if (history.Attended(day, (Subject)subject))
                results[subject] = history.WasAsked(day, (Subject)subject);
        return results;
    }

    private void ObserveDay(IReadOnlyList<bool?> yesterday, IReadOnlyList<bool?> today)
    {
        for (int subject = 0; subject < Subjects.Names.Count; subject++)
        {
            if (!today[subject].HasValue)
                continue;

            foreach (Hypothesis hypothesis in hypotheses[subject])
            {
                double probability = hypothesis.Predict(yesterday)!.Value;
                double likelihood = today[subject]!.Value ? probability : 1 - probability;
                hypothesis.LogWeight += Math.Log(likelihood);
            }

            // Логарифмы сохраняют случайную гипотезу даже после очень длинной истории.
            // Вычитание максимума позволяет нормировать веса без потери малых значений.
            double maximum = hypotheses[subject].Max(hypothesis => hypothesis.LogWeight);
            double logTotal = maximum + Math.Log(hypotheses[subject]
                .Sum(hypothesis => Math.Exp(hypothesis.LogWeight - maximum)));
            foreach (Hypothesis hypothesis in hypotheses[subject])
                hypothesis.LogWeight -= logTotal;
        }
    }

    private sealed class Hypothesis
    {
        private readonly QuestionRule rule;
        private readonly int subjectA;
        private readonly int subjectB;
        public double LogWeight { get; set; }
        public double Weight => double.IsNegativeInfinity(LogWeight) ? 0
            : Math.Max(double.Epsilon, Math.Exp(LogWeight));

        public Hypothesis(QuestionRule rule, int subjectA, int subjectB, double weight)
        {
            this.rule = rule;
            this.subjectA = subjectA;
            this.subjectB = subjectB;
            LogWeight = Math.Log(weight);
        }

        public double? Predict(IReadOnlyList<bool?> yesterday)
        {
            return rule switch
            {
                QuestionRule.Random => 0.5,
                QuestionRule.Copy => yesterday[subjectA] is bool a ? (a ? 1 : 0) : null,
                QuestionRule.ExclusiveOr => yesterday[subjectA] is bool left &&
                    yesterday[subjectB] is bool right ? (left ^ right ? 1 : 0) : null,
                _ => throw new InvalidOperationException("Неизвестная гипотеза.")
            };
        }
    }
}
