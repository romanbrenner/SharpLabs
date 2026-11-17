using System.Text;
using Lab02;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
{
    Console.WriteLine("Запуск: dotnet run --project src/Lab02 -- [--seed целое_число]");
    Console.WriteLine("Настройки: appsettings.json, переменные Simulation__... или --Simulation:... значение.");
    return 0;
}

try
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory
    });
    builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
    {
        ["--seed"] = "Simulation:Seed"
    });
    builder.Services.AddSimulationServices(builder.Configuration);

    using IHost host = builder.Build();
    await host.RunAsync();
    return 0;
}
catch (Exception exception) when (exception is OptionsValidationException or IOException
                                 or InvalidOperationException or ArgumentException or FormatException)
{
    Console.Error.WriteLine($"Ошибка запуска: {exception.Message}");
    return 1;
}
