using System;
using System.Collections.Generic;
using SecPerf.Domain.Entities;

namespace SecPerf.Application.Dtos.Order
{
    public record OrderResponseDto
    {
        public Guid Id { get; init; }
        // nullable because principal (User) can be soft-deleted / detached
        public Guid? UserId { get; init; }
        public DateTime CreatedAt { get; init; }
        public OrderStatus Status { get; init; }
        public decimal TotalPrice { get; init; }
        public List<OrderItemResponseDto> Items { get; init; } = new List<OrderItemResponseDto>();
    }
}
