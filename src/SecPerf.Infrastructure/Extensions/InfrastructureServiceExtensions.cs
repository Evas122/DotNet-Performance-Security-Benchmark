using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AspNetCoreRateLimit;

namespace SecPerf.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Reuse existing registration (DbContext, repositories, jwt service)
        services.AddInfrastructureServices(configuration);

        // Rate limiting
        services.AddOptions();
        services.AddMemoryCache();

        // Load general configuration from appsettings: IpRateLimiting and IpRateLimitPolicies
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));

        // In-memory rate limiting stores
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }
}
