using SecPerf.Security.Tests.Fixtures;

namespace SecPerf.Security.Tests.Tests;

public class MinimalApiRateLimitTests : RateLimitTestsBase, IClassFixture<RateLimitMinimalApiFactory>
{
    private readonly RateLimitMinimalApiFactory _factory;
    public MinimalApiRateLimitTests(RateLimitMinimalApiFactory factory) => _factory = factory;
    protected override HttpClient CreateClient() => _factory.CreateClient();
}
