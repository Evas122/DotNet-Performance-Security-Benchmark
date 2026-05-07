using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SecPerf.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Reuse existing registration implementation
        services.AddApplicationServices();
        return services;
    }
}
