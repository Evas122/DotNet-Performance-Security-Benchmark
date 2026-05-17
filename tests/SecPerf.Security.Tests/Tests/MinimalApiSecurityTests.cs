using SecPerf.Security.Tests.Fixtures;

namespace SecPerf.Security.Tests.Tests;

public class MinimalApiSecurityTests : SecurityTestsBase, IClassFixture<MinimalApiFactory>
{
    private readonly MinimalApiFactory _factory;

    public MinimalApiSecurityTests(MinimalApiFactory factory) => _factory = factory;

    protected override HttpClient CreateClient() => _factory.CreateClient();
}
