using System;
using SecPerf.Domain.Entities;

namespace SecPerf.Application.Dtos.User
{
    public record UserResponseDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = null!;
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public UserRole Role { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
