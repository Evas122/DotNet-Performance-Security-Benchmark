using System;

namespace SecPerf.Application.Dtos.Category
{
    public record CategoryResponseDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
    }
}
