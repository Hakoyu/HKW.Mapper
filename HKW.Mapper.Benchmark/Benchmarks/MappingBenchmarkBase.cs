using BenchmarkDotNet.Attributes;

namespace HKW.Mapper.Benchmark.Benchmarks;

public abstract class MappingBenchmarkBase
{
    public IEnumerable<int> MapOperationValues => [10_000, 100_000, 1_000_000];

    [ParamsSource(nameof(MapOperationValues))]
    public int MapOperations { get; set; }
}
