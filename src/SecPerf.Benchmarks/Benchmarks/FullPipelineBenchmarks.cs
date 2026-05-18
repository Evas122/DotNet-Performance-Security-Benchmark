using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using SecPerf.Benchmarks.Helpers;

namespace SecPerf.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark 2 — full in-process request pipeline.
///
/// Measures end-to-end latency of GET /api/products: routing → middleware stack
/// (JWT, NWebsec, rate-limit) → MediatR dispatch → EF Core InMemory query →
/// AutoMapper → JSON serialization → response.
///
/// Both factories share the same InMemory database name so they read the
/// same (empty) product catalogue — the query cost is identical for both.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class FullPipelineBenchmarks
{
    private WebApplicationFactory<SecPerf.ApiMinimal.MinimalApiMarker>?    _minimalFactory;
    private WebApplicationFactory<SecPerf.ApiMvc.ControllersApiMarker>? _controllersFactory;

    private HttpClient _minimalClient     = null!;
    private HttpClient _controllersClient = null!;

    [GlobalSetup]
    public void Setup()
    {
        _minimalFactory     = BenchmarkWebAppFactory.Create<SecPerf.ApiMinimal.MinimalApiMarker>("Bench_Pipeline_Minimal_" + Guid.NewGuid().ToString("N"));
        _controllersFactory = BenchmarkWebAppFactory.Create<SecPerf.ApiMvc.ControllersApiMarker>("Bench_Pipeline_Controllers_" + Guid.NewGuid().ToString("N"));

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

    [Benchmark(Baseline = true, Description = "Minimal API — GET /api/products")]
    public async Task<System.Net.HttpStatusCode> MinimalApi_GetProducts()
    {
        var resp = await _minimalClient.GetAsync("/api/products");
        return resp.StatusCode;
    }

    [Benchmark(Description = "Controller-based API — GET /api/products")]
    public async Task<System.Net.HttpStatusCode> ControllersApi_GetProducts()
    {
        var resp = await _controllersClient.GetAsync("/api/products");
        return resp.StatusCode;
    }
}
