using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;

namespace SecPerf.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));

        // Register FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly);

        return services;
    }
}
