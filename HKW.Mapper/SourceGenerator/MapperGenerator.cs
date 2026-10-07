using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWMapper.SourceGenerator;

internal class MapperGenerator
{
    public static void Generate(
        ClassInfo classInfo,
        IReadOnlyDictionary<INamedTypeSymbol, HashSet<MapTargetInfo>> mapTargetDic
    )
    {
        var x = new MapperGenerator(classInfo, mapTargetDic);
        x.Execute();
    }

    public MapperGenerator(
        ClassInfo classInfo,
        IReadOnlyDictionary<INamedTypeSymbol, HashSet<MapTargetInfo>> mapTargetDic
    )
    {
        _classInfo = classInfo;
        MapTargetDic = mapTargetDic;
    }

    private readonly ClassInfo _classInfo;
    private readonly Dictionary<
        MapTargetInfo,
        Dictionary<string, IPropertySymbol>
    > _mappedTargets = [];
    private int _compositeVariableIndex;

    public IReadOnlyDictionary<INamedTypeSymbol, HashSet<MapTargetInfo>> MapTargetDic { get; }

    public void Execute()
    {
        foreach (var target in _classInfo.MapTargets)
        {
            TryAddConfig(target);
            AddStartAction(target);
            foreach (var property in _classInfo.Properties)
            {
                ParseProperty(target, property);
            }
            AddEndAction(target);
            if (target.Direction.HasFlag(MapDirections.To))
                target.MapToMethod.Contents.Add($"return {MapTargetInfo.TargetParamName};");
            if (target.Direction.HasFlag(MapDirections.From))
                target.MapFromMethod.Contents.Add($"return {MapTargetInfo.SourceParamName};");
        }
    }

