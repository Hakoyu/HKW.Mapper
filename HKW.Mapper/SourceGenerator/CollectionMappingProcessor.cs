using System;
using System.Collections.Generic;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.Mapper.SourceGenerator;

internal sealed class CollectionMappingProcessor
{
    private readonly MapperGenerationContext _context;

    internal CollectionMappingProcessor(MapperGenerationContext context)
    {
        _context = context;
    }

    private readonly struct ElementMapping
    {
        public ElementMapping(
            Func<string, string>? toTarget,
            Func<string, string>? toSource,
            bool canMapTo,
            bool canMapFrom
        )
        {
            ToTarget = toTarget;
            ToSource = toSource;
            CanMapTo = canMapTo;
            CanMapFrom = canMapFrom;
        }

        public Func<string, string>? ToTarget { get; }
        public Func<string, string>? ToSource { get; }
        public bool CanMapTo { get; }
        public bool CanMapFrom { get; }
    }

    internal bool TryMapComplexProperty(
        MapTargetInfo mapTarget,
        MapMemberInfo sourceProperty,
        MapMemberInfo targetProperty
    )
    {
        var sourceType = ConverterMappingProcessor.UnwrapNullable(sourceProperty.Type);
        var targetType = ConverterMappingProcessor.UnwrapNullable(targetProperty.Type);

        if (
            TryMapImmutableCollectionProperty(
                mapTarget,
                sourceProperty,
                targetProperty,
                sourceType,
                targetType
            )
        )
            return true;

        if (
            sourceType is IArrayTypeSymbol sourceArray
            && targetType is IArrayTypeSymbol targetArray
        )
        {
            if (
                TryGetElementMapping(
                    (INamedTypeSymbol)sourceArray.ElementType,
                    (INamedTypeSymbol)targetArray.ElementType,
                    out var map
                )
                is false
            )
                return false;
            var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.AccessPath}";
            var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}";
            if (map.CanMapTo)
            {
                mapTarget.MapToMethod.Contents.Add("{");
                mapTarget.MapToMethod.Contents.Add(
                    $"{target} = new {targetArray.ElementType.GetGlobalFullName()}[{source}.Length];"
                );
                mapTarget.MapToMethod.Contents.Add($"for (var i = 0; i < {source}.Length; i++)");
                mapTarget.MapToMethod.Contents.Add(
                    $"    {target}[i] = {map.ToTarget!($"{source}[i]")};"
                );
                mapTarget.MapToMethod.Contents.Add("}");
            }

