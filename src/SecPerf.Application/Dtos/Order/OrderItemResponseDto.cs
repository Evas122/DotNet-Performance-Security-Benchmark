using System;

namespace SecPerf.Application.Dtos.Order
{
    public record OrderItemResponseDto
    {
        public Guid Id { get; init; }
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
    }
}
