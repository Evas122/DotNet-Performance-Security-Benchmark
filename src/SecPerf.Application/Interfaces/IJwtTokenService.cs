using System;
using SecPerf.Domain.Entities;

namespace SecPerf.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(User user, out DateTime expiresAt);
        (string Token, DateTime ExpiresAt) GenerateRefreshToken();
        bool ValidateAccessToken(string token);
    }
}
