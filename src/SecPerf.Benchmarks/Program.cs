using BenchmarkDotNet.Running;
using SecPerf.Benchmarks.Benchmarks;

// BenchmarkDotNet must run in Release mode — it will print a warning and exit in Debug.
// Usage: dotnet run --project src/SecPerf.Benchmarks -c Release
//
// To run a single benchmark class:
//   dotnet run --project src/SecPerf.Benchmarks -c Release --filter "*Routing*"
//   dotnet run --project src/SecPerf.Benchmarks -c Release --filter "*FullPipeline*"
//   dotnet run --project src/SecPerf.Benchmarks -c Release --filter "*JsonSerialization*"
//
// Reports are written to: BenchmarkDotNet.Artifacts/results/

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
