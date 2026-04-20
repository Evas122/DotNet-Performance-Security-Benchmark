using System;

namespace SecPerf.Application.Dtos.Category
{
    public record CategoryRequestDto
    {
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
    }
}
