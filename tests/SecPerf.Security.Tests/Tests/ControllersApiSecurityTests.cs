using SecPerf.Security.Tests.Fixtures;

namespace SecPerf.Security.Tests.Tests;

public class ControllersApiSecurityTests : SecurityTestsBase, IClassFixture<ControllersApiFactory>
{
    private readonly ControllersApiFactory _factory;

    public ControllersApiSecurityTests(ControllersApiFactory factory) => _factory = factory;

    protected override HttpClient CreateClient() => _factory.CreateClient();
}
