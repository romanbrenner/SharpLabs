using Lab01;
using SharpLabs.Core;
using static Lab03.Tests.TestData;

namespace Lab03.Tests;

public class SimulationTests
{
    // Разные награды позволяют отличить пирожок от успешного прогула.
    private static SimulationSettings Rewards => Settings with { PiePleasure = 3, SkipPleasure = 2 };

    [TestCase(true,  false, 3, false)]
    [TestCase(true,  true,  3, false)]
    [TestCase(false, false, 5, false)]
    [TestCase(false, true,  0, true)]
    public void AttendanceAndQuestionDetermineTheOutcome(bool attended, bool asked,
        int expectedPleasure, bool expelled)
    {
        var random = new ScriptedRandom((2, asked ? 0 : 1));
        bool[] plan = Flags(AllSubjects);
        plan[(int)Subject.Physics] = attended;
        var history = new StudentHistoryStore();
        var simulation = new DaySimulation(new TeacherService(WithRandomPhysics(), random), history,
            new PlannedAttendance(plan), Rewards);

        DayResult result = simulation.NextDay();

        Assert.That(result.Day, Is.EqualTo(1));
        Assert.That(result.TotalPleasure, Is.EqualTo(expectedPleasure));
        Assert.That(result.IsExpelled, Is.EqualTo(expelled));
        Assert.That(result.ExpelledSubject, Is.EqualTo(expelled ? Subject.Physics : (Subject?)null));
        if (!expelled)
        {
            IReadOnlyStudentHistory snapshot = history.Snapshot();
            Assert.That(snapshot.Attended(1, Subject.Physics), Is.EqualTo(attended));
            Assert.That(snapshot.WasAsked(1, Subject.Physics), Is.EqualTo(attended ? asked : (bool?)null));
        }
        random.AssertConsumed();
    }

    [Test]
    public void YesterdayAdvancesAfterAllSixTeachersHaveAsked()
    {
        Teacher[] teachers = SilentTeachers();
        teachers[(int)Subject.Calculus] = new Teacher(QuestionRule.Random);
        teachers[(int)Subject.LinearAlgebra] = new Teacher(QuestionRule.Copy, Subject.Calculus);
        teachers[(int)Subject.DiscreteMath] = new Teacher(QuestionRule.Copy, Subject.LinearAlgebra);
        var random = new ScriptedRandom((2, 0), (2, 1), (2, 1), (2, 1));
        bool[] attend = Flags(AllSubjects);
        var history = new StudentHistoryStore();
        var simulation = new DaySimulation(new TeacherService(teachers, random), history,
            new PlannedAttendance(attend, attend, attend, attend), Rewards);

        // Единственный случайный опрос переходит по цепочке по одному предмету в день.
        bool[][] expected = [Flags(Subject.Calculus), Flags(Subject.LinearAlgebra), Flags(Subject.DiscreteMath), Flags()];
        for (int day = 1; day <= expected.Length; day++)
        {
            DayResult result = simulation.NextDay();
            Assert.That(result.Day, Is.EqualTo(day));
            IReadOnlyStudentHistory snapshot = history.Snapshot();
            Assert.That(AllSubjects.Select(subject => snapshot.WasAsked(day, subject)),
                Is.EqualTo(expected[day - 1]), $"День {day}");
        }
        random.AssertConsumed();
    }

    [Test]
    public void ExpulsionErasesEarlierRewardsAndPreventsAnotherDay()
    {
        var random = new ScriptedRandom((2, 1), (2, 0));
        var strategy = new PlannedAttendance(Flags(), Flags());
        var simulation = new DaySimulation(new TeacherService(WithRandomPhysics(), random),
            new StudentHistoryStore(), strategy, Rewards);

        Assert.That(simulation.NextDay().TotalPleasure, Is.EqualTo(15));
        DayResult last = simulation.NextDay();
        Assert.That(last.Day, Is.EqualTo(2));
        Assert.That(last.ExpelledSubject, Is.EqualTo(Subject.Physics));
        Assert.That(last.TotalPleasure, Is.Zero);
        Assert.That(last.AveragePerDay, Is.Zero);

        Assert.Throws<InvalidOperationException>(() => simulation.NextDay());
        Assert.That(strategy.Calls, Is.EqualTo(2));
        random.AssertConsumed();
    }

    [Test]
    public void SemesterStopsItsLoopOnTheDayOfExpulsion()
    {
        var random = new ScriptedRandom((2, 1), (2, 0));
        var strategy = new PlannedAttendance(Flags(), Flags());

        SemesterResult result = new Semester(WithRandomPhysics(), random, Rewards).Run(strategy);

        Assert.That(result.ExpelledDay, Is.EqualTo(2));
        Assert.That(result.ExpelledSubject, Is.EqualTo(Subject.Physics));
        Assert.That(result.CompletedDays, Is.EqualTo(1));
        Assert.That(result.TotalPleasure, Is.Zero);
        Assert.That(strategy.Calls, Is.EqualTo(2));
        random.AssertConsumed();
    }

    private static Teacher[] SilentTeachers() =>
        AllSubjects.Select(subject => new Teacher(QuestionRule.Copy, subject)).ToArray();

    private static Teacher[] WithRandomPhysics()
    {
        Teacher[] teachers = SilentTeachers();
        teachers[(int)Subject.Physics] = new Teacher(QuestionRule.Random);
        return teachers;
    }

    private sealed class PlannedAttendance(params bool[][] days) : ISkipStrategy
    {
        public string Name => "Заданный план";
        public int Calls { get; private set; }

        public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
        {
            Calls++;
            Assert.That(day, Is.EqualTo(Calls));
            Assert.That(day, Is.LessThanOrEqualTo(days.Length), "Стратегия вызвана после последнего дня");
            return days[day - 1];
        }
    }
}
