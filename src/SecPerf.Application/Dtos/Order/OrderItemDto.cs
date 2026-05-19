using System;

namespace SecPerf.Application.Dtos.Order
{
    public record OrderItemDto
    {
        public Guid Id { get; init; }
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = null!;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal { get; init; }
    }
}
