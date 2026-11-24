using SharpLabs.Core;
using static Lab03.Tests.TestData;

namespace Lab03.Tests;

public class TeacherTests
{
    [Test]
    public void RandomRuleUsesOneCoinTossPerLesson()
    {
        var random = new ScriptedRandom((2, 0), (2, 1), (2, 1), (2, 0));
        var teacher = new Teacher(QuestionRule.Random);
        bool[] outcomes = Enumerable.Range(0, 4).Select(_ => teacher.WillAsk(Flags(), random)).ToArray();

        Assert.That(outcomes, Is.EqualTo(new[] { true, false, false, true }));
        random.AssertConsumed();
    }

    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, true)]
    public void CopyReadsOnlyItsSelectedSubject(bool askedA, bool askedOther, bool expected)
    {
        var teacher = new Teacher(QuestionRule.Copy, Subject.Physics);
        bool[] yesterday = Flags();
        yesterday[(int)Subject.Physics] = askedA;
        yesterday[(int)Subject.English] = askedOther;

        Assert.That(teacher.WillAsk(yesterday, new ScriptedRandom()), Is.EqualTo(expected));
    }

    [TestCase(false, false, false)]
    [TestCase(false, true, true)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    public void ExclusiveOrAsksWhenExactlyOneSelectedSubjectWasAsked(bool a, bool b, bool expected)
    {
        var teacher = new Teacher(QuestionRule.ExclusiveOr, Subject.English, Subject.DiscreteMath);
        bool[] yesterday = Flags(Subject.LinearAlgebra);
        yesterday[(int)Subject.English] = a;
        yesterday[(int)Subject.DiscreteMath] = b;

        Assert.That(teacher.WillAsk(yesterday, new ScriptedRandom()), Is.EqualTo(expected));
    }

    [TestCase(0, Subject.Calculus)]
    [TestCase(3, Subject.Physics)]
    public void RandomTeacherFactorySetsTheDrawnRuleAndSubjects(int secondDraw, Subject expectedB)
    {
        // A — программирование. Второй индекс выбирается из пяти оставшихся предметов.
        var random = new ScriptedRandom((3, (int)QuestionRule.ExclusiveOr), (6, (int)Subject.Programming),
            (5, secondDraw));
        Teacher teacher = Teacher.CreateRandom(random);
        foreach (Subject subject in AllSubjects)
            Assert.That(teacher.WillAsk(Flags(subject), random),
                Is.EqualTo(subject == Subject.Programming || subject == expectedB), subject.ToString());
        Assert.That(teacher.WillAsk(Flags(Subject.Programming, expectedB), random), Is.False);
        random.AssertConsumed();
    }
}
