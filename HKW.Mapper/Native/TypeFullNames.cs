using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWMapper;

internal static class TypeFullNames
{
    public const string _global = "global::";

    public static string MapTargetAttribute { get; } =
        typeof(MapTargetAttribute).GetGlobalFullName();
    public static string MapIgnoreAttribute { get; } =
        typeof(MapIgnoreAttribute).GetGlobalFullName();

    public static string MapConfigClass { get; } = typeof(MapperConfig<,>).GetGlobalFullName();
    public static string MapConverterInterface { get; } =
        typeof(IMapConverter<,>).GetGlobalFullName();
    public static string MapConfigPropertyConverter { get; } =
        typeof(MapConfigPropertyConverter<,>).GetGlobalFullName();

    public static string MapToConfigActionAttribute { get; } =
        typeof(MapToConfigActionAttribute).GetGlobalFullName();
    public static string MapFromConfigActionAttribute { get; } =
        typeof(MapFromConfigActionAttribute).GetGlobalFullName();

    public static string ICloneable { get; } = typeof(ICloneable).GetGlobalFullName();
    public static string MapPropertyTypes { get; } = typeof(MapPropertyTypes).GetGlobalFullName();

    public static string Type { get; } = typeof(Type).GetGlobalFullName();
}