            if (map.CanMapFrom)
            {
                mapTarget.MapFromMethod.Contents.Add("{");
                mapTarget.MapFromMethod.Contents.Add(
                    $"{source} = new {sourceArray.ElementType.GetGlobalFullName()}[{target}.Length];"
                );
                mapTarget.MapFromMethod.Contents.Add($"for (var i = 0; i < {target}.Length; i++)");
                mapTarget.MapFromMethod.Contents.Add(
                    $"    {source}[i] = {map.ToSource!($"{target}[i]")};"
                );
                mapTarget.MapFromMethod.Contents.Add("}");
            }
            return true;
        }

        if (
            TryMapDictionaryProperty(
                mapTarget,
                sourceProperty,
                targetProperty,
                sourceType,
                targetType
            )
        )
            return true;

        if (
            TryMapCollectionProperty(
                mapTarget,
                sourceProperty,
                targetProperty,
                sourceType,
                targetType
            )
        )
            return true;

        return false;
    }

    private bool TryMapImmutableCollectionProperty(
        MapTargetInfo mapTarget,
        MapMemberInfo sourceProperty,
        MapMemberInfo targetProperty,
        ITypeSymbol sourceType,
        ITypeSymbol targetType
    )
    {
        var sourceKind = GetImmutableCollectionKind(sourceType);
        var targetKind = GetImmutableCollectionKind(targetType);
        if (sourceKind is null && targetKind is null)
            return false;

        var sourceExpression = $"{MapTargetInfo.SourceParamName}.{sourceProperty.AccessPath}";
        var targetExpression = $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}";
        if (IsDictionaryKind(sourceKind) || IsDictionaryKind(targetKind))
        {
            if (
                TryGetDictionaryTypes(sourceType, out var sourceKey, out var sourceValue) is false
                || TryGetDictionaryTypes(targetType, out var targetKey, out var targetValue) is false
                || TryGetElementMapping(sourceKey, targetKey, out var keyMap) is false
                || TryGetElementMapping(sourceValue, targetValue, out var valueMap) is false
            )
                return false;

            if (keyMap.CanMapTo && valueMap.CanMapTo)
            {
                AppendDictionaryMapping(
                    mapTarget.MapToMethod.Contents,
                    sourceExpression,
                    targetExpression,
                    sourceProperty,
                    targetProperty,
                    targetType,
                    targetKind,
                    targetKey,
                    targetValue,
                    keyMap.ToTarget!,
                    valueMap.ToTarget!
                );
            }
            if (keyMap.CanMapFrom && valueMap.CanMapFrom)
            {
                AppendDictionaryMapping(
                    mapTarget.MapFromMethod.Contents,
                    targetExpression,
                    sourceExpression,
                    targetProperty,
                    sourceProperty,
                    sourceType,
                    sourceKind,
                    sourceKey,
                    sourceValue,
                    keyMap.ToSource!,
                    valueMap.ToSource!
                );
            }
            return true;
        }

        if (
            TryGetEnumerableElementType(sourceType, out var sourceElement) is false
            || TryGetEnumerableElementType(targetType, out var targetElement) is false
            || TryGetElementMapping(sourceElement, targetElement, out var map) is false
        )
            return false;
        if (map.CanMapTo)
        {
            AppendCollectionMapping(
                mapTarget.MapToMethod.Contents,
                sourceExpression,
                targetExpression,
                sourceProperty,
                targetProperty,
                targetType,
                targetKind,
                targetElement,
                map.ToTarget!
            );
        }
        if (map.CanMapFrom)
        {
            AppendCollectionMapping(
                mapTarget.MapFromMethod.Contents,
                targetExpression,
                sourceExpression,
                targetProperty,
                sourceProperty,
                sourceType,
                sourceKind,
                sourceElement,
                map.ToSource!
            );
        }
        return true;
    }

    private void AppendCollectionMapping(
        List<string> contents,
        string input,
        string output,
        MapMemberInfo inputMember,
        MapMemberInfo outputMember,
        ITypeSymbol outputType,
        string? outputKind,
        INamedTypeSymbol outputElement,
        Func<string, string> convert
    )
    {
        if (outputKind is not null)
        {
            if (ConverterMappingProcessor.IsNullable(inputMember.Type) && ConverterMappingProcessor.IsNullable(outputMember.Type) is false)
            {
                _context.ClassInfo.ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.NullableCollectionRequiresNullableTarget,
                        inputMember.Locations[0],
                        inputMember.AccessPath,
                        outputMember.AccessPath
                    )
                );
                return;
            }
            var selected = BuildSelectExpression(input, outputElement, convert);
            var factory = BuildImmutableCollectionFactory(outputKind, outputElement, selected);
            var value = ConverterMappingProcessor.IsNullable(inputMember.Type) && ConverterMappingProcessor.IsNullable(outputMember.Type)
                ? $"{input} is null ? null : {factory}"
                : factory;
            contents.Add($"{output} = {value};");
            return;
        }

        if (TryGetGeneric(outputType, TypeFullNames.ICollectionT, out _) is false)
            return;
        var inputNullable = ConverterMappingProcessor.IsNullable(inputMember.Type);
        if (inputNullable && ConverterMappingProcessor.IsNullable(outputMember.Type) is false)
        {
            _context.ClassInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.NullableCollectionRequiresNullableTarget,
                    inputMember.Locations[0],
                    inputMember.AccessPath,
                    outputMember.AccessPath
                )
            );
            return;
        }
        if (inputNullable)
        {
            contents.Add($"if ({input} is null) {output} = null;");
            contents.Add("else");
        }
        contents.Add("{");
        contents.Add($"if ({output} is null) {output} = new(); else {output}.Clear();");
        contents.Add($"foreach (var item in {input}) {output}.Add({convert("item")});");
        contents.Add("}");
    }

    private void AppendDictionaryMapping(
        List<string> contents,
        string input,
        string output,
        MapMemberInfo inputMember,
        MapMemberInfo outputMember,
        ITypeSymbol outputType,
        string? outputKind,
        INamedTypeSymbol outputKey,
        INamedTypeSymbol outputValue,
        Func<string, string> convertKey,
        Func<string, string> convertValue
    )
    {
        if (outputKind is not null)
        {
            if (ConverterMappingProcessor.IsNullable(inputMember.Type) && ConverterMappingProcessor.IsNullable(outputMember.Type) is false)
            {
                _context.ClassInfo.ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.NullableCollectionRequiresNullableTarget,
                        inputMember.Locations[0],
                        inputMember.AccessPath,
                        outputMember.AccessPath
                    )
                );
                return;
            }
            var pairType =
                $"global::System.Collections.Generic.KeyValuePair<{outputKey.GetGlobalFullName()}, {outputValue.GetGlobalFullName()}>";
            var selected =
                $"global::System.Linq.Enumerable.Select({input}, item => new {pairType}({convertKey("item.Key")}, {convertValue("item.Value")}))";
            var factory = BuildImmutableDictionaryFactory(
                outputKind,
                outputKey,
                outputValue,
                selected
            );
            var value = ConverterMappingProcessor.IsNullable(inputMember.Type) && ConverterMappingProcessor.IsNullable(outputMember.Type)
                ? $"{input} is null ? null : {factory}"
                : factory;
            contents.Add($"{output} = {value};");
            return;
        }

        if (TryGetGeneric(outputType, TypeFullNames.IDictionaryT, out _) is false)
            return;
        var inputNullable = ConverterMappingProcessor.IsNullable(inputMember.Type);
        if (inputNullable && ConverterMappingProcessor.IsNullable(outputMember.Type) is false)
        {
            _context.ClassInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.NullableCollectionRequiresNullableTarget,
                    inputMember.Locations[0],
                    inputMember.AccessPath,
                    outputMember.AccessPath
                )
            );
            return;
        }
        if (inputNullable)
        {
            contents.Add($"if ({input} is null) {output} = null;");
            contents.Add("else");
        }
        contents.Add("{");
        contents.Add($"if ({output} is null) {output} = new(); else {output}.Clear();");
        contents.Add(
            $"foreach (var item in {input}) {output}.Add({convertKey("item.Key")}, {convertValue("item.Value")});"
        );
        contents.Add("}");
    }

    private static string BuildSelectExpression(
        string input,
        INamedTypeSymbol outputElement,
        Func<string, string> convert
    ) =>
        $"global::System.Linq.Enumerable.Select({input}, item => ({outputElement.GetGlobalFullName()})({convert("item")}))";

    private static string BuildImmutableCollectionFactory(
        string kind,
        INamedTypeSymbol element,
        string values
    ) =>
        kind switch
        {
            "ImmutableArray" => $"global::System.Collections.Immutable.ImmutableArray.CreateRange<{element.GetGlobalFullName()}>({values})",
            "ImmutableList" => $"global::System.Collections.Immutable.ImmutableList.CreateRange<{element.GetGlobalFullName()}>({values})",
            "ImmutableHashSet" => $"global::System.Collections.Immutable.ImmutableHashSet.CreateRange<{element.GetGlobalFullName()}>({values})",
            "FrozenSet" => $"global::System.Collections.Frozen.FrozenSet.ToFrozenSet<{element.GetGlobalFullName()}>({values})",
            _ => throw new InvalidOperationException(),
        };

    private static string BuildImmutableDictionaryFactory(
        string kind,
        INamedTypeSymbol key,
        INamedTypeSymbol value,
        string values
    ) =>
        kind switch
        {
            "ImmutableDictionary" => $"global::System.Collections.Immutable.ImmutableDictionary.CreateRange<{key.GetGlobalFullName()}, {value.GetGlobalFullName()}>({values})",
            "FrozenDictionary" => $"global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary<{key.GetGlobalFullName()}, {value.GetGlobalFullName()}>({values})",
            _ => throw new InvalidOperationException(),
        };

    private static bool TryGetEnumerableElementType(
        ITypeSymbol type,
        out INamedTypeSymbol element
    )
    {
        if (type is IArrayTypeSymbol array && array.ElementType is INamedTypeSymbol arrayElement)
        {
            element = arrayElement;
            return true;
        }
        if (
            type is INamedTypeSymbol named
            && GetImmutableCollectionKind(type) is not null
            && IsDictionaryKind(GetImmutableCollectionKind(type)) is false
            && named.TypeArguments[0] is INamedTypeSymbol namedElement
        )
        {
            element = namedElement;
            return true;
        }
        if (TryGetGeneric(type, "System.Collections.Generic.IEnumerable<T>", out var args))
        {
            element = args[0];
            return true;
        }
        element = null!;
        return false;
    }

    private static bool TryGetDictionaryTypes(
        ITypeSymbol type,
        out INamedTypeSymbol key,
        out INamedTypeSymbol value
    )
    {
        if (
            type is INamedTypeSymbol named
            && IsDictionaryKind(GetImmutableCollectionKind(type))
            && named.TypeArguments[0] is INamedTypeSymbol namedKey
            && named.TypeArguments[1] is INamedTypeSymbol namedValue
        )
        {
            key = namedKey;
            value = namedValue;
            return true;
        }
        if (
            TryGetGeneric(type, TypeFullNames.IDictionaryT, out var args)
            || TryGetGeneric(type, "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>", out args)
        )
        {
            key = args[0];
            value = args[1];
            return true;
        }
        key = null!;
        value = null!;
        return false;
    }

    private static string? GetImmutableCollectionKind(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
            return null;
        var metadataName = $"{named.OriginalDefinition.ContainingNamespace}.{named.OriginalDefinition.MetadataName}";
        return metadataName switch
        {
            "System.Collections.Immutable.ImmutableArray`1" => "ImmutableArray",
            "System.Collections.Immutable.ImmutableList`1" => "ImmutableList",
            "System.Collections.Immutable.ImmutableHashSet`1" => "ImmutableHashSet",
            "System.Collections.Immutable.ImmutableDictionary`2" => "ImmutableDictionary",
            "System.Collections.Frozen.FrozenSet`1" => "FrozenSet",
            "System.Collections.Frozen.FrozenDictionary`2" => "FrozenDictionary",
            _ => null,
        };
    }

    private static bool IsDictionaryKind(string? kind) =>
        kind is "ImmutableDictionary" or "FrozenDictionary";

    private bool TryMapCollectionProperty(
        MapTargetInfo mapTarget,
        MapMemberInfo sourceProperty,
        MapMemberInfo targetProperty,
        ITypeSymbol sourceType,
        ITypeSymbol targetType
    )
    {
        if (
            TryGetGeneric(sourceType, TypeFullNames.ICollectionT, out var sourceArgs) is false
            || TryGetGeneric(targetType, TypeFullNames.ICollectionT, out var targetArgs) is false
            || TryGetElementMapping(sourceArgs[0], targetArgs[0], out var map) is false
        )
            return false;
        var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.AccessPath}";
        var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}";
        if (map.CanMapTo)
        {
            mapTarget.MapToMethod.Contents.Add("{");
            var targetItem = map.ToTarget!("item");
            mapTarget.MapToMethod.Contents.Add(
                $"if ({target} is null) {target} = new(); else {target}.Clear();"
            );
            mapTarget.MapToMethod.Contents.Add(
                $"foreach (var item in {source}) {target}.Add({targetItem});"
            );
            mapTarget.MapToMethod.Contents.Add("}");
        }
        if (map.CanMapFrom)
        {
            mapTarget.MapFromMethod.Contents.Add("{");
            var sourceItem = map.ToSource!("item");
            mapTarget.MapFromMethod.Contents.Add(
                $"if ({source} is null) {source} = new(); else {source}.Clear();"
            );
            mapTarget.MapFromMethod.Contents.Add(
                $"foreach (var item in {target}) {source}.Add({sourceItem});"
            );
            mapTarget.MapFromMethod.Contents.Add("}");
        }
        return true;
    }

    private bool TryMapDictionaryProperty(
        MapTargetInfo mapTarget,
        MapMemberInfo sourceProperty,
        MapMemberInfo targetProperty,
        ITypeSymbol sourceType,
        ITypeSymbol targetType
    )
    {
        if (
            TryGetGeneric(sourceType, TypeFullNames.IDictionaryT, out var sourceArgs) is false
            || TryGetGeneric(targetType, TypeFullNames.IDictionaryT, out var targetArgs) is false
            || TryGetElementMapping(sourceArgs[0], targetArgs[0], out var keyMap) is false
            || TryGetElementMapping(sourceArgs[1], targetArgs[1], out var valueMap) is false
        )
            return false;
        var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.AccessPath}";
        var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}";
        mapTarget.MapToMethod.Contents.Add("{");
        if (keyMap.CanMapTo && valueMap.CanMapTo)
        {
            var targetKey = keyMap.ToTarget!("item.Key");
            var targetValue = valueMap.ToTarget!("item.Value");
            mapTarget.MapToMethod.Contents.Add(
                $"if ({target} is null) {target} = new(); else {target}.Clear();"
            );
            mapTarget.MapToMethod.Contents.Add(
                $"foreach (var item in {source}) {target}.Add({targetKey}, {targetValue});"
            );
        }
        mapTarget.MapToMethod.Contents.Add("}");
        mapTarget.MapFromMethod.Contents.Add("{");
        if (keyMap.CanMapFrom && valueMap.CanMapFrom)
        {
            var sourceKey = keyMap.ToSource!("item.Key");
            var sourceValue = valueMap.ToSource!("item.Value");
            mapTarget.MapFromMethod.Contents.Add(
                $"if ({source} is null) {source} = new(); else {source}.Clear();"
            );
            mapTarget.MapFromMethod.Contents.Add(
                $"foreach (var item in {target}) {source}.Add({sourceKey}, {sourceValue});"
            );
        }
        mapTarget.MapFromMethod.Contents.Add("}");
        return true;
    }

    private bool TryGetElementMapping(
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        out ElementMapping mapping
    )
    {
        if (sourceType.SymbolEquals(targetType))
        {
            mapping = new(value => value, value => value, true, true);
            return true;
        }
        mapping = default;
        if (_context.MapTargetDic.TryGetValue(sourceType, out var targets) is false)
            return false;
        var nested = targets.FirstOrDefault(t => t.TargetType.SymbolEquals(targetType));
        var canMapTo = nested?.Direction.HasFlag(MapDirections.To) is true;
        var canMapFrom = nested?.Direction.HasFlag(MapDirections.From) is true;
        if (
            (canMapTo || canMapFrom)
            && targetType is INamedTypeSymbol namedTarget
            && namedTarget.InstanceConstructors.Any(c =>
                c.Parameters.Length == 0 && c.DeclaredAccessibility >= Accessibility.Internal
            )
        )
        {
            var nestedTarget = nested!;
            var extensionType = nestedTarget.SourceType.GetFullName().ReplaceDotToUnderline();
            mapping = new(
                canMapTo
                    ? value =>
                        $"global::HKW.Mapper.{extensionType}MapExtensions.{nestedTarget.MapToName}({value}, new {targetType.GetGlobalFullName()}())"
                    : null,
                canMapFrom
                    ? value =>
                        $"global::HKW.Mapper.{extensionType}MapExtensions.{nestedTarget.MapFromName}(new {sourceType.GetGlobalFullName()}(), {value})"
                    : null,
                canMapTo,
                canMapFrom
            );
            return true;
        }
        return false;
    }

    private static bool TryGetGeneric(
        ITypeSymbol type,
        string metadataShape,
        out INamedTypeSymbol[] arguments
    )
    {
        arguments = [];
        if (type is not INamedTypeSymbol named)
            return false;
        var matchingInterface = named.GetInterface(metadataShape);
        if (matchingInterface is null)
            return false;
        arguments = matchingInterface.TypeArguments.Cast<INamedTypeSymbol>().ToArray();
        return true;
    }
}
