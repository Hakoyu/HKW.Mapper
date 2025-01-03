﻿using System.ComponentModel;
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
        //var test = new Test2();
        //var c = new TestMapConfig();
        //test.Value ??= 1;
        //test.
    }

    //internal static HKW.HKWMapper.Demo.TestTarget MapToTestTarget(
    //    this HKW.HKWMapper.Demo.TestSource source,
    //    HKW.HKWMapper.Demo.TestTarget target
    //)
    //{
    //    HKW_HKWMapper_Demo_TestMapConfig.BeginMapAction(source, target);
    //    HKW_HKWMapper_Demo_TestMapConfig.GetMapActionAsync("Value")(source, target).Wait();
    //    if (source.Value1 == default)
    //        if (target.Value1 == default)
    //            target.Value1 = source.Value1;
    //    if (source.Value2 is not null)
    //        if (target.Value2 is null)
    //            target.Value2 = source.Value2;
    //    target.Value3 = HKW_HKWMapper_Demo_TestConverter.Convert(source, source.Value3);
    //    target.Value4 = HKW_HKWMapper_Demo_TestConverter.Convert(source, source.Value4);
    //    return target;
    //}
}

//[MapTo(
//    typeof(TestTarget),
//    ScrutinyMode = true,
//    MapperConfig = typeof(TestMapConfig),
//    InvokeState = MapMethodInvokeState.Both
//)]
//[MapFrom(typeof(TestSource), TargetName = "TestSource1")]
//[MapFrom(typeof(TestTarget))]
//internal class TestSource
//{
//    public int Value { get; set; }

//    [TestSourceMapToTestTargetProperty(
//        "Value1",
//        MapWhenLValueNullOrDefault = true,
//        MapWhenRValueNotNullOrDefault = true
//    )]
//    [TestSourceMapFromTestTargetProperty(
//        "Value1",
//        MapWhenLValueNullOrDefault = true,
//        MapWhenRValueNotNullOrDefault = true
//    )]
//    public int Value1 { get; set; }

//    [TestSourceMapToTestTargetProperty(
//        "Value2",
//        MapWhenLValueNullOrDefault = true,
//        MapWhenRValueNotNullOrDefault = true
//    )]
//    [TestSourceMapFromTestTargetProperty(
//        "Value2",
//        MapWhenLValueNullOrDefault = true,
//        MapWhenRValueNotNullOrDefault = true
//    )]
//    public string Value2 { get; set; }

//    [TestSourceMapToTestTargetProperty(typeof(TestConverter))]
//    [TestSourceMapFromTestTargetProperty(typeof(TestConverter))]
//    public int Value3 { get; set; }

//    [TestSourceMapFromTestTargetProperty(typeof(TestConverter))]
//    public int Value4 { get; set; }
//}

//internal class TestTarget
//{
//    internal int Abc;
//    internal int Value { get; set; }
//    public int Value1 { get; set; }
//    public string Value2 { get; set; } = null!;
//    public string? Value3 { get; set; }
//    public Task<int> Value4 { get; set; }
//}

//internal static class Test3
//{
//    public class Test33
//    {
//        public int Value { get; set; }
//    }
//}

//internal class TestConverter : IMapConverter<int, string>
//{
//    public string Convert(object source, int value)
//    {
//        return value.ToString();
//    }

//    public int ConvertBack(object source, string value)
//    {
//        return int.Parse(value);
//    }
//}

//internal class TestAsyncConverter : IMapConverter<Task<int>, Task<string>>
//{
//    public Task<string> Convert(object source, Task<int> value)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<int> ConvertBack(object source, Task<string> value)
//    {
//        throw new NotImplementedException();
//    }
//}

//internal class TestMapConfig : MapperConfig<TestSource, TestTarget>
//{
//    public TestMapConfig()
//    {
//        AddMapAsync(
//            x => x.Value,
//            async (s, t) =>
//            {
//                await Task.Delay(1);
//            }
//        );
//    }

//    public override void BeginMapAction(TestSource source, TestTarget target)
//    {
//        return;
//    }
//}

//internal class TestMap1Config : MapperConfig<TestSource, TestTarget>
//{
//    public TestMap1Config()
//    {
//        //AddMap(
//        //    x => x.Value,
//        //    (s, t) =>
//        //    {
//        //        s.Value = t.Value;
//        //    }
//        //);
//    }

//    public override void BeginMapAction(TestSource source, TestTarget target)
//    {
//        return;
//    }

//    public override void EndMapAction(TestSource source, TestTarget target)
//    {
//        return;
//    }
//}

