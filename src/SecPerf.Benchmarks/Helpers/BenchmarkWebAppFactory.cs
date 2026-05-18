using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SecPerf.Benchmarks.Helpers;

/// <summary>
/// Creates a WebApplicationFactory wired to EF Core InMemory and with rate
/// limiting disabled — identical to the security-test factories but shared
/// across all benchmark classes.
/// </summary>
public static class BenchmarkWebAppFactory
{
    private static readonly Dictionary<string, string?> BaseConfig = new()
    {
        ["ConnectionStrings:DefaultConnection"]          = "InMemory",
        ["Jwt:Secret"]                                   = "benchmark-secret-key-min-32-characters!!",
        ["Jwt:Issuer"]                                   = "secperf-benchmark",
        ["Jwt:Audience"]                                 = "secperf-benchmark-users",
        ["Jwt:AccessTokenExpirationMinutes"]             = "15",
        ["Jwt:RefreshTokenExpirationDays"]               = "7",
        ["IpRateLimiting:EnableEndpointRateLimiting"]    = "false",
        ["IpRateLimiting:StackBlockedRequests"]          = "false",
        ["IpRateLimiting:RealIpHeader"]                  = "X-Real-IP",
        ["IpRateLimiting:ClientIdHeader"]                = "X-ClientId",
        ["IpRateLimiting:HttpStatusCode"]                = "429",
        ["IpRateLimiting:GeneralRules:0:Endpoint"]       = "*",
        ["IpRateLimiting:GeneralRules:0:Period"]         = "1s",
        ["IpRateLimiting:GeneralRules:0:Limit"]          = "100000",
    };

    public static WebApplicationFactory<TMarker> Create<TMarker>(string dbName)
        where TMarker : class
    {
        return new WebApplicationFactory<TMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration(cfg =>
                {
                    var config = new Dictionary<string, string?>(BaseConfig)
                    {
                        ["InMemoryDbName"] = dbName
                    };
                    cfg.AddInMemoryCollection(config);
                });
            });
    }
}
