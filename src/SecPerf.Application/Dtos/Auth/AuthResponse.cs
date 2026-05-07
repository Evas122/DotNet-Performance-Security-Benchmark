using System;

namespace SecPerf.Application.Dtos.Auth
{
    public record AuthResponse
    {
        public string AccessToken { get; init; } = null!;
        public string RefreshToken { get; init; } = null!;
        public int ExpiresIn { get; init; }
    }
}
