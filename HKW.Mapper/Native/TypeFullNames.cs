using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.Mapper;

internal static class TypeFullNames
{
    public static string MapTargetAttribute { get; } =
        typeof(MapTargetAttribute).GetGlobalFullName();
    public static string MapConstructorAttribute { get; } =
        typeof(MapConstructorAttribute).GetGlobalFullName();
    public static string MapIgnorePropertyAttribute { get; } =
        typeof(MapIgnorePropertyAttribute).GetGlobalFullName();
    public static string MapPropertyAttribute { get; } =
        typeof(MapPropertyAttribute).GetGlobalFullName();
    public static string MapCompositePropertyAttribute { get; } =
        typeof(MapCompositePropertyAttribute).GetGlobalFullName();
    public static string MapIgnoreFieldAttribute { get; } =
        typeof(MapIgnoreFieldAttribute).GetGlobalFullName();
    public static string MapFieldAttribute { get; } = typeof(MapFieldAttribute).GetGlobalFullName();
    public static string MapCompositeFieldAttribute { get; } =
        typeof(MapCompositeFieldAttribute).GetGlobalFullName();

    public static string MapConfigClass { get; } = typeof(MapperConfig<,>).GetGlobalFullName();
    public static string MapConverterInterface { get; } =
        typeof(IMapConverter<,>).GetGlobalFullName();
    public static string MapCompositeConverterInterface { get; } =
        typeof(IMapCompositeConverter<,>).GetGlobalFullName();
    public static string MapConfigPropertyConverter { get; } =
        typeof(MapConfigPropertyConverter<,>).GetGlobalFullName();
    public static string MapConfigPropertyCompositeConverter { get; } =
        typeof(MapConfigPropertyCompositeConverter<,>).GetGlobalFullName();
    public static string MapConfigFieldConverter { get; } =
        typeof(MapConfigFieldConverter<,>).GetGlobalFullName();
    public static string MapConfigFieldCompositeConverter { get; } =
        typeof(MapConfigFieldCompositeConverter<,>).GetGlobalFullName();

    public static string MapToConfigActionAttribute { get; } =
        typeof(MapToConfigActionAttribute).GetGlobalFullName();
    public static string MapFromConfigActionAttribute { get; } =
        typeof(MapFromConfigActionAttribute).GetGlobalFullName();

    public static string ICloneable { get; } = typeof(ICloneable).GetGlobalFullName();
    public static string MapPropertyType { get; } = typeof(MapPropertyType).GetGlobalFullName();

    public static string ICollectionT { get; } = typeof(ICollection<>).GetGlobalFullName();
    public static string IDictionaryT { get; } = typeof(IDictionary<,>).GetGlobalFullName();
}
