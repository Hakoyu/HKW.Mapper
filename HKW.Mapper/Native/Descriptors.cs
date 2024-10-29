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
            messageFormat: "Same method name exists \"{0}\", please check attribute and set different method name",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor SameTargetName =
        new(
            id: "M0002",
            title: "Same target property name exists",
            messageFormat: "Same target property name exists \"{0}\", please check attribute and set different property name",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor ConverterError =
        new(
            id: "M0003",
            title: "Converter not implemented IConverter interface",
            messageFormat: "Converter \"{0}\", not implemented IMapConverter interface",
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
}
