using SharpLabs.Core;
using static Lab03.Tests.TestData;

namespace Lab03.Tests;

public class StrategyTests
{
    [Test]
    public void FirstDayWithEmptyHistoryAttendsEveryLesson()
    {
        var strategy = new BayesianStrategy(Settings);
        Assert.That(strategy.DecideDay(1, new History()), Is.EqualTo(Flags(AllSubjects)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TwentyIdenticalDaysGiveTheExpectedPlan(bool alwaysAsked)
    {
        var history = new History();
        bool[] expected = alwaysAsked ? Flags(AllSubjects) : Flags();
        for (int day = 1; day <= 20; day++)
            history.Add(expected);

        Assert.That(new BayesianStrategy(Settings).DecideDay(21, history), Is.EqualTo(expected));
    }

    [TestCase(Subject.Calculus)]
    [TestCase(Subject.LinearAlgebra)]
    [TestCase(Subject.DiscreteMath)]
    [TestCase(Subject.Programming)]
    [TestCase(Subject.Physics)]
    [TestCase(Subject.English)]
    public void OnlyTheSubjectWithRegularQuestionsIsAttended(Subject asked)
    {
        var history = new History();
        for (int day = 1; day <= 20; day++)
            history.Add(Flags(asked));

        Assert.That(new BayesianStrategy(Settings).DecideDay(21, history), Is.EqualTo(Flags(asked)));
    }

    [TestCase(false, false, false, false)]
    [TestCase(false, true,  false, true)]
    [TestCase(true,  false, true,  true)]
    [TestCase(true,  true,  true,  false)]
    public void MixedHistoryPredictsCopyAndXorOnTheNextDay(bool lastPhysics, bool lastEnglish,
        bool attendCalculus, bool attendProgramming)
    {
        var history = new History();
        bool previousPhysics = false, previousEnglish = false;
        for (int day = 1; day <= 40; day++)
        {
            // Матанализ повторяет вчерашнюю физику. Программирование зависит от физики и английского.
            bool physics = day == 40 ? lastPhysics : day % 5 < 3;
            bool english = day == 40 ? lastEnglish : day % 7 < 4;
            history.Add([previousPhysics, false, false, previousPhysics ^ previousEnglish, physics, english]);
            previousPhysics = physics;
            previousEnglish = english;
        }

        bool[] expected = [attendCalculus, false, false, attendProgramming, true, true];
        Assert.That(new BayesianStrategy(Settings).DecideDay(41, history), Is.EqualTo(expected));
    }

    [Test]
    public void MissingObservationsAreNotTreatedAsSixNegativeAnswers()
    {
        var history = new History();
        for (int day = 1; day <= 20; day++)
            history.Add(Flags());
        history.AddMissedDay();

        Assert.That(new BayesianStrategy(Settings).DecideDay(22, history), Is.EqualTo(Flags(AllSubjects)));
    }

    private sealed class History : IReadOnlyStudentHistory
    {
        private readonly List<bool?[]> rows = [];
        public void Add(bool[] asked) => rows.Add(Array.ConvertAll(asked, value => (bool?)value));
        public void AddMissedDay() => rows.Add(new bool?[AllSubjects.Length]);
        public bool Attended(int day, Subject subject) => WasAsked(day, subject).HasValue;
        public bool? WasAsked(int day, Subject subject) => day <= rows.Count ? rows[day - 1][(int)subject] : null;
    }
}
