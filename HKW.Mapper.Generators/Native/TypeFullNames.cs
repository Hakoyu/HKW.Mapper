using System;
using System.Collections.Generic;
using HKW.SourceGeneratorUtils;

namespace HKW.Mapper;

internal static class TypeFullNames
{
    public const string MapTargetAttributeFullName = "HKW.Mapper.MapTargetAttribute";
    public const string MapSourceAttributeFullName = "HKW.Mapper.MapSourceAttribute";
    public const string MapSourceConstructorAttributeFullName =
        "HKW.Mapper.MapSourceConstructorAttribute";

    public const string MapTargetAttribute = "global::HKW.Mapper.MapTargetAttribute";
    public const string MapSourceAttribute = "global::HKW.Mapper.MapSourceAttribute";
    public const string MapConstructorAttribute = "global::HKW.Mapper.MapConstructorAttribute";
    public const string MapSourceConstructorAttribute =
        "global::HKW.Mapper.MapSourceConstructorAttribute";
    public const string MapIgnorePropertyAttribute =
        "global::HKW.Mapper.MapIgnorePropertyAttribute";
    public const string MapPropertyAttribute = "global::HKW.Mapper.MapPropertyAttribute";
    public const string MapCompositePropertyAttribute =
        "global::HKW.Mapper.MapCompositePropertyAttribute";
    public const string MapIgnoreFieldAttribute = "global::HKW.Mapper.MapIgnoreFieldAttribute";
    public const string MapFieldAttribute = "global::HKW.Mapper.MapFieldAttribute";
    public const string MapCompositeFieldAttribute =
        "global::HKW.Mapper.MapCompositeFieldAttribute";

    public const string MapConfigClass = "global::HKW.Mapper.MapperConfigBase<TSource, TTarget>";
    public const string MapConverterInterface =
        "global::HKW.Mapper.IMapConverter<TSourceValue, TTargetValue>";
    public const string MapCompositeConverterInterface =
        "global::HKW.Mapper.IMapCompositeConverter<TSourceValue, TTargetValues>";
    public const string MapConfigMemberConverter =
        "global::HKW.Mapper.MapConfigMemberConverter<TSourceValue, TTargetValue>";
    public const string MapConfigCompositeMemberConverter =
        "global::HKW.Mapper.MapConfigCompositeMemberConverter<TSourceValue, TTargetValues>";

    public const string MapToConfigActionAttribute =
        "global::HKW.Mapper.MapToConfigActionAttribute";
    public const string MapFromConfigActionAttribute =
        "global::HKW.Mapper.MapFromConfigActionAttribute";

    public static string ICloneable { get; } = typeof(ICloneable).GetGlobalFullName();
    public const string MapPropertyType = "global::HKW.Mapper.MapPropertyType";

    public static string ICollectionT { get; } = typeof(ICollection<>).GetGlobalFullName();
    public static string IDictionaryT { get; } = typeof(IDictionary<,>).GetGlobalFullName();
}
