using Microsoft.Extensions.Options;

namespace Lab02;

public interface IDayClock
{
    ValueTask<bool> WaitForNextDayAsync(CancellationToken cancellationToken);
}

public sealed class PeriodicDayClock : IDayClock, IDisposable
{
    private readonly PeriodicTimer timer;

    public PeriodicDayClock(IOptions<Lab02Options> options)
    {
        timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.DayIntervalSeconds));
    }

    public ValueTask<bool> WaitForNextDayAsync(CancellationToken cancellationToken)
        => timer.WaitForNextTickAsync(cancellationToken);

    public void Dispose() => timer.Dispose();
}