    private void ParseProperty(MapTargetInfo mapTarget, IPropertySymbol propertySymbol)
    {
        var atts = propertySymbol.GetAttributes();
        // 检查全局忽略属性
        if (
            atts.Any(a =>
                a.AttributeClass?.GetGlobalFullName() == TypeFullNames.MapIgnorePropertyAttribute
            )
        )
            return;
        if (TryUseConfigCompositePropertyConverter(mapTarget, propertySymbol))
            return;
        var compositeAttributes = atts.Where(a =>
                a.AttributeClass?.GetGlobalFullName() == TypeFullNames.MapCompositePropertyAttribute
            )
            .ToArray();
        var matchingCompositeAttributes = GetMatchingAttributes(
            mapTarget,
            propertySymbol,
            compositeAttributes,
            GetCompositePropertyTargetName
        );
        if (matchingCompositeAttributes is null)
            return;
        if (matchingCompositeAttributes.Count == 1)
        {
            MapCompositeProperty(mapTarget, propertySymbol, matchingCompositeAttributes[0]);
            return;
        }

        var propertyAttributes = atts.Where(a =>
                a.AttributeClass?.GetGlobalFullName() == TypeFullNames.MapPropertyAttribute
            )
            .ToArray();
        // 获取当前映射目标有效的特性
        var matchingAttributes = new List<AttributeData>();
        foreach (var att in propertyAttributes)
        {
            var targetName = GetMapPropertyTargetName(att);
            if (mapTarget.TargetName == targetName)
            {
                matchingAttributes.Add(att);
            }
            else if (_classInfo.MapTargets.All(x => x.TargetName != targetName))
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.MapPropertyTargetNotFound,
                    att.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                        att.ApplicationSyntaxReference.Span
                    ),
                    targetName
                );
                _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            }
        }
        if (matchingAttributes.Count > 1)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.MapPropertyTargetAmbiguous,
                propertySymbol.Locations[0],
                propertySymbol.Name
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        var attributeData = matchingAttributes.FirstOrDefault();

        var attributeInfo = attributeData is null ? null : new AttributeInfo(attributeData);
        // 指定特性忽略
        if (
            attributeInfo?.TryGetParam<bool>(nameof(MapPropertyAttribute.Ignore), out var ignore)
                is true
            && ignore is true
        )
            return;

        if (
            attributeInfo?.TryGetParam<string>(
                nameof(MapPropertyAttribute.PropertyName),
                out var targetPropertyName
            )
            is not true
        )
            targetPropertyName = propertySymbol.Name;

        var targetProperty = GetTargetProperty(mapTarget, propertySymbol, targetPropertyName);
        if (targetProperty is null)
            return;
        if (TryReserveTargetProperties(mapTarget, propertySymbol, [targetProperty]) is false)
            return;
        AddBeforePropertyAction(mapTarget, propertySymbol);
        if (
            TryUseConfigPropertyConverter(mapTarget, propertySymbol, targetProperty) is false
            && TryUsePropertyConverter(mapTarget, propertySymbol, attributeInfo, targetProperty)
                is false
        )
        {
            MapProperty(mapTarget, propertySymbol, attributeInfo, targetProperty);
        }
        TryReplacePropertyAction(mapTarget, propertySymbol);
        AddAfterPropertyAction(mapTarget, propertySymbol);
    }

    private List<AttributeData>? GetMatchingAttributes(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        IEnumerable<AttributeData> attributes,
        Func<AttributeData, string> getTargetName
    )
    {
        var matchingAttributes = new List<AttributeData>();
        foreach (var attribute in attributes)
        {
            var targetName = getTargetName(attribute);
            if (mapTarget.TargetName == targetName)
            {
                matchingAttributes.Add(attribute);
            }
            else if (_classInfo.MapTargets.All(x => x.TargetName != targetName))
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.MapPropertyTargetNotFound,
                    attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                        attribute.ApplicationSyntaxReference.Span
                    ),
                    targetName
                );
                _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            }
        }
        if (matchingAttributes.Count <= 1)
            return matchingAttributes;

        var ambiguousDiagnostic = Diagnostic.Create(
            Descriptors.MapPropertyTargetAmbiguous,
            propertySymbol.Locations[0],
            propertySymbol.Name
        );
        _classInfo.ProductionContext.ReportDiagnostic(ambiguousDiagnostic);
        return null;
    }

    private static string GetCompositePropertyTargetName(AttributeData attribute)
    {
        var target = attribute.ConstructorArguments[0];
        return target.Value is INamedTypeSymbol targetType
            ? targetType.Name
            : target.Value as string ?? string.Empty;
    }

    private static string GetMapPropertyTargetName(AttributeData attribute)
    {
        var attributeInfo = attribute.GetInfo()!;
        var targetName = string.Empty;
        if (
            attributeInfo.TryGetParam<INamedTypeSymbol>(
                nameof(MapPropertyAttribute.TargetType),
                out var targetType
            )
        )
        {
            targetName = targetType.Name;
        }
        else if (
            attributeInfo.TryGetParam<string>(nameof(MapPropertyAttribute.TargetName), out var name)
        )
        {
            targetName = name;
        }

        return targetName;
    }

    private static void TryReplacePropertyAction(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol
    )
    {
        if (mapTarget.ConfigInfo is null)
            return;
        if (
            mapTarget.ConfigInfo.MapTo.ReplacePropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapToMethod
            )
        )
        {
            mapTarget.MapToMethod.Contents.RemoveAt(mapTarget.MapToMethod.Contents.Count - 1);
            mapTarget.MapToMethod.Contents.Add($"// Replace {propertySymbol.Name}");
            mapTarget.MapToMethod.Contents.Add(
                mapToMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }

        if (
            mapTarget.ConfigInfo.MapFrom.ReplacePropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapFromMethod
            )
        )
        {
            mapTarget.MapFromMethod.Contents.RemoveAt(mapTarget.MapFromMethod.Contents.Count - 1);
            mapTarget.MapFromMethod.Contents.Add($"// Replace {propertySymbol.Name}");
            mapTarget.MapFromMethod.Contents.Add(
                mapFromMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }

    private static void AddBeforePropertyAction(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol
    )
    {
        if (mapTarget.ConfigInfo is null)
            return;
        if (
            mapTarget.ConfigInfo.MapTo.BeforePropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapToMethod
            )
        )
        {
            mapTarget.MapToMethod.Contents.Add(
                mapToMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }

        if (
            mapTarget.ConfigInfo.MapFrom.BeforePropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapFromMethod
            )
        )
        {
            mapTarget.MapFromMethod.Contents.Add(
                mapFromMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }

    private static void AddAfterPropertyAction(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol
    )
    {
        if (mapTarget.ConfigInfo is null)
            return;
        if (
            mapTarget.ConfigInfo.MapTo.AfterPropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapToMethod
            )
        )
        {
            mapTarget.MapToMethod.Contents.Add(
                mapToMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }

        if (
            mapTarget.ConfigInfo.MapFrom.AfterPropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapFromMethod
            )
        )
        {
            mapTarget.MapFromMethod.Contents.Add(
                mapFromMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }

    private void MapProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        AttributeInfo? attributeInfo,
        IPropertySymbol targetProperty
    )
    {
        if (
            attributeInfo?.TryGetParam<MapPropertyType>(
                nameof(MapPropertyAttribute.MapType),
                out var mapType
            )
            is not true
        )
            mapType = default;

        if (mapType is MapPropertyType.ConstructFromSelf)
        {
            mapTarget.MapToMethod.Contents.Add(
                $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = new({MapTargetInfo.SourceParamName}.{propertySymbol.Name});"
            );
            mapTarget.MapFromMethod.Contents.Add(
                $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = new({MapTargetInfo.TargetParamName}.{targetProperty.Name});"
            );
            return;
        }

        if (TryMapComplexProperty(mapTarget, propertySymbol, targetProperty))
            return;

        // 比较当前属性与目标属性的类型
        if (propertySymbol.Type.SymbolEquals(targetProperty.Type) is false)
        {
            if (TryMapNullableProperty(mapTarget, propertySymbol, targetProperty))
                return;
            // 如果当前属性类型与目标属性类型不一样, 则异常
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyTypeError,
                propertySymbol.Locations[0],
                targetProperty.ToString()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        if (propertySymbol.Type.SpecialType != SpecialType.System_String)
        {
            if (propertySymbol.Type.HasInterface(TypeFullNames.ICloneable))
            {
                // 如果实现了 ICloneable, 则克隆
                mapTarget.MapToMethod.Contents.Add(
                    $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = ({propertySymbol.Type.GetGlobalFullName()}){MapTargetInfo.SourceParamName}.{propertySymbol.Name}?.{nameof(ICloneable.Clone)}()!;"
                );
                mapTarget.MapFromMethod.Contents.Add(
                    $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = ({targetProperty.Type.GetGlobalFullName()}){MapTargetInfo.TargetParamName}.{targetProperty.Name}?.{nameof(ICloneable.Clone)}()!;"
                );
                return;
            }
            if (propertySymbol.Type.IsReferenceType && mapType is not MapPropertyType.Reference)
            {
                // 如果是引用类型, 则错误
                var diagnostic = Diagnostic.Create(
                    Descriptors.PropertyIsReferenceType,
                    propertySymbol.Locations[0],
                    $"{mapTarget.SourceType.Name}.{propertySymbol.Name}"
                );
                _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
                return;
            }
        }

        // 正常赋值
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = {MapTargetInfo.SourceParamName}.{propertySymbol.Name};"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = {MapTargetInfo.TargetParamName}.{targetProperty.Name};"
        );
    }

    private static bool TryMapNullableProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty
    )
    {
        var sourceType = UnwrapNullable(sourceProperty.Type);
        var targetType = UnwrapNullable(targetProperty.Type);
        if (sourceType.SymbolEquals(targetType) is false)
            return false;

        var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.Name}";
        var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}";
        var sourceNullable = IsNullable(sourceProperty.Type);
        var targetNullable = IsNullable(targetProperty.Type);
        var toExpression =
            sourceNullable && targetNullable is false ? $"{source} ?? default!" : source;
        var fromExpression =
            targetNullable && sourceNullable is false ? $"{target} ?? default!" : target;
        mapTarget.MapToMethod.Contents.Add($"{target} = {toExpression};");
        mapTarget.MapFromMethod.Contents.Add($"{source} = {fromExpression};");
        return sourceNullable || targetNullable;
    }

    private static bool IsNullable(ITypeSymbol type)
    {
        return type.NullableAnnotation == NullableAnnotation.Annotated
            || type is INamedTypeSymbol named
                && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    private bool TryMapComplexProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty
    )
    {
        var sourceType = UnwrapNullable(sourceProperty.Type);
        var targetType = UnwrapNullable(targetProperty.Type);

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
            var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.Name}";
            var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}";
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

    private bool TryMapCollectionProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty,
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
        var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.Name}";
        var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}";
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
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty,
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
        var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.Name}";
        var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}";
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
        if (MapTargetDic.TryGetValue(sourceType, out var targets) is false)
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
                        $"global::HKW.HKWMapper.{extensionType}MapExtensions.{nestedTarget.MapToName}({value}, new {targetType.GetGlobalFullName()}())"
                    : null,
                canMapFrom
                    ? value =>
                        $"global::HKW.HKWMapper.{extensionType}MapExtensions.{nestedTarget.MapFromName}(new {sourceType.GetGlobalFullName()}(), {value})"
                    : null,
                canMapTo,
                canMapFrom
            );
            return true;
        }
        return false;
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (
            type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        )
            return named.TypeArguments[0];
        return type;
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
        var matchingInterface = named.AllInterfaces.FirstOrDefault(i =>
            i.OriginalDefinition.GetGlobalFullName() == metadataShape
        );
        if (matchingInterface is null)
            return false;
        arguments = matchingInterface.TypeArguments.Cast<INamedTypeSymbol>().ToArray();
        return true;
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

    private bool TryUseConfigCompositePropertyConverter(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol
    )
    {
        if (
            mapTarget.ConfigInfo?.CompositeConverters.TryGetValue(propertySymbol.Name, out var data)
            is not true
        )
            return false;

        var converterType = (INamedTypeSymbol)data.Property.Type;
        var converterSourceType = converterType.TypeArguments[0];
        var location = data.Property.Locations[0];
        if (AreConverterTypesCompatible(converterSourceType, propertySymbol.Type) is false)
        {
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.CompositeSourceTypeMismatch,
                    location,
                    converterType.TypeArguments[0].GetName(),
                    propertySymbol.Type.GetName()
                )
            );
            return true;
        }
        if (
            converterType.TypeArguments[1] is not INamedTypeSymbol tupleType
            || tupleType.IsTupleType is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.CompositeConverterRequiresTuple,
                location,
                converterType.GetName()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return true;
        }

        GenerateConfigCompositeMapping(
            mapTarget,
            propertySymbol,
            converterSourceType,
            data.TargetNames,
            tupleType,
            $"{MapConfigInfo.ConfigName}.{data.Property.Name}",
            location
        );
        return true;
    }

    private void GenerateConfigCompositeMapping(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        ITypeSymbol converterSourceType,
        string[] targetPropertyNames,
        INamedTypeSymbol tupleType,
        string converterExpression,
        Location location
    )
    {
        var tupleElements = tupleType.TupleElements;
        if (tupleElements.Length != targetPropertyNames.Length)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.CompositePropertyCountMismatch,
                location,
                targetPropertyNames.Length,
                tupleElements.Length
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        var targetProperties = new List<IPropertySymbol>(targetPropertyNames.Length);
        for (var i = 0; i < targetPropertyNames.Length; i++)
        {
            var targetProperty = GetCompositeTargetProperty(
                mapTarget,
                propertySymbol,
                targetPropertyNames[i]
            );
            if (targetProperty is null)
                return;
            if (AreConverterTypesCompatible(tupleElements[i].Type, targetProperty.Type) is false)
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.CompositePropertyTypeMismatch,
                    location,
                    targetProperty.Name,
                    targetProperty.Type.GetName(),
                    i + 1,
                    tupleElements[i].Type.GetName()
                );
                _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
                return;
            }
            targetProperties.Add(targetProperty);
        }
        if (TryReserveTargetProperties(mapTarget, propertySymbol, targetProperties) is false)
            return;

        AddBeforePropertyAction(mapTarget, propertySymbol);
        var mapToContentStart = mapTarget.MapToMethod.Contents.Count;
        var mapFromContentStart = mapTarget.MapFromMethod.Contents.Count;
        var sourceValue = ConvertExpression(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name}",
            propertySymbol.Type,
            converterSourceType
        );
        mapTarget.MapToMethod.Contents.Add(
            $"({string.Join(", ", targetProperties.Select(x => $"{MapTargetInfo.TargetParamName}.{x.Name}"))}) = {converterExpression}.{nameof(ICompositeMapConverter<object, ValueTuple>.Convert)}({MapTargetInfo.SourceParamName}, {sourceValue});"
        );
        var targetValues = string.Join(
            ", ",
            targetProperties.Select(
                (x, i) =>
                    ConvertExpression(
                        $"{MapTargetInfo.TargetParamName}.{x.Name}",
                        x.Type,
                        tupleElements[i].Type
                    )
            )
        );
        var convertedSourceValue =
            $"{converterExpression}.{nameof(ICompositeMapConverter<object, ValueTuple>.ConvertBack)}({MapTargetInfo.TargetParamName}, ({targetValues}))";
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = {ConvertExpression(convertedSourceValue, converterSourceType, propertySymbol.Type)};"
        );
        TryReplaceCompositePropertyAction(
            mapTarget,
            propertySymbol,
            mapToContentStart,
            mapFromContentStart
        );
        AddAfterPropertyAction(mapTarget, propertySymbol);
    }

    private void MapCompositeProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        AttributeData attribute
    )
    {
        var location = attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
            attribute.ApplicationSyntaxReference.Span
        );
        if (attribute.ConstructorArguments[1].Value is not INamedTypeSymbol converterType)
            return;

        var targetPropertyNames = attribute
            .ConstructorArguments[2]
            .Values.Select(x => x.Value as string ?? string.Empty)
            .ToArray();
        if (
            targetPropertyNames.Length < 2
            || targetPropertyNames.Any(string.IsNullOrWhiteSpace)
            || targetPropertyNames.Distinct(StringComparer.Ordinal).Count()
                != targetPropertyNames.Length
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.CompositeTargetPropertiesInvalid,
                location
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        var converterInterface = converterType.AllInterfaces.FirstOrDefault(i =>
            i.OriginalDefinition.GetGlobalFullName() == TypeFullNames.CompositeMapConverterInterface
        );
        if (converterInterface is null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.CompositeConverterNotImplemented,
                location,
                converterType.GetName()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        var converterSourceType = converterInterface.TypeArguments[0];
        if (AreConverterTypesCompatible(converterSourceType, propertySymbol.Type) is false)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.CompositeSourceTypeMismatch,
                location,
                converterInterface.TypeArguments[0].GetName(),
                propertySymbol.Type.GetName()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        if (
            converterInterface.TypeArguments[1] is not INamedTypeSymbol tupleType
            || tupleType.IsTupleType is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.CompositeConverterRequiresTuple,
                location,
                converterType.GetName()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        _classInfo.AddConverters(converterType, out var fieldName);
        GenerateConfigCompositeMapping(
            mapTarget,
            propertySymbol,
            converterSourceType,
            targetPropertyNames,
            tupleType,
            fieldName,
            location
        );
    }

    private static void TryReplaceCompositePropertyAction(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        int mapToContentStart,
        int mapFromContentStart
    )
    {
        if (mapTarget.ConfigInfo is null)
            return;
        if (
            mapTarget.ConfigInfo.MapTo.ReplacePropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapToMethod
            )
        )
        {
            mapTarget.MapToMethod.Contents.RemoveRange(
                mapToContentStart,
                mapTarget.MapToMethod.Contents.Count - mapToContentStart
            );
            mapTarget.MapToMethod.Contents.Add($"// Replace {propertySymbol.Name}");
            mapTarget.MapToMethod.Contents.Add(
                mapToMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
        if (
            mapTarget.ConfigInfo.MapFrom.ReplacePropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapFromMethod
            )
        )
        {
            mapTarget.MapFromMethod.Contents.RemoveRange(
                mapFromContentStart,
                mapTarget.MapFromMethod.Contents.Count - mapFromContentStart
            );
            mapTarget.MapFromMethod.Contents.Add($"// Replace {propertySymbol.Name}");
            mapTarget.MapFromMethod.Contents.Add(
                mapFromMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }

    private bool TryReserveTargetProperties(
        MapTargetInfo mapTarget,
        IPropertySymbol sourceProperty,
        IEnumerable<IPropertySymbol> targetProperties
    )
    {
        if (_mappedTargets.TryGetValue(mapTarget, out var mappedProperties) is false)
        {
            mappedProperties = new(StringComparer.Ordinal);
            _mappedTargets.Add(mapTarget, mappedProperties);
        }
        foreach (var targetProperty in targetProperties)
        {
            if (mappedProperties.TryGetValue(targetProperty.Name, out var firstSourceProperty))
            {
                _classInfo.ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.SameMapTargetProperty,
                        sourceProperty.Locations[0],
                        firstSourceProperty.Name,
                        sourceProperty.Name,
                        targetProperty.Name
                    )
                );
                return false;
            }
        }
        foreach (var targetProperty in targetProperties)
            mappedProperties.Add(targetProperty.Name, sourceProperty);
        return true;
    }

    private static void TryAddConfig(MapTargetInfo mapTarget)
    {
        if (mapTarget.ConfigInfo is null)
            return;
        mapTarget.MapToMethod.Contents.Add(
            $"var {MapConfigInfo.ConfigName} = new {mapTarget.ConfigInfo.Type.GetGlobalFullName()}({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName});"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"var {MapConfigInfo.ConfigName} = new {mapTarget.ConfigInfo.Type.GetGlobalFullName()}({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName});"
        );
    }

    private bool AreConverterTypesCompatible(ITypeSymbol converterType, ITypeSymbol propertyType)
    {
        return HasImplicitCompatibleConversion(converterType, propertyType)
            || HasImplicitCompatibleConversion(propertyType, converterType);
    }

    private bool HasImplicitCompatibleConversion(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        var conversion = ClassifyConversion(sourceType, targetType);
        return conversion.Exists
            && conversion.IsImplicit
            && (conversion.IsIdentity || conversion.IsReference || conversion.IsBoxing);
    }

    private string ConvertExpression(
        string expression,
        ITypeSymbol sourceType,
        ITypeSymbol targetType
    )
    {
        var conversion = ClassifyConversion(sourceType, targetType);
        if (conversion.Exists && conversion.IsImplicit)
            return expression;
        return $"({targetType.GetGlobalFullName()})({expression})";
    }

    private Conversion ClassifyConversion(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        return ((CSharpCompilation)_classInfo.Compilation).ClassifyConversion(
            sourceType,
            targetType
        );
    }

    private bool TryUseConfigPropertyConverter(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        IPropertySymbol targetProperty
    )
    {
        if (
            mapTarget.ConfigInfo?.Converters.TryGetValue(propertySymbol.Name, out var data)
            is not true
        )
            return false;

        var converterType = (INamedTypeSymbol)data.Property.Type;
        var converterSourceType = converterType.TypeArguments[0];
        var converterTargetType = converterType.TypeArguments[1];
        if (
            AreConverterTypesCompatible(converterSourceType, propertySymbol.Type) is false
            || AreConverterTypesCompatible(converterTargetType, targetProperty.Type) is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterGenericTypeError,
                data.Property.Locations[0],
                converterType.TypeArguments[0].GetName(),
                converterType.TypeArguments[1].GetName(),
                propertySymbol.Type.GetName(),
                targetProperty.Type.GetName()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return false;
        }
        var convertName = $"{MapConfigInfo.ConfigName}.{data.Property.Name}";
        var sourceValue = ConvertExpression(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name}",
            propertySymbol.Type,
            converterSourceType
        );
        var convertedTargetValue =
            $"{convertName}.{nameof(IMapConverter<,>.Convert)}({MapTargetInfo.SourceParamName}, {sourceValue})";
        var targetValue = ConvertExpression(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}",
            targetProperty.Type,
            converterTargetType
        );
        var convertedSourceValue =
            $"{convertName}.{nameof(IMapConverter<,>.ConvertBack)}({MapTargetInfo.TargetParamName}, {targetValue})";
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = {ConvertExpression(convertedTargetValue, converterTargetType, targetProperty.Type)};"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = {ConvertExpression(convertedSourceValue, converterSourceType, propertySymbol.Type)};"
        );
        return true;
    }

    private bool TryUsePropertyConverter(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        AttributeInfo? attributeInfo,
        IPropertySymbol targetProperty
    )
    {
        if (
            attributeInfo?.TryGetParam<INamedTypeSymbol>(
                nameof(MapPropertyAttribute.ConverterType),
                out var converterType
            )
                is not true
            || converterType is null
        )
            return false;

        var converterInterface = converterType.Interfaces.FirstOrDefault(i =>
            i.OriginalDefinition.GetGlobalFullName() == TypeFullNames.MapConverterInterface
        );
        // 判断转换器是否实现转换器接口
        if (converterInterface is null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterNotImplementIMapConverter,
                attributeInfo.Data.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                    attributeInfo.Data.ApplicationSyntaxReference.Span
                ),
                targetProperty.ToString()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return false;
        }
        var converterSourceType = converterInterface.TypeArguments[0];
        var converterTargetType = converterInterface.TypeArguments[1];
        if (
            AreConverterTypesCompatible(converterSourceType, propertySymbol.Type) is false
            || AreConverterTypesCompatible(converterTargetType, targetProperty.Type) is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterGenericTypeError,
                attributeInfo.Data.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                    attributeInfo.Data.ApplicationSyntaxReference.Span
                ),
                converterInterface.TypeArguments[0].GetName(),
                converterInterface.TypeArguments[1].GetName(),
                propertySymbol.Type.GetName(),
                targetProperty.Type.GetName()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return false;
        }
        _classInfo.AddConverters(converterType, out var fieldName);
        var sourceValue = ConvertExpression(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name}",
            propertySymbol.Type,
            converterSourceType
        );
        var convertedTargetValue =
            $"{fieldName}.{nameof(IMapConverter<,>.Convert)}({MapTargetInfo.SourceParamName}, {sourceValue})";
        var targetValue = ConvertExpression(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}",
            targetProperty.Type,
            converterTargetType
        );
        var convertedSourceValue =
            $"{fieldName}.{nameof(IMapConverter<,>.ConvertBack)}({MapTargetInfo.TargetParamName}, {targetValue})";
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = {ConvertExpression(convertedTargetValue, converterTargetType, targetProperty.Type)};"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = {ConvertExpression(convertedSourceValue, converterSourceType, propertySymbol.Type)};"
        );
        return true;
    }

    private IPropertySymbol? GetCompositeTargetProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol property,
        string targetPropertyName
    )
    {
        if (mapTarget.Direction.HasFlag(MapDirections.To))
            return GetTargetProperty(mapTarget, property, targetPropertyName);
        if (
            mapTarget.PropertyByName.TryGetValue(targetPropertyName, out var targetProperty)
            is false
        )
        {
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.TargetPropertyNotExists,
                    property.Locations[0],
                    mapTarget.TargetType.GetName() + "." + targetPropertyName
                )
            );
            return null;
        }
        if (targetProperty.IsStatic)
        {
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.TargetPropertyIsStatic,
                    property.Locations[0],
                    targetProperty.ToString()
                )
            );
            return null;
        }
        if (
            targetProperty.GetMethod is null
            || IsAccessorAccessible(targetProperty.GetMethod) is false
        )
        {
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.TargetPropertyAccessibilityInsufficient,
                    property.Locations[0],
                    targetProperty.ToString()
                )
            );
            return null;
        }
        return targetProperty;
    }

    private bool IsAccessorAccessible(IMethodSymbol accessor)
    {
        if (accessor.DeclaredAccessibility == Accessibility.Public)
            return true;
        return (
                accessor.DeclaredAccessibility == Accessibility.Internal
                || accessor.DeclaredAccessibility == Accessibility.ProtectedOrInternal
            ) && accessor.ContainingAssembly.SymbolEquals(_classInfo.Compilation.Assembly);
    }

    private IPropertySymbol? GetTargetProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol property,
        string targetPropertyName
    )
    {
        // 目标属性不存在
        if (
            mapTarget.PropertyByName.TryGetValue(targetPropertyName, out var targetProperty)
            is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyNotExists,
                property.Locations[0],
                mapTarget.TargetType.GetName() + "." + property.Name
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        // 只读属性
        if (targetProperty.SetMethod is null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyIsReadOnly,
                property.Locations[0],
                targetProperty.ToString()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        // 静态属性
        if (targetProperty.IsStatic)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyIsStatic,
                property.Locations[0],
                targetProperty.ToString()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        if (targetProperty.SetMethod.DeclaredAccessibility == Accessibility.Public)
            return targetProperty;
        // 可访问性为本地或保护本地
        if (
            targetProperty.SetMethod.DeclaredAccessibility == Accessibility.Internal
            || targetProperty.SetMethod.DeclaredAccessibility == Accessibility.ProtectedOrInternal
        )
        {
            // 在主程序集
            if (
                targetProperty.SetMethod.ContainingAssembly.SymbolEquals(
                    _classInfo.Compilation.Assembly
                )
            )
                return targetProperty;

            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyAccessibilityInsufficient,
                property.Locations[0],
                targetProperty.ToString()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        // 更低的可访问性
        if (targetProperty.SetMethod.DeclaredAccessibility < Accessibility.Internal)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyAccessibilityInsufficient,
                property.Locations[0],
                targetProperty.ToString()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        return targetProperty;
    }

    private static void AddStartAction(MapTargetInfo mapTarget)
    {
        if (mapTarget.ConfigInfo is null)
            return;
        foreach (var action in mapTarget.ConfigInfo.MapTo.StartActions)
        {
            mapTarget.MapToMethod.Contents.Add(
                action.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }

        foreach (var action in mapTarget.ConfigInfo.MapFrom.StartActions)
        {
            mapTarget.MapFromMethod.Contents.Add(
                action.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }

    private static void AddEndAction(MapTargetInfo mapTarget)
    {
        if (mapTarget.ConfigInfo is null)
            return;
        foreach (var action in mapTarget.ConfigInfo.MapTo.EndActions)
        {
            mapTarget.MapToMethod.Contents.Add(
                action.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }

        foreach (var action in mapTarget.ConfigInfo.MapFrom.EndActions)
        {
            mapTarget.MapFromMethod.Contents.Add(
                action.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }
}
