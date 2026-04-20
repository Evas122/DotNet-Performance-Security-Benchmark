using System;
using SecPerf.Domain.Entities;

namespace SecPerf.Application.Dtos.User
{
    public record UserRequestDto
    {
        public string Email { get; init; } = null!;
        public string Password { get; init; } = null!;
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public UserRole Role { get; init; } = UserRole.Customer;
    }
}
