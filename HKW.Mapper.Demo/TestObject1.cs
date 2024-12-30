using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HKW.HKWMapper.Demo;

[MapTo(typeof(TestObject1))]
//[MapTo(typeof(TestObject1), TargetName = "114514")]
internal class TestObject1
{
    public int feild;

    public int Property1 { get; set; }

    [TestObject1MapToTestObject1Property(nameof(Property1))]
    public Task<int> Property2 { get; set; } = null!;
    public string Property3 { get; set; } = string.Empty;

    public object? Property4 { get; set; }

    public List<string> Property5 { get; set; } = null!;

    public Dictionary<string, string> Property6 { get; set; } = null!;
}

internal class TestObject1MapToTestObject1Config : MapperConfig<TestObject1, TestObject1>
{
    public TestObject1MapToTestObject1Config()
    {
        AddMap(
            x => x.Property1,
            (s, t) =>
            {
                t.Property1 = s.Property1;
            }
        );
    }

    public override Task BeginMapActionAsync(TestObject1 source, TestObject1 target)
    {
        return base.BeginMapActionAsync(source, target);
    }
}
