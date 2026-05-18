using Bogus;
using SecPerf.Domain.Entities;
using SecPerf.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecPerf.Infrastructure.Seeders;

public static class ProductSeeder
{
    public static async Task<List<Product>> SeedAsync(ApplicationDbContext context, List<Category> categories, int count = 10000)
    {
        if (await context.Products.AnyAsync())
            return await context.Products.ToListAsync();

        var categoryIds = categories.Select(c => c.Id).ToList();

        var faker = new Faker<Product>(locale: "en")
            .UseSeed(42)
            .RuleFor(p => p.Id, _ => Guid.NewGuid())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            // Use Random.Decimal to avoid localization issues from parsing formatted strings
            .RuleFor(p => p.Price, f => Math.Round(f.Random.Decimal(10m, 5000m), 2))
            .RuleFor(p => p.Stock, f => f.Random.Int(0, 500))
            .RuleFor(p => p.CategoryId, f => f.PickRandom(categoryIds));

        var products = faker.Generate(count);
        foreach (var batch in products.Chunk(1000))
        {
            await context.Products.AddRangeAsync(batch);
            await context.SaveChangesAsync();
            // Clear change tracker to free tracked entities and reduce memory usage
            context.ChangeTracker.Clear();
        }

        return products;
    }
}