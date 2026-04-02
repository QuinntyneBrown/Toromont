using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IronvaleFleetHub.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<ITelemetryRepository, DapperTelemetryRepository>();
        services.AddSingleton<IDeadLetterService, DeadLetterService>();
    })
    .Build();

host.Run();
