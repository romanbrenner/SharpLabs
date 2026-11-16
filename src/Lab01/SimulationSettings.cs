using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lab01;

public sealed record SimulationSettings
{
    public required int DayCount { get; init; }
    public required int SkipPleasure { get; init; }
    public required int PiePleasure { get; init; }
    public int? Seed { get; init; }

    public static SimulationSettings Load(string path)
    {
        string json = File.ReadAllText(path);
        SimulationSettings settings = JsonSerializer.Deserialize<SimulationSettings>(json,
            new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })
            ?? throw new JsonException("Файл настроек не должен содержать null.");
        settings.Validate();
        return settings;
    }

    public void Validate()
    {
        if (DayCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(DayCount), "Количество дней должно быть положительным.");
        if (SkipPleasure <= 0 || PiePleasure <= 0)
            throw new ArgumentException("Удовольствие от прогула и пирожка должно быть положительным.");

        long maximumPerDay = (long)SkipPleasure * Subjects.Names.Count + PiePleasure;
        if (DayCount > int.MaxValue / maximumPerDay)
            throw new ArgumentException("Максимальное удовольствие за семестр не помещается в int.");
    }
}
