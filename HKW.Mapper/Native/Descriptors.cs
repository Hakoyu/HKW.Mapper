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
        messageFormat: "MapProperty target is '{0}', but not any MapTarget target is '{0}'.",
        category: _category,
        DiagnosticSeverity.Warning,
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
}
