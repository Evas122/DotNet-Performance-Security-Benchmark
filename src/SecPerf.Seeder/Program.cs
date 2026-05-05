using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecPerf.Infrastructure.Extensions;
using SecPerf.Infrastructure.Seeders;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        cfg.AddEnvironmentVariables();
        cfg.AddCommandLine(args);
    })
    .ConfigureServices((ctx, services) =>
    {
        // Register infrastructure (DbContext, repositories)
        services.AddInfrastructureServices(ctx.Configuration);
    })
    .Build();

try
{
    await host.StartAsync();
    Console.WriteLine("Starting data seeding...");
    await DataSeeder.SeedAsync(host.Services);
    Console.WriteLine("Seeding finished.");
}
finally
{
    await host.StopAsync();
}
