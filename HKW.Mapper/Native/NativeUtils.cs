using System;
using System.Collections.Generic;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWMapper;

internal static class NativeUtils { }
#pragma warning disable S2436
/// <summary>
/// HashCodeHelper
/// </summary>
internal static class HashCodeHelper
{
    private const int Seed = unchecked((int)2166136261);
    private const int Prime = 16777619;

    public static int Combine<T1>(T1 value1)
    {
        var hash = Seed;
        hash = Mix(hash, GetHashCode(value1));
        return Finish(hash, 1);
    }

    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        var hash = Seed;
        hash = Mix(hash, GetHashCode(value1));
        hash = Mix(hash, GetHashCode(value2));
        return Finish(hash, 2);
    }

    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        var hash = Seed;
        hash = Mix(hash, GetHashCode(value1));
        hash = Mix(hash, GetHashCode(value2));
        hash = Mix(hash, GetHashCode(value3));
        return Finish(hash, 3);
    }

    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        var hash = Seed;
        hash = Mix(hash, GetHashCode(value1));
        hash = Mix(hash, GetHashCode(value2));
        hash = Mix(hash, GetHashCode(value3));
        hash = Mix(hash, GetHashCode(value4));
        return Finish(hash, 4);
    }

    public static int Combine<T1, T2, T3, T4, T5>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5
    )
    {
        var hash = Seed;
        hash = Mix(hash, GetHashCode(value1));
        hash = Mix(hash, GetHashCode(value2));
        hash = Mix(hash, GetHashCode(value3));
        hash = Mix(hash, GetHashCode(value4));
        hash = Mix(hash, GetHashCode(value5));
        return Finish(hash, 5);
    }

    public static int Combine<T1, T2, T3, T4, T5, T6>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5,
        T6 value6
    )
    {
        var hash = Seed;
        hash = Mix(hash, GetHashCode(value1));
        hash = Mix(hash, GetHashCode(value2));
        hash = Mix(hash, GetHashCode(value3));
        hash = Mix(hash, GetHashCode(value4));
        hash = Mix(hash, GetHashCode(value5));
        hash = Mix(hash, GetHashCode(value6));
        return Finish(hash, 6);
    }

    public static int Combine<T1, T2, T3, T4, T5, T6, T7>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5,
        T6 value6,
        T7 value7
    )
    {
        var hash = Seed;
        hash = Mix(hash, GetHashCode(value1));
        hash = Mix(hash, GetHashCode(value2));
        hash = Mix(hash, GetHashCode(value3));
        hash = Mix(hash, GetHashCode(value4));
        hash = Mix(hash, GetHashCode(value5));
        hash = Mix(hash, GetHashCode(value6));
        hash = Mix(hash, GetHashCode(value7));
        return Finish(hash, 7);
    }

    public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(
        T1 value1,
        T2 value2,
        T3 value3,
        T4 value4,
        T5 value5,
        T6 value6,
        T7 value7,
        T8 value8
    )
    {
        var hash = Seed;
        hash = Mix(hash, GetHashCode(value1));
        hash = Mix(hash, GetHashCode(value2));
        hash = Mix(hash, GetHashCode(value3));
        hash = Mix(hash, GetHashCode(value4));
        hash = Mix(hash, GetHashCode(value5));
        hash = Mix(hash, GetHashCode(value6));
        hash = Mix(hash, GetHashCode(value7));
        hash = Mix(hash, GetHashCode(value8));
        return Finish(hash, 8);
    }

    public static int Combine(params object?[]? values)
    {
        if (values is null)
            return 0;

        var hash = Seed;
        foreach (var value in values)
            hash = Mix(hash, value?.GetHashCode() ?? 0);

        return Finish(hash, values.Length);
    }

    public static int Combine<T>(params T[]? values)
    {
        if (values is null)
            return 0;

        var hash = Seed;
        foreach (var value in values)
            hash = Mix(hash, GetHashCode(value));

        return Finish(hash, values.Length);
    }

    private static int GetHashCode<T>(T value)
    {
        if (value is null)
            return 0;

        return EqualityComparer<T>.Default.GetHashCode(value);
    }

    private static int Mix(int hash, int value)
    {
        unchecked
        {
            return (hash ^ value) * Prime;
        }
    }

    private static int Finish(int hash, int count)
    {
        unchecked
        {
            hash += count * 4;
            hash ^= hash >> 16;
            hash *= unchecked((int)0x85EBCA6B);
            hash ^= hash >> 13;
            hash *= unchecked((int)0xC2B2AE35);
            hash ^= hash >> 16;
            return hash;
        }
    }
}
#pragma warning restore S2436
