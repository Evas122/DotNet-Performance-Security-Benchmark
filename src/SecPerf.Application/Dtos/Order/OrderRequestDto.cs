using System;
using System.Collections.Generic;

namespace SecPerf.Application.Dtos.Order
{
    public record OrderRequestDto
    {
        public Guid UserId { get; init; }
        public List<OrderItemRequestDto> Items { get; init; } = new List<OrderItemRequestDto>();
    }
}
