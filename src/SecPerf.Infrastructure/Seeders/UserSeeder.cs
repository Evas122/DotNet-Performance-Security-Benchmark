using Bogus;
using SecPerf.Domain.Entities;
using SecPerf.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecPerf.Infrastructure.Seeders;

public static class UserSeeder
{
    public static async Task<List<User>> SeedAsync(ApplicationDbContext context, int count = 10000)
    {
        if (await context.Users.AnyAsync())
            return await context.Users.ToListAsync();

        // one hash for all users to speed up seeding, work factor 4 for very fast hashing (not secure, but this is just test data)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Test123!", workFactor: 4);

        // Ensure emails are unique by including a short GUID token — Bogus' Email() can produce duplicates at scale
        var faker = new Faker<User>(locale: "en")
            .UseSeed(42)
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.Email, f => $"{f.Internet.UserName()}.{Guid.NewGuid():N}@{f.Internet.DomainName()}")
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.PasswordHash, _ => passwordHash) // ten sam hash
            .RuleFor(u => u.Role, f => UserRole.Customer)
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(2));

        var users = faker.Generate(count);

        foreach (var batch in users.Chunk(1000))
        {
            await context.Users.AddRangeAsync(batch);
            await context.SaveChangesAsync();
            // Clear change tracker to release memory and avoid tracking overhead for large seed operations
            context.ChangeTracker.Clear();
        }

        return users;
    }
}