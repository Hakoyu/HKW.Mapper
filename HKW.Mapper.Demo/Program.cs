using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Windows.Input;
using HKW.HKWMapper;

namespace HKW.HKWMapper.Demo;

internal class Program
{
    //private string $Name;
    //public string Name { get; set; } = string.Empty;

    static void Main(string[] args)
    {
        var task = new Task<int>(() => 1);
        //var arr = new object();
        //if(EqualityComparer<object>.Default.Equals(arr,default))

        //var test = new Test2();
        //var c = new TestMapConfig();
        //test.Value ??= 1;
        //test.
    }
}

[MapTarget(typeof(Test2), typeof(MyMapConfig))]
public class Test1
{
    //[Test1MapTargetTest2Property(nameof(Test2.Value1))]
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
    public int Value4 { get; set; }
    public int Value5 { get; set; }
}

public class Test2
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
    public int Value4 { get; set; }
    public int Value5 { get; set; }
}

public class MyMapConfig : MapperConfig<Test1, Test2>
{
    public MyMapConfig(Test1 source, Test2 target)
        : base(source, target) { }

    public MapConfigPropertyConverter<int, int> Converter1 { get; } =
        new(
            nameof(Test1.Value1),
            nameof(Test2.Value1),
            (source, value) => value,
            (source, value) => value
        );

    [MapToConfigAction(MapConfigActionMode.Start)]
    public async Task Start()
    {
        await Task.Delay(100);
    }
}
