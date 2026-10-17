using BenchmarkDotNet.Attributes;
using HKW.Mapper.Benchmark.Infrastructure;
using HKW.Mapper.Benchmark.Models;
using Mapster;

namespace HKW.Mapper.Benchmark.Benchmarks;

public class ComplexMappingBenchmark : MappingBenchmarkBase
{
    private Customer _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        MappingConfiguration.EnsureMapsterConfigured();
        _source = BenchmarkData.CreateCustomer();
        MappingConfiguration.WarmMapster(_source);
        MappingCorrectness.Verify(_source);
    }

    [Benchmark(Baseline = true, Description = "Mapster 10.0.13")]
    public void MapsterDefault() =>
        MappingLoop.Run(_source, static source => source.Adapt<CustomerDto>(), MapOperations);

    [Benchmark(Description = "HKW.Mapper 0.3.2")]
    public void HkwMapper() =>
        MappingLoop.Run(
            _source,
            static source => source.MapTo(new CustomerDto()),
            MapOperations
        );
}
