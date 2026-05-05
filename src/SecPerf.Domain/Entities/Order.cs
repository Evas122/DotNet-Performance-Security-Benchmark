using System;
using System.Collections.Generic;

namespace SecPerf.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }

        // FK (nullable to avoid issues with global query filters on User)
        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalPrice { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
