using System;
using Microsoft.EntityFrameworkCore;
using SecPerf.Domain.Entities;

namespace SecPerf.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<SecPerf.Domain.Entities.RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User
        builder.Entity<User>(b => {
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(320);
            b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
            b.Property(u => u.FirstName).IsRequired().HasMaxLength(200);
            b.Property(u => u.LastName).IsRequired().HasMaxLength(200);
            b.Property(u => u.CreatedAt).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
            b.HasQueryFilter(u => !u.IsDeleted);
            // Make relationship optional to avoid issues when User is filtered by global query filter
            b.HasMany(u => u.Orders).WithOne(o => o.User).HasForeignKey(o => o.UserId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });

        // Category
        builder.Entity<Category>(b => {
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).IsRequired().HasMaxLength(200);
            b.HasIndex(c => c.Name).IsUnique();
            b.HasMany(c => c.Products).WithOne(p => p.Category).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        // Product
        builder.Entity<Product>(b => {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Description).HasMaxLength(2000);
            b.Property(p => p.Price).HasColumnType("decimal(18,2)").IsRequired();
            b.Property(p => p.Stock).IsRequired();
            b.HasIndex(p => p.CategoryId);
            b.HasQueryFilter(p => !p.IsDeleted);
            // Make relationship optional to avoid issues when Product is filtered by global query filter
            b.HasMany(p => p.OrderItems).WithOne(oi => oi.Product).HasForeignKey(oi => oi.ProductId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        });

        // Order
        builder.Entity<Order>(b => {
            b.HasKey(o => o.Id);
            b.Property(o => o.CreatedAt).IsRequired();
            b.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");
            b.HasIndex(o => o.UserId);
            b.HasMany(o => o.OrderItems).WithOne(oi => oi.Order).HasForeignKey(oi => oi.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem
        builder.Entity<OrderItem>(b => {
            b.HasKey(oi => oi.Id);
            b.Property(oi => oi.Quantity).IsRequired();
            b.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
            b.HasIndex(oi => oi.OrderId);
            b.HasIndex(oi => oi.ProductId);
        });

        // RefreshToken
        builder.Entity<SecPerf.Domain.Entities.RefreshToken>(b => {
            b.HasKey(t => t.Id);
            b.Property(t => t.Token).IsRequired().HasMaxLength(512);
            b.Property(t => t.CreatedAt).IsRequired();
            b.Property(t => t.ExpiresAt).IsRequired();
            b.HasIndex(t => t.Token).IsUnique();
            b.HasIndex(t => t.UserId);
            b.HasQueryFilter(rt => !rt.User.IsDeleted);
            b.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
