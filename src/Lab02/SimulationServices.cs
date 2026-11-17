using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharpLabs.Core;

namespace Lab02;

public static class SimulationServices
{
    public static IServiceCollection AddSimulationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<Lab02Options>()
            .Bind(configuration.GetRequiredSection("Simulation"))
            .Validate(options => options.IsValid(),
                "Проверьте Simulation: интервалы, горизонт планирования и награды должны быть положительными.")
            .ValidateOnStart();

        services.AddSingleton(provider => provider.GetRequiredService<IOptions<Lab02Options>>()
            .Value.ToSimulationSettings());
        services.AddSingleton<SeededRandom>();
        services.AddSingleton<ITeacherService>(provider =>
            new TeacherService(provider.GetRequiredService<SeededRandom>()));
        services.AddSingleton<IStudentHistoryStore, StudentHistoryStore>();
        services.AddSingleton<ISkipStrategy>(provider =>
            new BayesianStrategy(provider.GetRequiredService<SimulationSettings>(), continuous: true));
        services.AddSingleton<IDaySimulation, DaySimulation>();
        services.AddSingleton<IDayClock, PeriodicDayClock>();
        services.AddSingleton<IKeyInput, ConsoleKeyInput>();
        services.AddSingleton<TextWriter>(Console.Out);
        services.AddSingleton<IProgressReporter, ConsoleProgressReporter>();
        services.AddHostedService<SimulationWorker>();
        services.AddHostedService<KeyboardStopService>();
        return services;
    }
}
