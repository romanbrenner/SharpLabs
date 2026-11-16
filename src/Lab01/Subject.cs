namespace Lab01;

public enum Subject
{
    Calculus,           // Матанализ
    LinearAlgebra,      // Линейная алгебра
    DiscreteMath,       // Дискретная математика
    Programming,        // Программирование
    Physics,            // Физика
    English             // Английский язык
}

public static class Subjects
{
    // Порядок названий соответствует значениям Subject.
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(new[]
    {
        "Матанализ", "Линейная алгебра", "Дискретная математика",
        "Программирование", "Физика", "Английский язык"
    });
}
