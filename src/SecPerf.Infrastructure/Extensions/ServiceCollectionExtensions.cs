using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SecPerf.Infrastructure.Data;
using SecPerf.Domain.Repositories;

namespace SecPerf.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure DbContext - use DefaultConnection from configuration or a sensible LocalDB fallback
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // register repositories / infrastructure services
        services.AddScoped<IUserRepository, SecPerf.Infrastructure.Repositories.UserRepository>();
        services.AddScoped<IProductRepository, SecPerf.Infrastructure.Repositories.ProductRepository>();
        services.AddScoped<ICategoryRepository, SecPerf.Infrastructure.Repositories.CategoryRepository>();
        services.AddScoped<IOrderRepository, SecPerf.Infrastructure.Repositories.OrderRepository>();

        return services;
    }
}
