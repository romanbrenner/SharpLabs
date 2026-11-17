using Microsoft.Extensions.Hosting;
using SharpLabs.Core;

namespace Lab02;

public sealed class SimulationWorker(IDaySimulation simulation, IDayClock clock,
    IProgressReporter reporter, IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        reporter.Started();
        try
        {
            while (await clock.WaitForNextDayAsync(stoppingToken))
            {
                stoppingToken.ThrowIfCancellationRequested();
                DayResult result = simulation.NextDay();
                reporter.WriteDay(result);
                if (result.IsExpelled)
                {
                    lifetime.StopApplication();
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Остановка хоста прерывает ожидание следующего дня.
        }
        finally
        {
            reporter.Stopped();
        }
    }
}
