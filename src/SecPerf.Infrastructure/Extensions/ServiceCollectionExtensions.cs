using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SecPerf.Infrastructure.Data;

namespace SecPerf.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure DbContext - use DefaultConnection from configuration or a sensible LocalDB fallback
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=SecPerfDb;Trusted_Connection=True;MultipleActiveResultSets=true";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // TODO: register repositories / infrastructure services

        return services;
    }
}
