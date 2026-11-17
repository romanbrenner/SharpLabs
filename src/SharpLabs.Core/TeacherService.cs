namespace SharpLabs.Core;

public interface ITeacherService
{
    IReadOnlyList<bool> AskToday();
}

public sealed class TeacherService : ITeacherService
{
    private readonly Teacher[] teachers;
    private readonly Random random;
    private bool[] yesterday = new bool[Subjects.Names.Count];

    public TeacherService(Random random)
        : this(Enumerable.Range(0, Subjects.Names.Count).Select(_ => Teacher.CreateRandom(random)).ToArray(), random)
    {
    }

    public TeacherService(IReadOnlyList<Teacher> teachers, Random random)
    {
        if (teachers.Count != Subjects.Names.Count)
            throw new ArgumentException("Количество преподавателей должно совпадать с количеством предметов.");
        this.teachers = teachers.ToArray();
        this.random = random;
    }

    public IReadOnlyList<bool> AskToday()
    {
        bool[] today = new bool[Subjects.Names.Count];
        for (int subject = 0; subject < today.Length; subject++)
            today[subject] = teachers[subject].WillAsk(yesterday, random);

        yesterday = today;
        return Array.AsReadOnly(today);
    }
}
