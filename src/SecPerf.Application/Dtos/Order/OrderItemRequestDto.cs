using System;

namespace SecPerf.Application.Dtos.Order
{
    public record OrderItemRequestDto
    {
        public Guid ProductId { get; init; }
        public int Quantity { get; init; }
    }
}
