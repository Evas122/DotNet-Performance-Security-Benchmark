using System;
using System.Collections.Generic;

namespace SecPerf.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }

        // FK
        public Guid UserId { get; set; }
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalPrice { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
