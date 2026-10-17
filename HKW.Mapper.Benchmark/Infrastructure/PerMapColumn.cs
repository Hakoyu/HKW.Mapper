using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using HKW.Mapper.Benchmark.Benchmarks;

namespace HKW.Mapper.Benchmark.Infrastructure;

internal abstract class PerMapColumn : IColumn
{
    public static readonly IColumn Nanoseconds = new NanosecondsPerMapColumn();
    public static readonly IColumn Bytes = new BytesPerMapColumn();

    public abstract string Id { get; }
    public abstract string ColumnName { get; }
    public abstract string Legend { get; }
    public abstract ColumnCategory Category { get; }
    public UnitType UnitType => UnitType.Dimensionless;
    public int PriorityInCategory => 100;
    public bool IsNumeric => true;
    public bool AlwaysShow => true;
    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
    public abstract string GetValue(
        Summary summary,
        BenchmarkCase benchmarkCase,
        SummaryStyle style
    );

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) =>
        GetValue(summary, benchmarkCase, summary.Style);

    protected static long GetMapCount(BenchmarkCase benchmarkCase)
    {
        var operationCount = benchmarkCase
            .Parameters.Items.FirstOrDefault(parameter =>
                parameter.Name == nameof(MappingBenchmarkBase.MapOperations)
            )
            ?.Value as int? ?? 1;
        var mapsPerOperation = benchmarkCase.Descriptor.Type == typeof(TotalMappingBenchmark) ? 3 : 1;
        return (long)operationCount * mapsPerOperation;
    }

    private sealed class NanosecondsPerMapColumn : PerMapColumn
    {
        public override string Id => nameof(NanosecondsPerMapColumn);
        public override string ColumnName => "Ns/Map";
        public override string Legend => "Mean nanoseconds per logical mapping";
        public override ColumnCategory Category => ColumnCategory.Statistics;

        public override string GetValue(
            Summary summary,
            BenchmarkCase benchmarkCase,
            SummaryStyle style
        )
        {
            var report = summary[benchmarkCase];
            if (report is null)
                return "?";

            var mean = report.ResultStatistics?.Mean;
            return mean.HasValue
                ? (mean.Value / GetMapCount(benchmarkCase)).ToString("0.###", style.CultureInfo)
                : "?";
        }
    }

    private sealed class BytesPerMapColumn : PerMapColumn
    {
        public override string Id => nameof(BytesPerMapColumn);
        public override string ColumnName => "Bytes/Map";
        public override string Legend => "Allocated bytes per logical mapping";
        public override ColumnCategory Category => ColumnCategory.Metric;

        public override string GetValue(
            Summary summary,
            BenchmarkCase benchmarkCase,
            SummaryStyle style
        )
        {
            var report = summary[benchmarkCase];
            if (report is null)
                return "?";

            var bytes = report.GcStats.GetBytesAllocatedPerOperation(benchmarkCase);
            return bytes.HasValue
                ? ((double)bytes.Value / GetMapCount(benchmarkCase)).ToString(
                    "0.###",
                    style.CultureInfo
                )
                : "?";
        }
    }
}
