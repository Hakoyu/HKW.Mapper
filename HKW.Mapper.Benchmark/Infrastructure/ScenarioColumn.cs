using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace HKW.Mapper.Benchmark.Infrastructure;

internal sealed class ScenarioColumn : IColumn
{
    public static readonly IColumn Default = new ScenarioColumn();

    public string Id => nameof(ScenarioColumn);
    public string ColumnName => "Scenario";
    public string Legend => "Object graph scenario";
    public UnitType UnitType => UnitType.Dimensionless;
    public ColumnCategory Category => ColumnCategory.Job;
    public int PriorityInCategory => -10;
    public bool IsNumeric => false;
    public bool AlwaysShow => true;
    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) =>
        benchmarkCase.Descriptor.Type.Name.Replace("MappingBenchmark", string.Empty);

    public string GetValue(
        Summary summary,
        BenchmarkCase benchmarkCase,
        SummaryStyle style
    ) => GetValue(summary, benchmarkCase);
}
