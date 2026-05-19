using System;
using System.Collections.Generic;
using SecPerf.Domain.Entities;

namespace SecPerf.Application.Dtos.Order
{
    public record OrderDto
    {
        public Guid Id { get; init; }
        public Guid? UserId { get; init; }
        public DateTime CreatedAt { get; init; }
        public OrderStatus Status { get; init; }
        public decimal TotalPrice { get; init; }
        public IEnumerable<OrderItemDto> Items { get; init; } = [];
    }
}
