using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SecPerf.Security.Tests.Helpers;

public static class JwtTestHelper
{
    public static string CreateExpiredToken(Guid? userId = null)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtTestConstants.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:             JwtTestConstants.Issuer,
            audience:           JwtTestConstants.Audience,
            claims:             BuildClaims(userId),
            notBefore:          DateTime.UtcNow.AddHours(-2),
            expires:            DateTime.UtcNow.AddHours(-1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateInvalidSignatureToken(Guid? userId = null)
    {
        var wrongKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("wrong-signing-key-that-is-definitely-not-the-real-one!"));
        var creds = new SigningCredentials(wrongKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:             JwtTestConstants.Issuer,
            audience:           JwtTestConstants.Audience,
            claims:             BuildClaims(userId),
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateAlgNoneToken(Guid? userId = null)
    {
        var header  = Base64UrlEncode("""{"alg":"none","typ":"JWT"}""");
        var payload = new
        {
            sub = (userId ?? Guid.NewGuid()).ToString(),
            iss = JwtTestConstants.Issuer,
            aud = JwtTestConstants.Audience,
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
        var payloadJson    = System.Text.Json.JsonSerializer.Serialize(payload);
        var payloadEncoded = Base64UrlEncode(payloadJson);
        return $"{header}.{payloadEncoded}."; // empty signature = alg:none
    }

    public static string CreateWrongIssuerToken(Guid? userId = null)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtTestConstants.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:             "https://attacker.evil.com",   // wrong issuer, correct key
            audience:           JwtTestConstants.Audience,
            claims:             BuildClaims(userId),
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateWrongAudienceToken(Guid? userId = null)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtTestConstants.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:             JwtTestConstants.Issuer,
            audience:           "wrong-audience-not-accepted", // wrong audience, correct key
            claims:             BuildClaims(userId),
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateValidToken(Guid userId, string role = "Customer")
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtTestConstants.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer:             JwtTestConstants.Issuer,
            audience:           JwtTestConstants.Audience,
            claims:             BuildClaims(userId, role),
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static Claim[] BuildClaims(Guid? userId, string role = "Customer") =>
    [
        new Claim(JwtRegisteredClaimNames.Sub, (userId ?? Guid.NewGuid()).ToString()),
        new Claim(ClaimTypes.Role, role),
    ];

    private static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
