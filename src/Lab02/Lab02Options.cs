using SharpLabs.Core;

namespace Lab02;

public sealed class Lab02Options
{
    public double DayIntervalSeconds { get; set; }
    public int KeyboardPollMilliseconds { get; set; }
    public int PlanningDays { get; set; }
    public int SkipPleasure { get; set; }
    public int PiePleasure { get; set; }
    public int? Seed { get; set; }

    public SimulationSettings ToSimulationSettings() => new()
    {
        DayCount = PlanningDays,
        SkipPleasure = SkipPleasure,
        PiePleasure = PiePleasure,
        Seed = Seed
    };

    public bool IsValid()
    {
        if (!double.IsFinite(DayIntervalSeconds) || DayIntervalSeconds < 0.001 ||
            DayIntervalSeconds > (uint.MaxValue - 1) / 1000.0 || KeyboardPollMilliseconds <= 0)
            return false;
        try
        {
            ToSimulationSettings().Validate();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
