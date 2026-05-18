using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using SecPerf.Application.Dtos.Product;

namespace SecPerf.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark 3 — JSON serialization cost comparison.
///
/// Isolates System.Text.Json serialization from the rest of the pipeline so
/// we can attribute throughput differences between the two APIs to their
/// serializer configuration, not routing or DB access.
///
/// Options compared:
///   • Default        — JsonSerializerOptions.Default (no custom policy)
///   • CamelCase      — PropertyNamingPolicy = CamelCase  (Minimal API default)
///   • CamelCase+Null — CamelCase + IgnoreNullValues      (Controllers API config)
///
/// The payload is a list of 50 ProductResponseDto instances — realistic size
/// for a paginated product listing response.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class JsonSerializationBenchmarks
{
    private List<ProductResponseDto> _products = [];

    // Serializer option sets matching each API's configuration
    private static readonly JsonSerializerOptions DefaultOptions = new();

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions CamelCaseIgnoreNullOptions = new()
    {
        PropertyNamingPolicy         = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition       = JsonIgnoreCondition.WhenWritingNull
    };

    [Params(10, 50, 200)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _products = Enumerable.Range(1, 200).Select(i => new ProductResponseDto
        {
            Id          = Guid.NewGuid(),
            Name        = $"Product {i}",
            Description = i % 3 == 0 ? null : $"Description for product {i}",
            Price       = Math.Round((decimal)(i * 1.99), 2),
            Stock       = i * 10,
            CategoryId  = Guid.NewGuid()
        }).ToList();
    }

    [Benchmark(Baseline = true, Description = "Default options")]
    public string Default_Serialize()
        => JsonSerializer.Serialize(_products.Take(ItemCount), DefaultOptions);

    [Benchmark(Description = "CamelCase (Minimal API)")]
    public string CamelCase_Serialize()
        => JsonSerializer.Serialize(_products.Take(ItemCount), CamelCaseOptions);

    [Benchmark(Description = "CamelCase + IgnoreNull (Controllers API)")]
    public string CamelCaseIgnoreNull_Serialize()
        => JsonSerializer.Serialize(_products.Take(ItemCount), CamelCaseIgnoreNullOptions);
}
