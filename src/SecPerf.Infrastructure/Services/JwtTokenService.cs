using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecPerf.Application.Interfaces;
using SecPerf.Domain.Entities;

namespace SecPerf.Infrastructure.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _config;
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenMinutes;
        private readonly int _refreshTokenDays;

        public JwtTokenService(IConfiguration config)
        {
            _config = config;
            _key = _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured in configuration (appsettings.json or environment variables)");
            if (_key.Length < 32)
                throw new InvalidOperationException("Jwt:Secret must be at least 32 characters long");

            _issuer = _config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured in configuration (appsettings.json or environment variables)");
            _audience = _config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured in configuration (appsettings.json or environment variables)");
            _accessTokenMinutes = int.TryParse(_config["Jwt:AccessTokenExpirationMinutes"], out var m) ? m : 15; // default 15 minutes
            _refreshTokenDays = int.TryParse(_config["Jwt:RefreshTokenExpirationDays"], out var d) ? d : 7; // default 7 days
        }

        public string GenerateAccessToken(User user, out DateTime expiresAt)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            expiresAt = DateTime.UtcNow.AddMinutes(_accessTokenMinutes);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public (string Token, DateTime ExpiresAt) GenerateRefreshToken()
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var expiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays);
            return (token, expiresAt);
        }

        public bool ValidateAccessToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return validatedToken is JwtSecurityToken;
            }
            catch
            {
                return false;
            }
        }
    }
}
