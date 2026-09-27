using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.HKWMapper;

internal static class Descriptors
{
    private const string _category = "HKWMapper";

    public static readonly DiagnosticDescriptor SameMapMethodName = new(
        id: "M0001",
        title: "Same map method name exists",
        messageFormat: "Same map method name exists '{0}', please check attribute and set different target name.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor SameMapTargetProperty = new(
        id: "M0002",
        title: "Same map target property",
        messageFormat: "The first source property '{0}' and this source property '{1}' has same target property '{2}', please check attribute and set different property name.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor WrongMapConfigType = new(
        id: "M0003",
        title: "Wrong map config type",
        messageFormat: "The map config '{0}' not inherited MapperConfig<TSource, TTarget>.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetPropertyNotExists = new(
        id: "M0004",
        title: "Target property not exists",
        messageFormat: "ScrutinyMode: Target property '{0}' not exists, please set property name or ignore.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor TargetPropertyTypeError = new(
        id: "M0005",
        title: "Target property type error",
        messageFormat: "ScrutinyMode: Target property '{0}' type error, please ignore or set converter.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetPropertyIsReadOnly = new(
        id: "M0006",
        title: "Target property is readonly",
        messageFormat: "ScrutinyMode: Target property '{0}' is readonly, please ignore or set property name.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetPropertyIsStatic = new(
        id: "M0007",
        title: "Target property is static",
        messageFormat: "Target property '{0}' is static, please ignore or check target property.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetPropertyAccessibilityInsufficient = new(
        id: "M0008",
        title: "Target property accessibility insufficient",
        messageFormat: "Target property '{0}' accessibility insufficient, please ignore or change property.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor PropertyIsReferenceType = new(
        id: "M0009",
        title: "Property is reference type",
        messageFormat: "Property is reference type, please igone or set the MapType.Reference of '{0}' to true.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor ConverterNotImplementIMapConverter = new(
        id: "M0010",
        title: "Converter not implement IMapConverter",
        messageFormat: "Converter '{0}' does not implement IMapConverter<TValue, TTarget> interface.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor ConverterGenericTypeError = new(
        id: "M0011",
        title: "Converter generic type error",
        messageFormat: "Converter generic type <{0}, {1}> cannot be applied to map type <{2}, {3}>.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MapConfigPropertyConverterPropertyNameError = new(
        id: "M0012",
        title: "Map config property converter property name error",
        messageFormat: "Map config property converter property name must be a valid string.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigActionModePropertyNameError = new(
        id: "M0013",
        title: "Map config action mode property name error",
        messageFormat: "Map config action mode property name must be a valid string.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigActionHasSameProperty = new(
        id: "M0014",
        title: "Map config action has the same target property",
        messageFormat: "Map config action has same target property, each property can only have one action.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigActionCannotHaveParameters = new(
        id: "M0015",
        title: "Map config action cannot have parameters",
        messageFormat: "Map config action cannot have parameters.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigActionInsufficientAccessibility = new(
        id: "M0016",
        title: "The accessibility of MapConfigAction is insufficient",
        messageFormat: "The accessibility of MapConfigAction is insufficient.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapPropertyTargetNotFound = new(
        id: "M0017",
        title: "Map property target not found",
        messageFormat: "MapProperty target '{0}' does not match any MapTarget on '{1}'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapPropertyTargetAmbiguous = new(
        id: "M0018",
        title: "Map property target is ambiguous",
        messageFormat: "MapProperty on '{0}' matches more than one mapping target.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    //public static readonly DiagnosticDescriptor ConverterTargetTypeDifferentNoMethod = new(
    //    id: "M0010",
    //    title: "Converter target property type is different",
    //    messageFormat: "Converter target property type '{0}' is different to target property type '{1}', please check your converter",
    //    category: _category,
    //    DiagnosticSeverity.Error,
    //    isEnabledByDefault: true
    //);
    //public static readonly DiagnosticDescriptor MapConfigSourceTypeDifferent = new(
    //    id: "M0011",
    //    title: "Map config source type is different",
    //    messageFormat: "Map config source type '{0}' is different to source type '{1}', please check your map config",
    //    category: _category,
    //    DiagnosticSeverity.Error,
    //    isEnabledByDefault: true
    //);
    //public static readonly DiagnosticDescriptor MapConfigTargetTypeDifferent = new(
    //    id: "M0012",
    //    title: "Map config target type is different",
    //    messageFormat: "Map config target type '{0}' is different to target type '{1}', please check your map config",
    //    category: _category,
    //    DiagnosticSeverity.Error,
    //    isEnabledByDefault: true
    //);
    //public static readonly DiagnosticDescriptor MapHasBeenAdded = new(
    //    id: "M0013",
    //    title: "Map config has been added",
    //    messageFormat: "Map has been added in '{0}', please check your map config or remove property attribute",
    //    category: _category,
    //    DiagnosticSeverity.Warning,
    //    isEnabledByDefault: true
    //);
    //public static readonly DiagnosticDescriptor SameMapPropertyConfig = new(
    //    id: "M0014",
    //    title: "Same map property config",
    //    messageFormat: "Map property config '{0}' is exists, please check your map property config",
    //    category: _category,
    //    DiagnosticSeverity.Error,
    //    isEnabledByDefault: true
    //);

    //public static DiagnosticDescriptor MapAsyncTaskDoesNotExist = new(
    //    id: "M0015",
    //    title: "Map async task does not exist",
    //    messageFormat: "Async task does not exist, please check the property type or set InvokeState to Sync in attribute",
    //    category: _category,
    //    DiagnosticSeverity.Error,
    //    isEnabledByDefault: true
    //);
}
