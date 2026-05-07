using System;
using System.Collections.Generic;

namespace SecPerf.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }

        // FK
        public Guid CategoryId { get; set; }
        public Category? Category { get; set; }

        // Soft delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Domain logic
        public void DecreaseStock(int amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
            if (amount > Stock) throw new InvalidOperationException("Insufficient stock");
            Stock -= amount;
        }

        public void IncreaseStock(int amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
            Stock += amount;
        }

        public void SoftDelete()
        {
            if (!IsDeleted)
            {
                IsDeleted = true;
                DeletedAt = DateTime.UtcNow;
            }
        }

        public void Restore()
        {
            if (IsDeleted)
            {
                IsDeleted = false;
                DeletedAt = null;
            }
        }
    }
}
