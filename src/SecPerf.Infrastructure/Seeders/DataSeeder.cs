using Microsoft.Extensions.DependencyInjection;
using SecPerf.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecPerf.Infrastructure.Seeders;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var categories = await CategorySeeder.SeedAsync(context);
        var users = await UserSeeder.SeedAsync(context);
        var products = await ProductSeeder.SeedAsync(context, categories);
        await OrderSeeder.SeedAsync(context, users, products);
    }
}