using SecPerf.Security.Tests.Fixtures;

namespace SecPerf.Security.Tests.Tests;

public class ControllersApiRateLimitTests : RateLimitTestsBase, IClassFixture<RateLimitControllersApiFactory>
{
    private readonly RateLimitControllersApiFactory _factory;
    public ControllersApiRateLimitTests(RateLimitControllersApiFactory factory) => _factory = factory;
    protected override HttpClient CreateClient() => _factory.CreateClient();
}
