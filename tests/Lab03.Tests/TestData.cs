using SharpLabs.Core;

namespace Lab03.Tests;

internal static class TestData
{
    public static readonly Subject[] AllSubjects = Enum.GetValues<Subject>();
    public static SimulationSettings Settings => new() { DayCount = 100, SkipPleasure = 1, PiePleasure = 1 };

    public static bool[] Flags(params Subject[] marked) => AllSubjects.Select(marked.Contains).ToArray();
}

// Каждый шаг задаёт границу Next и возвращаемое значение. Лишний вызов сразу провалит тест.
internal sealed class ScriptedRandom(params (int Bound, int Value)[] steps) : Random(0)
{
    private int position;

    public override int Next(int maxValue)
    {
        Assert.That(position, Is.LessThan(steps.Length), "Незапланированный вызов генератора");
        var (bound, value) = steps[position++];
        Assert.That(maxValue, Is.EqualTo(bound));
        Assert.That(value, Is.InRange(0, bound - 1));
        return value;
    }

    public void AssertConsumed() => Assert.That(position, Is.EqualTo(steps.Length));
}
