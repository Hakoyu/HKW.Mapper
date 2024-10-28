using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.HKWMapper;

internal static class Descriptors
{
    public static readonly DiagnosticDescriptor SameMethodNameDescriptor =
        new(
            id: "M0001",
            title: "Same method name exists",
            messageFormat: "Same method name exists \"{0}\", place check attribute and set different method name",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
    public static readonly DiagnosticDescriptor SameTargetNameDescriptor =
        new(
            id: "M0002",
            title: "Same target property name exists",
            messageFormat: "Same target property name exists \"{0}\", place check attribute and set different property name",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

    public static readonly DiagnosticDescriptor ConverterDescriptor =
        new(
            id: "M0003",
            title: "Converter not implemented IConverter interface",
            messageFormat: "Converter \"{0}\", not implemented IMapConverter interface",
            category: "HKWMapper",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
}
