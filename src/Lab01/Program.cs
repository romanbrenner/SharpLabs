using System.Globalization;
using System.Text;
using Lab01;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
{
    Console.WriteLine("Запуск: dotnet run --project src/Lab01 -- [--seed целое_число]");
    return 0;
}

int seed = Random.Shared.Next();
if (args.Length != 0 &&
    (args.Length != 2 || args[0] != "--seed" || !int.TryParse(args[1], out seed)))
{
    Console.Error.WriteLine("Ошибка: используйте --seed целое_число или --help.");
    return 1;
}

Random random = new Random(seed);
Teacher[] teachers = new Teacher[Semester.SubjectCount];
for (int subject = 0; subject < teachers.Length; subject++)
{
    teachers[subject] = Teacher.CreateRandom(random);
}

Semester semester = new Semester(teachers, random);
SemesterResult result = semester.Run(new BayesianStrategy());

Console.WriteLine("Мастер прогулов 80-го уровня — лабораторная работа 1");
Console.WriteLine($"Начальное значение генератора: {seed}");
Console.WriteLine("Стратегия: обучение правилам по истории опросов");
if (result.IsExpelled)
{
    Console.WriteLine($"Студент отчислен: день {result.ExpelledDay}, " +
                      $"предмет «{Semester.SubjectNames[result.ExpelledSubject!.Value]}».");
}
else
{
    Console.WriteLine($"Семестр завершён: {result.CompletedDays} дней.");
}

Console.WriteLine($"Общее удовольствие за семестр: {result.TotalPleasure}");
Console.WriteLine("Среднее удовольствие в день: " +
                  result.AveragePerDay.ToString("F2", CultureInfo.GetCultureInfo("ru-RU")));
return 0;
