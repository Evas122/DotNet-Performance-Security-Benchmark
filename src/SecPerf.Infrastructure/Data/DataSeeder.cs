using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecPerf.Domain.Entities;

namespace SecPerf.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, int usersCount = 1000, int productsCount = 10000, int ordersCount = 50000)
        {
            if (context.Users.Any() || context.Products.Any() || context.Orders.Any())
                return; // already seeded

            var rand = new Random(12345);

            // Seed users
            var users = new List<User>(usersCount);
            for (int i = 0; i < usersCount; i++)
            {
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = $"user{i}@example.com",
                    PasswordHash = "hashed_password",
                    FirstName = $"First{i}",
                    LastName = $"Last{i}",
                    Role = UserRole.Customer,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Seed categories
            var categories = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "Default", Description = "Default category" }
            };

            // create some more categories
            for (int i = 1; i < 20; i++)
            {
                categories.Add(new Category { Id = Guid.NewGuid(), Name = $"Category{i}", Description = $"Category number {i}" });
            }

            await context.Categories.AddRangeAsync(categories);
            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            // Seed products in batches
            var products = new List<Product>(productsCount);
            for (int i = 0; i < productsCount; i++)
            {
                var price = Math.Round((decimal)(rand.NextDouble() * 1000.0 + 1.0), 2);
                products.Add(new Product
                {
                    Id = Guid.NewGuid(),
                    Name = $"Product {i}",
                    Description = $"Description for product {i}",
                    Price = price,
                    Stock = rand.Next(0, 1000),
                    CategoryId = categories[rand.Next(categories.Count)].Id
                });

                if (products.Count >= 1000)
                {
                    await context.Products.AddRangeAsync(products);
                    await context.SaveChangesAsync();
                    products.Clear();
                }
            }

            if (products.Count > 0)
            {
                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
                products.Clear();
            }

            // Load product ids into array for faster access
            var productIds = await Task.Run(() => context.Products.Select(p => new { p.Id, p.Price }).ToArray());

            // Seed orders in batches
            var ordersBatch = new List<Order>(1000);
            var orderItemsBatch = new List<OrderItem>(5000);

            for (int i = 0; i < ordersCount; i++)
            {
                var user = users[rand.Next(users.Count)];
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-rand.Next(0, 60 * 24 * 365)),
                    Status = OrderStatus.Pending,
                    TotalPrice = 0m
                };

                int itemsCount = rand.Next(1, 6);
                decimal total = 0m;
                for (int it = 0; it < itemsCount; it++)
                {
                    var p = productIds[rand.Next(productIds.Length)];
                    var qty = rand.Next(1, 5);
                    var unitPrice = p.Price;
                    var oi = new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = p.Id,
                        Quantity = qty,
                        UnitPrice = unitPrice
                    };
                    orderItemsBatch.Add(oi);
                    total += unitPrice * qty;
                }

                order.TotalPrice = Math.Round(total, 2);
                ordersBatch.Add(order);

                if (ordersBatch.Count >= 1000)
                {
                    await context.Orders.AddRangeAsync(ordersBatch);
                    await context.OrderItems.AddRangeAsync(orderItemsBatch);
                    await context.SaveChangesAsync();
                    ordersBatch.Clear();
                    orderItemsBatch.Clear();
                }
            }

            if (ordersBatch.Count > 0)
            {
                await context.Orders.AddRangeAsync(ordersBatch);
                await context.OrderItems.AddRangeAsync(orderItemsBatch);
                await context.SaveChangesAsync();
                ordersBatch.Clear();
                orderItemsBatch.Clear();
            }
        }
    }
}
