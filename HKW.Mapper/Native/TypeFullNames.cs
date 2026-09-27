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
    public static string MapIgnorePropertyAttribute { get; } =
        typeof(MapIgnorePropertyAttribute).GetGlobalFullName();
    public static string MapPropertyAttribute { get; } =
        typeof(MapPropertyAttribute).GetGlobalFullName();

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
    public static string MapPropertyType { get; } = typeof(MapPropertyType).GetGlobalFullName();

    public static string Type { get; } = typeof(Type).GetGlobalFullName();

    public static string ICollectionT { get; } = typeof(ICollection<>).GetGlobalFullName();
    public static string ICollectionNeedGeneric { get; } =
        "global::System.Collections.Generic.ICollection";
}
