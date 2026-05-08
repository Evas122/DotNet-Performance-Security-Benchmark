using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SecPerf.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddApplicationServices();
        return services;
    }
}
