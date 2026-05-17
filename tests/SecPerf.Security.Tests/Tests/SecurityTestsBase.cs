using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using SecPerf.Security.Tests.Helpers;

namespace SecPerf.Security.Tests.Tests;

/// <summary>
/// Shared JWT and auth security tests executed against both API surfaces.
/// Concrete subclasses bind a specific WebApplicationFactory via IClassFixture.
/// </summary>
public abstract class SecurityTestsBase
{
    protected abstract HttpClient CreateClient();

    // ── Protected endpoint used for 401/403 token tests ────────────────────
    private const string ProtectedPostEndpoint = "/api/products";
    private static HttpContent SampleProductPayload() =>
        JsonContent.Create(new
        {
            name        = "TestProduct",
            description = "desc",
            price       = 9.99m,
            stock       = 1,
            categoryId  = Guid.NewGuid()
        });

    // ── Register helpers ────────────────────────────────────────────────────

    private static int _counter;
    private static string UniqueEmail() =>
        $"user{System.Threading.Interlocked.Increment(ref _counter)}_{Guid.NewGuid():N}@test.com";

    private record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn);

    private async Task<AuthResponse> RegisterAsync(HttpClient client, string? email = null)
    {
        var req = new
        {
            email     = email ?? UniqueEmail(),
            firstName = "Test",
            lastName  = "User",
            password  = "Password123!"
        };
        var resp = await client.PostAsJsonAsync("/api/auth/register", req);
        resp.EnsureSuccessStatusCode();
        var auth = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // JWT — missing / invalid token → 401
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MissingToken_OnProtectedEndpoint_Returns401()
    {
        var client = CreateClient();
        var resp   = await client.PostAsync(ProtectedPostEndpoint, SampleProductPayload());
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredToken_OnProtectedEndpoint_Returns401()
    {
        var client = CreateClient();
        var token  = JwtTestHelper.CreateExpiredToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.PostAsync(ProtectedPostEndpoint, SampleProductPayload());
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidSignatureToken_OnProtectedEndpoint_Returns401()
    {
        var client = CreateClient();
        var token  = JwtTestHelper.CreateInvalidSignatureToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.PostAsync(ProtectedPostEndpoint, SampleProductPayload());
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AlgNoneToken_OnProtectedEndpoint_Returns401()
    {
        var client = CreateClient();
        var token  = JwtTestHelper.CreateAlgNoneToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.PostAsync(ProtectedPostEndpoint, SampleProductPayload());
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WrongIssuerToken_OnProtectedEndpoint_Returns401()
    {
        var client = CreateClient();
        var token  = JwtTestHelper.CreateWrongIssuerToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.PostAsync(ProtectedPostEndpoint, SampleProductPayload());
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WrongAudienceToken_OnProtectedEndpoint_Returns401()
    {
        var client = CreateClient();
        var token  = JwtTestHelper.CreateWrongAudienceToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.PostAsync(ProtectedPostEndpoint, SampleProductPayload());
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Cross-user resource access → 403
    // User A cannot revoke User B's refresh token
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RevokeOtherUserToken_Returns403()
    {
        var client = CreateClient();

        var authA = await RegisterAsync(client);
        var authB = await RegisterAsync(client);

        // User A tries to revoke User B's refresh token
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authA.AccessToken);

        var resp = await client.PostAsJsonAsync("/api/auth/revoke",
            new { refreshToken = authB.RefreshToken });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Refresh token replay attacks
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RevokedRefreshToken_OnRefreshEndpoint_Returns400()
    {
        var client = CreateClient();
        var auth   = await RegisterAsync(client);

        // Revoke own refresh token (requires auth)
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var revoke = await client.PostAsJsonAsync("/api/auth/revoke",
            new { refreshToken = auth.RefreshToken });
        revoke.StatusCode.Should().Be(HttpStatusCode.OK);

        // Replay the revoked token against /api/auth/refresh → must be rejected
        client.DefaultRequestHeaders.Authorization = null;
        var replay = await client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = auth.RefreshToken });
        replay.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "a revoked refresh token must never be reusable");
    }

    [Fact]
    public async Task RefreshTokenRotation_OldTokenIsInvalidAfterRefresh()
    {
        var client = CreateClient();
        var auth   = await RegisterAsync(client);

        // First refresh — old token T1 is rotated, T2 is issued
        var firstRefresh = await client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = auth.RefreshToken });
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to use T1 again — must be rejected (token rotation)
        var replay = await client.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken = auth.RefreshToken });
        replay.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "after token rotation the old refresh token must be invalidated");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Security headers — NWebsec middleware must set all five headers
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Response_ContainsExpectedSecurityHeaders()
    {
        var client = CreateClient();
        var resp   = await client.GetAsync("/api/products");

        // UseXContentTypeOptions()
        resp.Headers.Should().ContainKey("X-Content-Type-Options");
        resp.Headers.GetValues("X-Content-Type-Options")
            .Should().Contain("nosniff");

        // UseXfo(options => options.Deny())
        resp.Headers.Should().ContainKey("X-Frame-Options");
        resp.Headers.GetValues("X-Frame-Options")
            .Should().ContainMatch("*eny*",  // NWebsec sends "Deny" (title-case)
                because: "X-Frame-Options must be set to DENY to prevent clickjacking");

        // UseXXssProtection(options => options.EnabledWithBlockMode())
        resp.Headers.Should().ContainKey("X-XSS-Protection");

        // UseReferrerPolicy(opts => opts.NoReferrer())
        resp.Headers.Should().ContainKey("Referrer-Policy");
        resp.Headers.GetValues("Referrer-Policy")
            .Should().Contain("no-referrer");

        // UseCsp(...)
        resp.Headers.Should().ContainKey("Content-Security-Policy");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // User enumeration protection — same error for unknown email vs wrong password
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "nonexistent@example.com",
            password = "AnyPassword123!"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = CreateClient();
        await RegisterAsync(client, "known@test.com");
        var resp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "known@test.com",
            password = "WrongPassword!"
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_ReturnSameStatusCode()
    {
        var client = CreateClient();
        await RegisterAsync(client, "enumtest@test.com");

        var unknownResp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "doesnotexist@test.com",
            password = "AnyPassword123!"
        });

        var wrongPassResp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "enumtest@test.com",
            password = "WrongPassword!"
        });

        unknownResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        wrongPassResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Both must return the same HTTP status — no information leakage
        unknownResp.StatusCode.Should().Be(wrongPassResp.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Password storage — BCrypt, not plain text
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void PasswordHasher_ProducesBCryptHash_NotPlainText()
    {
        var hasher   = new SecPerf.Infrastructure.Services.PasswordHasher();
        const string password = "SecureTestPassword123!";

        var hash = hasher.Hash(password);

        hash.Should().StartWith("$2",  because: "BCrypt hashes begin with $2a$ or $2b$");
        hash.Should().NotContain(password, because: "plain text must never appear in the hash");
        hasher.Verify(password, hash).Should()
            .BeTrue(because: "the correct password must verify against its own hash");
        hasher.Verify("WrongPassword!", hash).Should()
            .BeFalse(because: "a different password must not verify");
    }
}
