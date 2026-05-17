using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SecPerf.Security.Tests.Fixtures;

public class MinimalApiFactory : WebApplicationFactory<SecPerf.ApiMinimal.MinimalApiMarker>
{
    private static readonly string _dbName = "TestDb_Minimal_" + Guid.NewGuid().ToString("N");

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
                // "InMemory" sentinel triggers EF Core InMemory provider in ServiceCollectionExtensions
                ["ConnectionStrings:DefaultConnection"] = "InMemory",
                // Permissive rate-limiting so tests never get throttled
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "false",
                ["IpRateLimiting:StackBlockedRequests"]       = "false",
                ["IpRateLimiting:RealIpHeader"]               = "X-Real-IP",
                ["IpRateLimiting:ClientIdHeader"]             = "X-ClientId",
                ["IpRateLimiting:HttpStatusCode"]             = "429",
                ["IpRateLimiting:GeneralRules:0:Endpoint"]   = "*",
                ["IpRateLimiting:GeneralRules:0:Period"]      = "1s",
                ["IpRateLimiting:GeneralRules:0:Limit"]       = "100000",
            });
        });
    }
}
