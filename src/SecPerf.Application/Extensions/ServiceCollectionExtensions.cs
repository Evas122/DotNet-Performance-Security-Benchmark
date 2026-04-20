using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;
using SecPerf.Application.Services;
using SecPerf.Application.Services.Impl;

namespace SecPerf.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));

        // Register FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly);

        // Register AutoMapper profiles from this assembly
        services.AddAutoMapper(typeof(AssemblyReference).Assembly);

        // Register application services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
