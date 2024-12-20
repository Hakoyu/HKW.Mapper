using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.HKWMapper;

internal static class NativeUtils
{
    public static string GetMapPropertyAttributeName(INamedTypeSymbol sourceType, string methodName)
    {
        return $"{sourceType.ContainingNamespace}.{sourceType.Name}{methodName}PropertyAttribute";
    }
}
