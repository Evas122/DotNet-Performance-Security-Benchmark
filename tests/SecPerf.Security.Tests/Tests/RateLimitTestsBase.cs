using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SecPerf.Security.Tests.Tests;

/// <summary>
/// Rate limiting tests executed against both API surfaces.
/// The factory injected by concrete subclasses must have rate limiting enabled
/// with a login limit of 5 req/s (see RateLimitMinimalApiFactory / RateLimitControllersApiFactory).
/// </summary>
public abstract class RateLimitTestsBase
{
    protected abstract HttpClient CreateClient();

    // ═══════════════════════════════════════════════════════════════════════
    // Burst 11 POST /api/auth/login requests from same IP → 429
    //
    // The factory sets limit = 5 req/s on "post:/api/auth/login".
    // X-Real-IP is fixed so AspNetCoreRateLimit has a stable key even when
    // TestServer exposes no real network connection (RemoteIpAddress = null).
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task BurstLogin_ExceedsRateLimit_Returns429()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Real-IP", "10.10.10.1");

        var payload = new { email = "burst@test.com", password = "AnyPassword123!" };

        var statuses = new List<HttpStatusCode>();
        for (int i = 0; i < 11; i++)
        {
            var resp = await client.PostAsJsonAsync("/api/auth/login", payload);
            statuses.Add(resp.StatusCode);
        }

        statuses.Should().Contain(HttpStatusCode.TooManyRequests,
            because: "after 5 requests within 1 s the rate limiter must block further login attempts with 429");
    }
}