//partial class TestModel : ReactiveObject
//{
//    public TestModel()
//    {
//        var id = 1;
//        _id = string.Empty;
//        //OnPropertyChange(ref id, 2, nameof(ID), true);
//        //OnPropertyChange(ref id, 2, nameof(ID), false);
//        //CanExecute1 = false;
//    }

//    //private string _tid = string.Empty;
//    //public string TID
//    //{
//    //    get => _tid;
//    //    set => RaiseAndSet(ref _tid, value, nameof(TID), false);
//    //}
//    private string _id;

//    [ReactiveProperty(false)]
//    public string ID { get; set; } = string.Empty;

//    [ReactiveProperty]
//    public string Name { get; set; } = string.Empty;

//    [NotifyPropertyChangeFrom(nameof(Name), nameof(ID))]
//    public bool CanExecute => Name == ID;

//    [NotifyPropertyChangeFrom(nameof(Name))]
//    public List<int> List => new List<int>();

//    [NotifyPropertyChangeFrom(nameof(Name))]
//    public List<int> List1 => this.To(static x => new List<int>());

//    [ReactiveProperty]
//    public bool[,] Bools { get; set; }

//    /// <summary>
//    /// 文化名称
//    /// </summary>

//    [ReactiveProperty]
//    public string CultureName { get; set; } = string.Empty;

//    /// <summary>
//    /// 文化全名
//    /// </summary>
//    [NotifyPropertyChangeFrom(nameof(CultureName))]
//    public string CultureFullName =>
//        this.To(static x =>
//        {
//            if (string.IsNullOrWhiteSpace(x.CultureName))
//            {
//                return UnknownCulture;
//            }
//            CultureInfo info = null!;
//            try
//            {
//                info = CultureInfo.GetCultureInfo(x.CultureName);
//            }
//            catch
//            {
//                return UnknownCulture;
//            }
//            if (info is not null)
//            {
//                return $"{info.DisplayName} [{info.Name}]";
//            }
//            return UnknownCulture;
//        });
//    public static string UnknownCulture => "未知文化";

//    //public void OnNameChanging(string value)
//    //{
//    //    return;
//    //}

//    /// <summary>
//    /// Test
//    /// </summary>
//    public void Test()
//    {
//        Console.WriteLine(nameof(Test));
//    }

//    /// <summary>
//    /// TestAsync
//    /// </summary>
//    /// <returns></returns>
//    [ReactiveCommand]
//    public async Task TestAsync()
//    {
//        await Task.Delay(1000);
//        Console.WriteLine(nameof(TestAsync));
//    }
//}

//internal static class TestExtensions
//{
//    public static IObservable<(T? Previous, T? Current)> Zip<T>(this IObservable<T> source)
//    {
//        return source.Scan(
//            (Previous: default(T), Current: default(T)),
//            (pair, current) => (pair.Current, current)
//        );
//    }

//    public static TTarget To<TSource, TTarget>(this TSource source, Func<TSource, TTarget> func)
//    {
//        return func(source);
//    }
//}

///// <summary>
///// 可观察点
///// </summary>
///// <typeparam name="T">数据类型</typeparam>
//[DebuggerDisplay("({X}, {Y})")]
//internal partial class ObservablePoint<T> : ReactiveObjectX, IEquatable<ObservablePoint<T>>
//    where T : struct, INumber<T>
//{
//    /// <inheritdoc/>
//    public ObservablePoint() { }

//    /// <inheritdoc/>
//    /// <param name="x">坐标X</param>
//    /// <param name="y">坐标Y</param>
//    public ObservablePoint(T x, T y)
//    {
//        X = x;
//        Y = y;
//    }

//    /// <inheritdoc/>
//    [ReactiveProperty]
//    public T X { get; set; } = default!;

//    /// <inheritdoc/>
//    [ReactiveProperty]
//    public T Y { get; set; } = default!;

//    #region Clone
//    /// <inheritdoc/>
//    public ObservablePoint<T> Clone()
//    {
//        return new(X, Y);
//    }
//    #endregion

//    #region Equals

//    /// <inheritdoc/>
//    public override int GetHashCode()
//    {
//        return HashCode.Combine(X, Y);
//    }

//    /// <inheritdoc/>
//    public override bool Equals(object? obj)
//    {
//        return Equals(obj as ObservablePoint<T>);
//    }

//    /// <inheritdoc/>
//    public bool Equals(ObservablePoint<T>? other)
//    {
//        if (other is null)
//            return false;
//        return X == other.X && Y == other.Y;
//    }
//    #endregion
//    /// <inheritdoc/>
//    public override string ToString()
//    {
//        return $"X = {X}, Y = {Y}";
//    }
//}
