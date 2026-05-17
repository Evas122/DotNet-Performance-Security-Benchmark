using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SecPerf.Security.Tests.Helpers;

namespace SecPerf.Security.Tests.Fixtures;

/// <summary>
/// WebApplicationFactory for Controllers API with rate limiting enabled.
/// Uses a low login limit (5 req/s) so the 429 test is fast and deterministic.
/// </summary>
public class RateLimitControllersApiFactory : WebApplicationFactory<SecPerf.ApiMvc.ControllersApiMarker>
{
    private static readonly string _dbName = "TestDb_RateLimit_Controllers_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InMemoryDbName"]                      = _dbName,
                ["Jwt:Secret"]                          = JwtTestConstants.Secret,
                ["Jwt:Issuer"]                          = JwtTestConstants.Issuer,
                ["Jwt:Audience"]                        = JwtTestConstants.Audience,
                ["Jwt:AccessTokenExpirationMinutes"]    = "15",
                ["Jwt:RefreshTokenExpirationDays"]      = "7",
                ["ConnectionStrings:DefaultConnection"] = "InMemory",

                // Rate limiting enabled with tight login limit for testing
                ["IpRateLimiting:EnableEndpointRateLimiting"]  = "true",
                ["IpRateLimiting:StackBlockedRequests"]        = "false",
                ["IpRateLimiting:RealIpHeader"]                = "X-Real-IP",
                ["IpRateLimiting:ClientIdHeader"]              = "X-ClientId",
                ["IpRateLimiting:HttpStatusCode"]              = "429",
                ["IpRateLimiting:GeneralRules:0:Endpoint"]    = "post:/api/auth/login",
                ["IpRateLimiting:GeneralRules:0:Period"]       = "1s",
                ["IpRateLimiting:GeneralRules:0:Limit"]        = "5",
                ["IpRateLimiting:GeneralRules:1:Endpoint"]    = "*",
                ["IpRateLimiting:GeneralRules:1:Period"]       = "1s",
                ["IpRateLimiting:GeneralRules:1:Limit"]        = "10000",
            });
        });
    }
}
