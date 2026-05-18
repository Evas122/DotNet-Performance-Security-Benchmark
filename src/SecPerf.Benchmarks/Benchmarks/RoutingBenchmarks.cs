using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using SecPerf.Benchmarks.Helpers;

namespace SecPerf.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark 1 — routing resolution overhead.
///
/// Measures the time from HTTP request to response for an endpoint that does
/// nothing except return a timestamp (GET /health).  This isolates the routing
/// and middleware pipeline cost from any business / database work.
///
/// Minimal API  → MapGet("/health", () => Results.Ok(...))
/// Controller   → [HttpGet("/health")] IActionResult Health()
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class RoutingBenchmarks
{
    private WebApplicationFactory<SecPerf.ApiMinimal.MinimalApiMarker>?    _minimalFactory;
    private WebApplicationFactory<SecPerf.ApiMvc.ControllersApiMarker>? _controllersFactory;

    private HttpClient _minimalClient    = null!;
    private HttpClient _controllersClient = null!;

    [GlobalSetup]
    public void Setup()
    {
        _minimalFactory     = BenchmarkWebAppFactory.Create<SecPerf.ApiMinimal.MinimalApiMarker>("Bench_Routing_Minimal_" + Guid.NewGuid().ToString("N"));
        _controllersFactory = BenchmarkWebAppFactory.Create<SecPerf.ApiMvc.ControllersApiMarker>("Bench_Routing_Controllers_" + Guid.NewGuid().ToString("N"));

        _minimalClient     = _minimalFactory.CreateClient();
        _controllersClient = _controllersFactory.CreateClient();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        _minimalClient.Dispose();
        _controllersClient.Dispose();
        if (_minimalFactory     is not null) await _minimalFactory.DisposeAsync();
        if (_controllersFactory is not null) await _controllersFactory.DisposeAsync();
    }

    [Benchmark(Baseline = true, Description = "Minimal API — GET /health")]
    public async Task<string?> MinimalApi_Health()
    {
        var resp = await _minimalClient.GetAsync("/health");
        return await resp.Content.ReadAsStringAsync();
    }

    [Benchmark(Description = "Controller-based API — GET /health")]
    public async Task<string?> ControllersApi_Health()
    {
        var resp = await _controllersClient.GetAsync("/health");
        return await resp.Content.ReadAsStringAsync();
    }
}
