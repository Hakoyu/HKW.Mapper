using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.HKWMapper;

internal static class Descriptors
{
    public static readonly DiagnosticDescriptor SameMethodName =
        new(
            id: "M0001",
            title: "Same method name exists",
            messageFormat: "Same method name exists \"{0}\", please check attribute and set different target name",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor SameMapTargetProperty =
        new(
            id: "M0002",
            title: "Same map target property",
            messageFormat: "The first source property \"{0}\" and this source property \"{1}\" has same target property \"{1}\", please check attribute and set different property name",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor NotImplementIMapConverter =
        new(
            id: "M0003",
            title: "Converter not implemented IConverter interface",
            messageFormat: "Converter \"{0}\" Not implemented IMapConverter interface",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor TargetPropertyNotExists =
        new(
            id: "M0004",
            title: "Target property not exists",
            messageFormat: "Target property \"{0}\" not exists, please set property name or add ignore attribute",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor TargetPropertyIsReadOnly =
        new(
            id: "M0005",
            title: "Target property is readonly",
            messageFormat: "Target property \"{0}\" is readonly, please set property name or add ignore attribute",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor TargetPropertyIsStatic =
        new(
            id: "M0006",
            title: "Target property is static",
            messageFormat: "Target property \"{0}\" is static, please check target property or add ignore attribute",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor TargetPropertyInsufficientAccessibility =
        new(
            id: "M0007",
            title: "Target property insufficient Accessibility",
            messageFormat: "Target property \"{0}\" insufficient Accessibility, please check target property or add ignore attribute",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor TargetPropertyTypeDifferent =
        new(
            id: "M0008",
            title: "Target property type is different",
            messageFormat: "Target property \"{0}\" type \"{1}\" is different in method \"{2}\", please check target property or add converter",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor TargetPropertyTypeDifferentNoMethod =
        new(
            id: "M0008",
            title: "Target property type is different",
            messageFormat: "Target property \"{0}\" type \"{1}\" is different, please check target property or add converter",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor ConverterCurrentTypeDifferentNoMethod =
        new(
            id: "M0009",
            title: "Converter current property type is different",
            messageFormat: "Converter current property type \"{0}\" is different to current property type \"{1}\", please check your converter",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor ConverterTargetTypeDifferentNoMethod =
        new(
            id: "M0010",
            title: "Converter target property type is different",
            messageFormat: "Converter target property type \"{0}\" is different to target property type \"{1}\", please check your converter",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor MapConfigSourceTypeDifferent =
        new(
            id: "M0011",
            title: "Map config source type is different",
            messageFormat: "Map config source type \"{0}\" is different to source type \"{1}\", please check your map config",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor MapConfigTargetTypeDifferent =
        new(
            id: "M0012",
            title: "Map config target type is different",
            messageFormat: "Map config target type \"{0}\" is different to target type \"{1}\", please check your map config",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor MapHasBeenAdded =
        new(
            id: "M0013",
            title: "Map config has been added",
            messageFormat: "Map has been added in \"{0}\", please check your map config or remove property attribute",
            category: "HKWMapper",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor SameMapPropertyConfig =
        new(
            id: "M0014",
            title: "Same map property config",
            messageFormat: "Map property config \"{0}\" is exists, please check your map property config",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static DiagnosticDescriptor MapAsyncTaskDoesNotExist =
        new(
            id: "M0015",
            title: "Map async task does not exist",
            messageFormat: "Async task does not exist, please check the property type or set InvokeState to Sync in attribute",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
}
