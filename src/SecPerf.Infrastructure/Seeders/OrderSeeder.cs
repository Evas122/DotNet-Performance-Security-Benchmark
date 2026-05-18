using Bogus;
using SecPerf.Domain.Entities;
using SecPerf.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecPerf.Infrastructure.Seeders;

public static class OrderSeeder
{
    // Generate `count` orders (default 100k)
    public static async Task SeedAsync(ApplicationDbContext context, List<User> users, List<Product> products, int count = 100000)
    {
        if (await context.Orders.AnyAsync()) return;

        var random = new Random(42);
        var orderFaker = new Faker<Order>(locale: "en")
            .UseSeed(42)
            .RuleFor(o => o.Id, _ => Guid.NewGuid())
            .RuleFor(o => o.UserId, f => f.PickRandom(users).Id)
            .RuleFor(o => o.CreatedAt, f => f.Date.Past(2))
            .RuleFor(o => o.Status, f => f.PickRandom<OrderStatus>());

        var orders = orderFaker.Generate(count);

        // Use slightly larger batches for fewer roundtrips; change if memory becomes an issue
        foreach (var batch in orders.Chunk(2000))
        {
            foreach (var order in batch)
            {
                var itemCount = random.Next(1, 6);
                var items = new List<OrderItem>();

                for (int i = 0; i < itemCount; i++)
                {
                    var product = products[random.Next(products.Count)];
                    items.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = random.Next(1, 10),
                        UnitPrice = product.Price
                    });
                }

                order.TotalPrice = items.Sum(i => i.Quantity * i.UnitPrice);
                order.OrderItems = items;
            }

            await context.Orders.AddRangeAsync(batch);
            await context.SaveChangesAsync();
            // Clear change tracker to avoid keeping all created entities in memory
            context.ChangeTracker.Clear();
        }
    }
}
