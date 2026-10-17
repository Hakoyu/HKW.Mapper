using BenchmarkDotNet.Attributes;
using HKW.Mapper.Benchmark.Infrastructure;
using HKW.Mapper.Benchmark.Models;
using Mapster;

namespace HKW.Mapper.Benchmark.Benchmarks;

public class RecursiveMappingBenchmark : MappingBenchmarkBase
{
    private Foo _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MappingConfiguration.EnsureMapsterConfigured();
        _source = BenchmarkData.CreateFoo();
        MappingConfiguration.WarmMapster(_source);
        MappingCorrectness.Verify(_source);
    }

    [Benchmark(Baseline = true, Description = "Mapster 10.0.13")]
    public void MapsterDefault() =>
        MappingLoop.Run(_source, static source => source.Adapt<FooDto>(), MapOperations);

    [Benchmark(Description = "HKW.Mapper 0.3.2")]
    public void HkwMapper() =>
        MappingLoop.Run(_source, static source => source.MapTo(new FooDto()), MapOperations);
}
