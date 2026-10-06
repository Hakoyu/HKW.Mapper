using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.HKWMapper;

internal static class Descriptors
{
    private const string _category = "HKWMapper";

    public static readonly DiagnosticDescriptor SameMapMethodName = new(
        id: "MAP0001",
        title: "Same map method name exists",
        messageFormat: "Same map method name exists '{0}', please check attribute and set different target name.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor SameMapTargetProperty = new(
        id: "MAP0002",
        title: "Same map target property",
        messageFormat: "The first source property '{0}' and this source property '{1}' has same target property '{2}', please check attribute and set different property name.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MapConfigTypeError = new(
        id: "MAP0003",
        title: "Map config type error",
        messageFormat: "The map config '{0}' not inherited MapperConfig<TSource, TTarget>.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetPropertyNotExists = new(
        id: "MAP0004",
        title: "Target property not exists",
        messageFormat: "Target property '{0}' not exists, please set property name or ignore.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor TargetPropertyTypeError = new(
        id: "MAP0005",
        title: "Target property type error",
        messageFormat: "Target property '{0}' type error, please ignore or set converter.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetPropertyIsReadOnly = new(
        id: "MAP0006",
        title: "Target property is readonly",
        messageFormat: "Target property '{0}' is readonly, please ignore or set property name.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetPropertyIsStatic = new(
        id: "MAP0007",
        title: "Target property is static",
        messageFormat: "Target property '{0}' is static, please ignore or check target property.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetPropertyAccessibilityInsufficient = new(
        id: "MAP0008",
        title: "Target property accessibility insufficient",
        messageFormat: "Target property '{0}' accessibility insufficient, please ignore or change property.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor PropertyIsReferenceType = new(
        id: "MAP0009",
        title: "Property is reference type",
        messageFormat: "Property is reference type, please igone or set the MapType.Reference of '{0}' to true.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor ConverterNotImplementIMapConverter = new(
        id: "MAP0010",
        title: "Converter not implement IMapConverter",
        messageFormat: "Converter '{0}' does not implement IMapConverter<TValue, TTarget> interface.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor ConverterGenericTypeError = new(
        id: "MAP0011",
        title: "Converter generic type error",
        messageFormat: "Converter generic type <{0}, {1}> cannot be applied to map type <{2}, {3}>.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MapConfigPropertyConverterPropertyNameError = new(
        id: "MAP0012",
        title: "Map config property converter property name error",
        messageFormat: "Map config property converter property name must be a valid string.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigActionModePropertyNameError = new(
        id: "MAP0013",
        title: "Map config action mode property name error",
        messageFormat: "Map config action mode property name must be a valid string.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigActionHasSameProperty = new(
        id: "MAP0014",
        title: "Map config action has the same target property",
        messageFormat: "Map config action has same target property, each property can only have one action.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigActionCannotHaveParameters = new(
        id: "MAP0015",
        title: "Map config action cannot have parameters",
        messageFormat: "Map config action cannot have parameters.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigActionInsufficientAccessibility = new(
        id: "MAP0016",
        title: "The accessibility of MapConfigAction is insufficient",
        messageFormat: "The accessibility of MapConfigAction is insufficient.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapPropertyTargetNotFound = new(
        id: "MAP0017",
        title: "Map property target not found",
        messageFormat: "MapProperty target is '{0}', but not any MapTarget target is '{0}'.",
        category: _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapPropertyTargetAmbiguous = new(
        id: "MAP0018",
        title: "Map property target is ambiguous",
        messageFormat: "MapProperty on '{0}' matches more than one mapping target.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MapConfigGenericTypeError = new(
        id: "MAP0019",
        title: "Map config generic type error",
        messageFormat: "The MapConfig generic type '<{0},{1}>' cannot be applied to MapTarget type <{2}, {3}>.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor CompositeConverterNotImplemented = new(
        id: "MAP0020",
        title: "Composite converter interface not implemented",
        messageFormat: "Converter '{0}' does not implement ICompositeMapConverter<TSourceValue, TTargetValues>.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor CompositeConverterRequiresTuple = new(
        id: "MAP0021",
        title: "Composite converter requires a tuple",
        messageFormat: "Composite converter '{0}' must use a ValueTuple as its target values type.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor CompositePropertyCountMismatch = new(
        id: "MAP0022",
        title: "Composite property count mismatch",
        messageFormat: "Composite mapping declares {0} target properties, but converter tuple contains {1} elements.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor CompositePropertyTypeMismatch = new(
        id: "MAP0023",
        title: "Composite property type mismatch",
        messageFormat: "Target property '{0}' has type '{1}', but tuple element {2} has type '{3}'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor CompositeTargetPropertiesInvalid = new(
        id: "MAP0024",
        title: "Composite target properties are invalid",
        messageFormat: "Composite target properties must contain at least two unique, non-empty names.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor CompositeSourceTypeMismatch = new(
        id: "MAP0025",
        title: "Composite converter source type mismatch",
        messageFormat: "Composite converter source type '{0}' cannot be applied to source property type '{1}'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigCompositePropertyNameError = new(
        id: "MAP0026",
        title: "Map config composite property names are invalid",
        messageFormat: "Map config composite converter requires a source property and at least two unique target properties.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigCompositeInitializerError = new(
        id: "MAP0027",
        title: "Map config composite initializer cannot be analyzed",
        messageFormat: "Map config composite converter must use an inline object initializer with an inline target property array.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
