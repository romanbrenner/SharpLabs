namespace Lab01;

public interface IStudentStrategy
{
    // Текущий результат опроса и правило преподавателя здесь недоступны.
    bool ShouldSkip(int subject, IReadOnlyList<bool> yesterday, int maximumFinalPleasure);

    // Вызывается после завершения дня, когда результаты опросов уже известны.
    void ObserveDay(IReadOnlyList<bool> yesterday, IReadOnlyList<bool> today);
}
