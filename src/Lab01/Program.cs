using System.Globalization;
using System.Text;
using System.Text.Json;
using Lab01;
using SharpLabs.Core;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
{
    Console.WriteLine("Запуск: dotnet run --project src/Lab01 -- [--seed целое_число]");
    return 0;
}

int seedArgument = 0;
if (args.Length != 0 &&
    (args.Length != 2 || args[0] != "--seed" || !int.TryParse(args[1], out seedArgument)))
{
    Console.Error.WriteLine("Ошибка: используйте --seed целое_число или --help.");
    return 1;
}

SimulationSettings settings;
try
{
    settings = SimulationSettings.Load(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                                 or JsonException or ArgumentException)
{
    Console.Error.WriteLine($"Ошибка в appsettings.json: {exception.Message}");
    return 1;
}

int seed = args.Length == 2 ? seedArgument : settings.Seed ?? Random.Shared.Next();
Random random = new Random(seed);
Teacher[] teachers = new Teacher[Subjects.Names.Count];
for (int subject = 0; subject < teachers.Length; subject++)
{
    teachers[subject] = Teacher.CreateRandom(random);
}

ISkipStrategy strategy = new BayesianStrategy(settings);
Semester semester = new Semester(teachers, random, settings);
SemesterResult result = semester.Run(strategy);

Console.WriteLine("Мастер прогулов 80-го уровня — лабораторная работа 1");
Console.WriteLine($"Начальное значение генератора: {seed}");
Console.WriteLine($"Стратегия: {strategy.Name}");
if (result.IsExpelled)
{
    Console.WriteLine($"Студент отчислен: день {result.ExpelledDay}, " +
                      $"предмет «{Subjects.Names[(int)result.ExpelledSubject!.Value]}».");
}
else
{
    Console.WriteLine($"Семестр завершён: {result.CompletedDays} дней.");
}

Console.WriteLine($"Общее удовольствие за семестр: {result.TotalPleasure}");
Console.WriteLine("Среднее удовольствие в день: " +
                  result.AveragePerDay.ToString("F2", CultureInfo.GetCultureInfo("ru-RU")));
return 0;
