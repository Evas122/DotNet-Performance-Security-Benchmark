using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using SecPerf.Benchmarks.Helpers;

namespace SecPerf.Benchmarks.Benchmarks;

/// <summary>
/// Mierzy czas cold-start obu API — od new WebApplicationFactory()
/// do pierwszego przetworzonego żądania HTTP.
///
/// Minimal API powinien startować szybciej ze względu na brak refleksji MVC,
/// brak skanowania atrybutów kontrolerów i uproszczony pipeline DI.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class StartupBenchmarks
{
    [Benchmark(Baseline = true, Description = "Minimal API — cold start")]
    public async Task<string?> MinimalApi_ColdStart()
    {
        await using var factory = BenchmarkWebAppFactory
            .Create<SecPerf.ApiMinimal.MinimalApiMarker>(
                "Bench_Startup_Min_" + Guid.NewGuid().ToString("N"));

        var client = factory.CreateClient();
        var resp   = await client.GetAsync("/health");
        return await resp.Content.ReadAsStringAsync();
    }

    [Benchmark(Description = "Controller-based API — cold start")]
    public async Task<string?> ControllersApi_ColdStart()
    {
        await using var factory = BenchmarkWebAppFactory
            .Create<SecPerf.ApiMvc.ControllersApiMarker>(
                "Bench_Startup_Ctrl_" + Guid.NewGuid().ToString("N"));

        var client = factory.CreateClient();
        var resp   = await client.GetAsync("/health");
        return await resp.Content.ReadAsStringAsync();
    }
}
