using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Lab02;

public sealed class KeyboardStopService(IKeyInput input, IHostApplicationLifetime lifetime,
    IOptions<Lab02Options> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!input.IsInteractive)
            return;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (input.TryReadKey())
                {
                    lifetime.StopApplication();
                    return;
                }
                await Task.Delay(options.Value.KeyboardPollMilliseconds, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
