using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace SecPerf.Benchmarks;

/// <summary>
/// Shared BenchmarkDotNet configuration used by all benchmark classes.
/// Produces Markdown (GitHub-flavoured), CSV, and HTML reports in BenchmarkDotNet.Artifacts/.
/// </summary>
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(CsvExporter.Default);
        AddExporter(HtmlExporter.Default);

        // ShortRun: 3 launches × 1 warmup × 3 iterations — quick enough for thesis iterations.
        // Swap to Job.Default for the final publication run.
        AddJob(Job.ShortRun);

        AddColumnProvider(BenchmarkDotNet.Columns.DefaultColumnProviders.Instance);
    }
}
