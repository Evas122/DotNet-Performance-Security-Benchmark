using Bogus;
using SecPerf.Domain.Entities;
using SecPerf.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecPerf.Infrastructure.Seeders;

public static class CategorySeeder
{
    private static readonly string[] Categories = new[]
    {
        "Electronics", "Computers", "Smartphones", "Audio & Video",
        "Home & Garden", "Kitchen", "Furniture", "Bedding",
        "Sports & Outdoors", "Fitness", "Camping", "Cycling",
        "Clothing", "Shoes", "Accessories", "Jewelry",
        "Books", "Music", "Movies", "Games",
        "Beauty & Health", "Vitamins", "Personal Care",
        "Toys & Kids", "Baby", "Pet Supplies", "Automotive"
    };

    public static async Task<List<Category>> SeedAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return await context.Categories.ToListAsync();

        var categories = Categories.Select(name => new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = new Faker().Commerce.ProductDescription()
        }).ToList();

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
        // Clear change tracker after seeding categories
        context.ChangeTracker.Clear();
        return categories;
    }
}