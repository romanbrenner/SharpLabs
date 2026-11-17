namespace Lab02;

public interface IKeyInput
{
    bool IsInteractive { get; }
    bool TryReadKey();
}

public sealed class ConsoleKeyInput : IKeyInput
{
    public bool IsInteractive => !Console.IsInputRedirected;

    public bool TryReadKey()
    {
        if (!IsInteractive || !Console.KeyAvailable)
            return false;

        Console.ReadKey(intercept: true);
        return true;
    }
}
