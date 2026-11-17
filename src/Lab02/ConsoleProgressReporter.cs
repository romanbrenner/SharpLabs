using System.Globalization;
using Microsoft.Extensions.Options;
using SharpLabs.Core;

namespace Lab02;

public interface IProgressReporter
{
    void Started();
    void WriteDay(DayResult result);
    void Stopped();
}

public sealed class ConsoleProgressReporter(TextWriter output, ISkipStrategy strategy,
    SeededRandom random, IKeyInput input, IOptions<Lab02Options> options) : IProgressReporter
{
    public void Started()
    {
        output.WriteLine("Мастер прогулов 80-го уровня — лабораторная работа 2");
        output.WriteLine($"Стратегия: {strategy.Name}. Начальное значение генератора: {random.Seed}");
        output.WriteLine($"Новый день каждые {options.Value.DayIntervalSeconds} сек.");
        output.WriteLine(input.IsInteractive
            ? "Для остановки нажмите любую клавишу или Ctrl+C."
            : "Для остановки нажмите Ctrl+C.");
    }

    public void WriteDay(DayResult result)
    {
        string average = result.AveragePerDay.ToString("F2", CultureInfo.GetCultureInfo("ru-RU"));
        output.WriteLine($"День {result.Day}. Общее удовольствие: {result.TotalPleasure}. " +
                         $"Среднее удовольствие в день: {average}.");
        if (result.IsExpelled)
            output.WriteLine($"Студент отчислен: {Subjects.Names[(int)result.ExpelledSubject!.Value]}.");
    }

    public void Stopped() => output.WriteLine("Выполнение завершено.");
}
