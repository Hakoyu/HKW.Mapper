using BenchmarkDotNet.Running;
using HKW.Mapper.Benchmark.Benchmarks;
using HKW.Mapper.Benchmark.Infrastructure;

namespace HKW.Mapper.Benchmark;

internal static class Program
{
    private static void Main(string[] args)
    {
        var switcher = new BenchmarkSwitcher([
            typeof(FlatMappingBenchmark),
            typeof(ComplexMappingBenchmark),
            typeof(RecursiveMappingBenchmark),
            typeof(TotalMappingBenchmark),
        ]);

        var hasCommandLineJob = args.Any(argument =>
            argument.Equals("--job", StringComparison.OrdinalIgnoreCase)
            || argument.Equals("-j", StringComparison.OrdinalIgnoreCase)
        );
        switcher.Run(args, new BenchmarkConfig(addDefaultJob: !hasCommandLineJob));
    }
}
