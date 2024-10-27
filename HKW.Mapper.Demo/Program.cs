﻿using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
        var test = new Test1();
        //test.Value ??= 1;
        //test.
    }
}

[MapTo(typeof(Test2))]
[MapFrom(typeof(Test2))]
//[MapTo(typeof(Test2), "ToTestXXX")]
internal class Test1
{
    [Test1MapToTest2Property(typeof(TestConverter))]
    [Test1MapFromTest2Property(typeof(TestConverter))]
    public int Value { get; set; }

    [Test1MapToTest2Property(typeof(TestConverter))]
    [Test1MapFromTest2Property(typeof(TestConverter))]
    public int Value1 { get; set; }

    [Test1MapToTest2Property(typeof(TestConverter))]
    [Test1MapFromTest2Property(typeof(TestConverter))]
    public int Value2 { get; set; }

    [Test1MapToTest2Property(typeof(TestConverter))]
    [Test1MapFromTest2Property(typeof(TestConverter))]
    public int Value3 { get; set; }

    [Test1MapToTest2Property(typeof(TestConverter))]
    [Test1MapFromTest2Property(typeof(TestConverter))]
    public int Value4 { get; set; }
}

internal class Test2
{
    public string? Value { get; set; }
    public string? Value1 { get; set; }
    public string? Value2 { get; set; }
    public string? Value3 { get; set; }
    public string? Value4 { get; set; }
}

internal class Test3
{
    public int Value { get; set; }
    public string? Value1 { get; set; }
    public string? Value2 { get; set; }
    public string? Value3 { get; set; }
    public string? Value4 { get; set; }
}

internal class TestConverter : MapConverter<int, string>
{
    public override string Convert(object source, int value)
    {
        return value.ToString();
    }

    public override int ConvertBack(object source, string value)
    {
        return int.Parse(value);
    }
}



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