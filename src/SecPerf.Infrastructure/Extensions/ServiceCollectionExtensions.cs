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
        // Configure DbContext — the (serviceProvider, options) overload defers config reads to runtime
        // so WebApplicationFactory configuration overrides are visible when the context is first created.
        // Pass "InMemory" as the connection string (or leave it empty) to activate the InMemory provider.
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var cfg  = sp.GetRequiredService<IConfiguration>();
            var conn = cfg.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(conn) || conn.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                var dbName = cfg["InMemoryDbName"] ?? "SecPerfTestDb";
                options.UseInMemoryDatabase(dbName);
            }
            else
            {
                options.UseSqlServer(conn);
            }
        });

        // register repositories / infrastructure services
        services.AddScoped<IUserRepository, SecPerf.Infrastructure.Repositories.UserRepository>();
        services.AddScoped<IProductRepository, SecPerf.Infrastructure.Repositories.ProductRepository>();
        services.AddScoped<ICategoryRepository, SecPerf.Infrastructure.Repositories.CategoryRepository>();
        services.AddScoped<IOrderRepository, SecPerf.Infrastructure.Repositories.OrderRepository>();
        services.AddScoped<IRefreshTokenRepository, SecPerf.Infrastructure.Repositories.RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, SecPerf.Infrastructure.Repositories.UnitOfWork>();

        // JWT token service
        services.AddScoped<SecPerf.Application.Interfaces.IJwtTokenService, SecPerf.Infrastructure.Services.JwtTokenService>();
        services.AddScoped<SecPerf.Application.Interfaces.IPasswordHasher, SecPerf.Infrastructure.Services.PasswordHasher>();

        return services;
    }
}
