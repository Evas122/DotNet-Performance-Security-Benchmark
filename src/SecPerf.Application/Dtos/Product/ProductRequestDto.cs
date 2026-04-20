using System;

namespace SecPerf.Application.Dtos.Product
{
    public record ProductRequestDto
    {
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public int Stock { get; init; }
        public Guid CategoryId { get; init; }
    }
}
