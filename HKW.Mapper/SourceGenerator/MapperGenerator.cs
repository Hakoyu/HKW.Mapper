using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.Mapper.SourceGenerator;

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
        Dictionary<string, List<(MapMemberInfo Source, MapDirections Direction)>>
    > _mappedTargets = [];
    private readonly HashSet<MapMemberInfo> _reportedIgnoreAttributeConflicts = [];

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
            if (target.MapParentProperties)
            {
                foreach (var property in _classInfo.ParentProperties)
                    ParseProperty(target, property);
            }
            if (target.MapFields)
            {
                foreach (var field in _classInfo.Fields)
                    ParseProperty(target, field);
                if (target.MapParentFields)
                {
                    foreach (var field in _classInfo.ParentFields)
                        ParseProperty(target, field);
                }
            }
            AddEndAction(target);
            if (target.Direction.HasFlag(MapDirections.To))
                target.MapToMethod.Contents.Add($"return {MapTargetInfo.TargetParamName};");
            if (target.Direction.HasFlag(MapDirections.From))
                target.MapFromMethod.Contents.Add($"return {MapTargetInfo.SourceParamName};");
        }
    }

    private void ParseProperty(MapTargetInfo mapTarget, MapMemberInfo propertySymbol)
    {
        var isField = propertySymbol.IsField;
        if (
            (isField ? mapTarget.IgnoreFields : mapTarget.IgnoreProperties).Contains(
                propertySymbol.Name
            )
        )
            return;

        var atts = propertySymbol.GetAttributes();
        var ignoreAttributeName = isField
            ? TypeFullNames.MapIgnoreFieldAttribute
            : TypeFullNames.MapIgnorePropertyAttribute;
        var compositeAttributeName = isField
            ? TypeFullNames.MapCompositeFieldAttribute
            : TypeFullNames.MapCompositePropertyAttribute;
        var memberAttributeName = isField
            ? TypeFullNames.MapFieldAttribute
            : TypeFullNames.MapPropertyAttribute;
        var ignoreAttribute = atts.FirstOrDefault(a =>
            a.AttributeClass!.IsName(ignoreAttributeName)
        );
        var compositeAttributes = atts.Where(a => a.AttributeClass!.IsName(compositeAttributeName))
            .ToArray();
        var propertyAttributes = atts.Where(a => a.AttributeClass!.IsName(memberAttributeName))
            .ToArray();
        // 检查全局忽略属性
        if (ignoreAttribute is not null)
        {
            if (
                (compositeAttributes.Length > 0 || propertyAttributes.Length > 0)
                && _reportedIgnoreAttributeConflicts.Add(propertySymbol)
            )
            {
                var syntaxReference = ignoreAttribute.ApplicationSyntaxReference!;
                _classInfo.ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.MapIgnoreOverridesMappingAttributes,
                        syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                    )
                );
            }
            return;
        }
        var targetCompositeAttributes = compositeAttributes
            .Where(attribute => GetCompositePropertyTargetName(attribute) == mapTarget.TargetName)
            .ToArray();
        if (
            targetCompositeAttributes.Length == 1
            && propertyAttributes.Any(attribute =>
                GetMapPropertyTargetName(attribute) == mapTarget.TargetName
            )
        )
        {
            var syntaxReference = targetCompositeAttributes[0].ApplicationSyntaxReference!;
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.CompositeMappingOverridesRegularMapping,
                    syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                )
            );
        }
        if (TryUseConfigCompositePropertyConverter(mapTarget, propertySymbol))
        {
            foreach (var attribute in compositeAttributes)
            {
                if (GetCompositePropertyTargetName(attribute) == mapTarget.TargetName)
                    ReportConfigConverterOverridesAttribute(attribute);
            }
            return;
        }
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
                    Descriptors.MemberMappingTargetNotFound,
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
                Descriptors.MemberMappingTargetAmbiguous,
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

        var targetMemberParameter = isField
            ? nameof(MapFieldAttribute.FieldName)
            : nameof(MapPropertyAttribute.PropertyName);
        if (
            attributeInfo?.TryGetParam<string>(targetMemberParameter, out var targetPropertyName)
            is not true
        )
            targetPropertyName = propertySymbol.Name;
        var hasConfigConverter = false;
        if (
            mapTarget.ConfigInfo?.Converters.TryGetValue(propertySymbol.Name, out var configData)
                is true
            && configData.IsFieldConverter == isField
        )
        {
            targetPropertyName = configData.TargetName;
            hasConfigConverter = true;
        }

        var direction = hasConfigConverter
            ? mapTarget.Direction
            : GetEffectiveDirection(
                mapTarget,
                attributeInfo,
                attributeData,
                nameof(MapPropertyAttribute.Direction)
            );
        if (direction == 0)
            return;

        var sourceProperty = propertySymbol;
        if (
            hasConfigConverter is false
            && attributeInfo?.TryGetParam<string>(
                nameof(MapPropertyAttribute.SourcePath),
                out var sourcePath
            )
                is true
        )
        {
            sourceProperty = ResolveSourcePath(mapTarget, propertySymbol, sourcePath, direction);
            if (sourceProperty is null)
                return;
        }
        var hasAttributeConverter =
            attributeInfo?.TryGetParam<INamedTypeSymbol>(
                nameof(MapPropertyAttribute.ConverterType),
                out var attributeConverterType
            )
                is true
            && attributeConverterType is not null;

        var targetProperty = GetTargetProperty(
            mapTarget,
            sourceProperty,
            targetPropertyName,
            direction,
            allowPath: true
        );
        if (targetProperty is null)
            return;
        if (
            hasConfigConverter is false
            && hasAttributeConverter is false
            && TryReserveTargetProperties(mapTarget, sourceProperty, [targetProperty], direction)
                is false
        )
            return;
        var mapToContentStart = mapTarget.MapToMethod.Contents.Count;
        var mapFromContentStart = mapTarget.MapFromMethod.Contents.Count;
        AddBeforePropertyAction(mapTarget, propertySymbol);
        if (TryUseConfigPropertyConverter(mapTarget, sourceProperty, targetProperty))
        {
            if (
                attributeInfo?.TryGetParam<INamedTypeSymbol>(
                    nameof(MapPropertyAttribute.ConverterType),
                    out var converterType
                )
                    is true
                && converterType is not null
            )
                ReportConfigConverterOverridesAttribute(attributeData!);
        }
        else if (
            TryUsePropertyConverter(mapTarget, sourceProperty, attributeInfo, targetProperty)
            is false
        )
        {
            MapProperty(mapTarget, sourceProperty, attributeInfo, targetProperty);
        }
        TryReplacePropertyAction(mapTarget, propertySymbol);
        AddAfterPropertyAction(mapTarget, propertySymbol);
        RemoveDisabledDirectionContents(
            mapTarget,
            direction,
            mapToContentStart,
            mapFromContentStart
        );
    }

    private MapDirections GetEffectiveDirection(
        MapTargetInfo mapTarget,
        AttributeInfo? attributeInfo,
        AttributeData? attributeData,
        string parameterName
    )
    {
        var memberDirection = MapDirections.Both;
        if (attributeInfo?.TryGetParam<MapDirections>(parameterName, out var direction) is true)
            memberDirection = direction;

        var effectiveDirection = mapTarget.Direction & memberDirection;
        if (effectiveDirection == 0 && attributeData is not null)
        {
            var syntaxReference = attributeData.ApplicationSyntaxReference!;
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.MemberDirectionConflictsWithMapTarget,
                    syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span),
                    memberDirection,
                    mapTarget.Direction
                )
            );
        }
        return effectiveDirection;
    }

    private static void RemoveDisabledDirectionContents(
        MapTargetInfo mapTarget,
        MapDirections direction,
        int mapToContentStart,
        int mapFromContentStart
    )
    {
        if (direction.HasFlag(MapDirections.To) is false)
            mapTarget.MapToMethod.Contents.RemoveRange(
                mapToContentStart,
                mapTarget.MapToMethod.Contents.Count - mapToContentStart
            );
        if (direction.HasFlag(MapDirections.From) is false)
            mapTarget.MapFromMethod.Contents.RemoveRange(
                mapFromContentStart,
                mapTarget.MapFromMethod.Contents.Count - mapFromContentStart
            );
    }

    private List<AttributeData>? GetMatchingAttributes(
        MapTargetInfo mapTarget,
        MapMemberInfo propertySymbol,
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
                    Descriptors.MemberMappingTargetNotFound,
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
            Descriptors.MemberMappingTargetAmbiguous,
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

    private void ReportConfigConverterOverridesAttribute(AttributeData attribute)
    {
        var syntaxReference = attribute.ApplicationSyntaxReference!;
        _classInfo.ProductionContext.ReportDiagnostic(
            Diagnostic.Create(
                Descriptors.MapConfigConverterOverridesAttribute,
                syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
            )
        );
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
        MapMemberInfo propertySymbol
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
        MapMemberInfo propertySymbol
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
        MapMemberInfo propertySymbol
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
        MapMemberInfo propertySymbol,
        AttributeInfo? attributeInfo,
        MapMemberInfo targetProperty
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
                $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath} = new({MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath});"
            );
            mapTarget.MapFromMethod.Contents.Add(
                $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath} = new({MapTargetInfo.TargetParamName}.{targetProperty.AccessPath});"
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
                Descriptors.TargetMemberTypeIncompatible,
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
                    $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath} = ({propertySymbol.Type.GetGlobalFullName()}){MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath}?.{nameof(ICloneable.Clone)}()!;"
                );
                mapTarget.MapFromMethod.Contents.Add(
                    $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath} = ({targetProperty.Type.GetGlobalFullName()}){MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}?.{nameof(ICloneable.Clone)}()!;"
                );
                return;
            }
            if (propertySymbol.Type.IsReferenceType && mapType is not MapPropertyType.Reference)
            {
                // 如果是引用类型, 则错误
                var diagnostic = Diagnostic.Create(
                    Descriptors.SourceMemberIsReferenceType,
                    propertySymbol.Locations[0],
                    $"{mapTarget.SourceType.Name}.{propertySymbol.Name}"
                );
                _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
                return;
            }
        }

        // 正常赋值
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath} = {MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath};"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath} = {MapTargetInfo.TargetParamName}.{targetProperty.AccessPath};"
        );
    }

    private static bool TryMapNullableProperty(
        MapTargetInfo mapTarget,
        MapMemberInfo sourceProperty,
        MapMemberInfo targetProperty
    )
    {
        var sourceType = UnwrapNullable(sourceProperty.Type);
        var targetType = UnwrapNullable(targetProperty.Type);
        if (sourceType.SymbolEquals(targetType) is false)
            return false;

        var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.AccessPath}";
        var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}";
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
        MapMemberInfo sourceProperty,
        MapMemberInfo targetProperty
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
        var matchingInterface = named.GetInterface(metadataShape);
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
        MapMemberInfo propertySymbol
    )
    {
        if (
            mapTarget.ConfigInfo?.CompositeConverters.TryGetValue(propertySymbol.Name, out var data)
                is not true
            || data.IsFieldConverter != propertySymbol.IsField
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

        GenerateCompositeMapping(
            mapTarget,
            propertySymbol,
            propertySymbol,
            converterSourceType,
            data.TargetNames,
            tupleType,
            $"{MapConfigInfo.ConfigName}.{data.Property.Name}",
            location,
            mapTarget.Direction
        );
        return true;
    }

    private void GenerateCompositeMapping(
        MapTargetInfo mapTarget,
        MapMemberInfo propertySymbol,
        MapMemberInfo actionProperty,
        ITypeSymbol converterSourceType,
        string[] targetPropertyNames,
        INamedTypeSymbol tupleType,
        string converterExpression,
        Location location,
        MapDirections direction
    )
    {
        var tupleElements = tupleType.TupleElements;
        if (tupleElements.Length != targetPropertyNames.Length)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.CompositeTargetMemberCountMismatch,
                location,
                targetPropertyNames.Length,
                tupleElements.Length
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        var targetProperties = new List<MapMemberInfo>(targetPropertyNames.Length);
        for (var i = 0; i < targetPropertyNames.Length; i++)
        {
            var targetProperty = GetTargetProperty(
                mapTarget,
                propertySymbol,
                targetPropertyNames[i],
                direction
            );
            if (targetProperty is null)
                return;
            if (AreConverterTypesCompatible(tupleElements[i].Type, targetProperty.Type) is false)
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.CompositeTargetMemberTypeMismatch,
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
        if (
            TryReserveTargetProperties(mapTarget, propertySymbol, targetProperties, direction)
            is false
        )
            return;

        var mapToContentStart = mapTarget.MapToMethod.Contents.Count;
        var mapFromContentStart = mapTarget.MapFromMethod.Contents.Count;
        AddBeforePropertyAction(mapTarget, actionProperty);
        var sourceValue = ConvertExpression(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath}",
            propertySymbol.Type,
            converterSourceType
        );
        var targetPropertyNameLiterals = string.Join(
            ", ",
            targetProperties.Select(x => $"\"{x.Name}\"")
        );
        var convertArgs =
            $"new global::HKW.Mapper.MapCompositeConvertArgs({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName}, \"{propertySymbol.AccessPath}\", new global::System.String[] {{ {targetPropertyNameLiterals} }})";
        mapTarget.MapToMethod.Contents.Add("{");
        mapTarget.MapToMethod.Contents.Add(
            $"var result = {converterExpression}.{nameof(IMapCompositeConverter<object, ValueTuple>.Convert)}({sourceValue}, {convertArgs});"
        );
        for (var i = 0; i < targetProperties.Count; i++)
        {
            var targetProperty = targetProperties[i];
            var convertedValue = ConvertExpression(
                $"result.Item{i + 1}",
                tupleElements[i].Type,
                targetProperty.Type
            );
            mapTarget.MapToMethod.Contents.Add(
                $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = {convertedValue};"
            );
        }
        mapTarget.MapToMethod.Contents.Add("}");

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
            $"{converterExpression}.{nameof(IMapCompositeConverter<object, ValueTuple>.ConvertBack)}(({targetValues}), {convertArgs})";
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath} = {ConvertExpression(convertedSourceValue, converterSourceType, propertySymbol.Type)};"
        );
        TryReplaceCompositePropertyAction(
            mapTarget,
            actionProperty,
            mapToContentStart,
            mapFromContentStart
        );
        AddAfterPropertyAction(mapTarget, actionProperty);
        RemoveDisabledDirectionContents(
            mapTarget,
            direction,
            mapToContentStart,
            mapFromContentStart
        );
    }

    private void MapCompositeProperty(
        MapTargetInfo mapTarget,
        MapMemberInfo propertySymbol,
        AttributeData attribute
    )
    {
        var location = attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
            attribute.ApplicationSyntaxReference.Span
        );
        if (attribute.ConstructorArguments[1].Value is not INamedTypeSymbol converterType)
            return;

        var attributeInfo = new AttributeInfo(attribute);
        var direction = GetEffectiveDirection(
            mapTarget,
            attributeInfo,
            attribute,
            nameof(MapCompositePropertyAttribute.Direction)
        );
        if (direction == 0)
            return;

        var sourceProperty = propertySymbol;
        if (
            attributeInfo.TryGetParam<string>(
                nameof(MapCompositePropertyAttribute.SourcePath),
                out var sourcePath
            )
        )
        {
            sourceProperty = ResolveSourcePath(mapTarget, propertySymbol, sourcePath, direction);
            if (sourceProperty is null)
                return;
        }

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
                Descriptors.InvalidCompositeTargetMemberNames,
                location
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        var converterInterface = converterType.GetInterface(
            TypeFullNames.MapCompositeConverterInterface
        );
        if (converterInterface is null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterDoesNotImplementIMapCompositeConverter,
                location,
                converterType.GetName()
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        var converterSourceType = converterInterface.TypeArguments[0];
        if (AreConverterTypesCompatible(converterSourceType, sourceProperty.Type) is false)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.CompositeSourceTypeMismatch,
                location,
                converterInterface.TypeArguments[0].GetName(),
                sourceProperty.Type.GetName()
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
        GenerateCompositeMapping(
            mapTarget,
            sourceProperty,
            propertySymbol,
            converterSourceType,
            targetPropertyNames,
            tupleType,
            fieldName,
            location,
            direction
        );
    }

    private static void TryReplaceCompositePropertyAction(
        MapTargetInfo mapTarget,
        MapMemberInfo propertySymbol,
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
        MapMemberInfo sourceProperty,
        IEnumerable<MapMemberInfo> targetProperties,
        MapDirections direction
    )
    {
        if (_mappedTargets.TryGetValue(mapTarget, out var mappedProperties) is false)
        {
            mappedProperties = new(StringComparer.Ordinal);
            _mappedTargets.Add(mapTarget, mappedProperties);
        }
        foreach (var targetProperty in targetProperties)
        {
            if (
                mappedProperties.TryGetValue(targetProperty.AccessPath, out var mappings)
                && mappings.FirstOrDefault(mapping => (mapping.Direction & direction) != 0)
                    is var conflictingMapping
                && conflictingMapping.Source is not null
            )
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.DuplicateMapTargetMember,
                    sourceProperty.Locations[0],
                    conflictingMapping.Source.Name,
                    sourceProperty.Name,
                    targetProperty.AccessPath
                );
                _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
                return false;
            }
        }
        foreach (var targetProperty in targetProperties)
        {
            if (mappedProperties.TryGetValue(targetProperty.AccessPath, out var mappings) is false)
            {
                mappings = [];
                mappedProperties.Add(targetProperty.AccessPath, mappings);
            }
            mappings.Add((sourceProperty, direction));
        }
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
        MapMemberInfo propertySymbol,
        MapMemberInfo targetProperty
    )
    {
        if (
            mapTarget.ConfigInfo?.Converters.TryGetValue(propertySymbol.Name, out var data)
                is not true
            || data.IsFieldConverter != propertySymbol.IsField
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
                Descriptors.ConverterGenericTypeMismatch,
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
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath}",
            propertySymbol.Type,
            converterSourceType
        );
        var convertArgs =
            $"new global::HKW.Mapper.MapConvertArgs({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName}, \"{propertySymbol.AccessPath}\", \"{targetProperty.AccessPath}\")";
        var convertedTargetValue =
            $"{convertName}.{nameof(IMapConverter<,>.Convert)}({sourceValue}, {convertArgs})";
        var targetValue = ConvertExpression(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}",
            targetProperty.Type,
            converterTargetType
        );
        var convertedSourceValue =
            $"{convertName}.{nameof(IMapConverter<,>.ConvertBack)}({targetValue}, {convertArgs})";
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath} = {ConvertExpression(convertedTargetValue, converterTargetType, targetProperty.Type)};"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath} = {ConvertExpression(convertedSourceValue, converterSourceType, propertySymbol.Type)};"
        );
        return true;
    }

    private bool TryUsePropertyConverter(
        MapTargetInfo mapTarget,
        MapMemberInfo propertySymbol,
        AttributeInfo? attributeInfo,
        MapMemberInfo targetProperty
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

        var converterInterface = converterType.GetInterface(TypeFullNames.MapConverterInterface);
        // 判断转换器是否实现转换器接口
        if (converterInterface is null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterDoesNotImplementIMapConverter,
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
                Descriptors.ConverterGenericTypeMismatch,
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
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath}",
            propertySymbol.Type,
            converterSourceType
        );
        var convertArgs =
            $"new global::HKW.Mapper.MapConvertArgs({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName}, \"{propertySymbol.AccessPath}\", \"{targetProperty.AccessPath}\")";
        var convertedTargetValue =
            $"{fieldName}.{nameof(IMapConverter<,>.Convert)}({sourceValue}, {convertArgs})";
        var targetValue = ConvertExpression(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}",
            targetProperty.Type,
            converterTargetType
        );
        var convertedSourceValue =
            $"{fieldName}.{nameof(IMapConverter<,>.ConvertBack)}({targetValue}, {convertArgs})";
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath} = {ConvertExpression(convertedTargetValue, converterTargetType, targetProperty.Type)};"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath} = {ConvertExpression(convertedSourceValue, converterSourceType, propertySymbol.Type)};"
        );
        return true;
    }

    private MapMemberInfo? GetTargetProperty(
        MapTargetInfo mapTarget,
        MapMemberInfo property,
        string targetPropertyName,
        MapDirections direction,
        bool allowPath = false
    )
    {
        MapMemberInfo? targetProperty;
        if (allowPath && targetPropertyName.IndexOf('.') >= 0)
        {
            targetProperty = ResolveTargetPath(mapTarget, property, targetPropertyName, direction);
            if (targetProperty is null)
                return null;
        }
        else if (
            mapTarget.MemberByName.TryGetValue(targetPropertyName, out targetProperty) is false
        )
        {
            ReportTargetPathNotFound(mapTarget, property, targetPropertyName);
            return null;
        }

        // 静态属性
        if (targetProperty.IsStatic)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetMemberIsStatic,
                property.Locations[0],
                targetProperty.AccessPath
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        if (property.IsField && IsMemberAccessible(property.Symbol) is false)
        {
            ReportSourceAccessorDiagnostic(mapTarget, property, "field");
            return null;
        }
        if (targetProperty.IsField && IsMemberAccessible(targetProperty.Symbol) is false)
        {
            ReportTargetAccessorDiagnostic(property, targetProperty, "field");
            return null;
        }
        if (
            property.IsField
            && direction.HasFlag(MapDirections.From)
            && property.IsReadOnly
        )
        {
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.FieldIsReadOnly,
                    GetPropertyDiagnosticLocation(mapTarget, property),
                    property.Name,
                    nameof(MapDirections.From)
                )
            );
            return null;
        }
        if (
            targetProperty.IsField
            && direction.HasFlag(MapDirections.To)
            && targetProperty.IsReadOnly
        )
        {
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.FieldIsReadOnly,
                    property.Locations[0],
                    targetProperty.AccessPath,
                    nameof(MapDirections.To)
                )
            );
            return null;
        }

        if (
            property.IsField is false
            && direction.HasFlag(MapDirections.To)
            && (property.GetMethod is null || IsMemberAccessible(property.GetMethod) is false)
        )
        {
            ReportSourceAccessorDiagnostic(mapTarget, property, "getter");
            return null;
        }
        if (
            property.IsField is false
            && direction.HasFlag(MapDirections.From)
            && (property.SetMethod is null || IsMemberAccessible(property.SetMethod) is false)
        )
        {
            ReportSourceAccessorDiagnostic(mapTarget, property, "setter");
            return null;
        }
        if (
            targetProperty.IsField is false
            && direction.HasFlag(MapDirections.To)
            && (
                targetProperty.SetMethod is null
                || IsMemberAccessible(targetProperty.SetMethod) is false
            )
        )
        {
            ReportTargetAccessorDiagnostic(property, targetProperty, "setter");
            return null;
        }
        if (
            targetProperty.IsField is false
            && direction.HasFlag(MapDirections.From)
            && (
                targetProperty.GetMethod is null
                || IsMemberAccessible(targetProperty.GetMethod) is false
            )
        )
        {
            ReportTargetAccessorDiagnostic(property, targetProperty, "getter");
            return null;
        }
        return targetProperty;
    }

    private MapMemberInfo? ResolveSourcePath(
        MapTargetInfo mapTarget,
        MapMemberInfo annotatedMember,
        string sourcePath,
        MapDirections direction
    )
    {
        var segments = sourcePath.Split('.');
        if (segments.Any(string.IsNullOrWhiteSpace))
        {
            ReportSourcePathNotFound(mapTarget, annotatedMember, sourcePath);
            return null;
        }

        ITypeSymbol currentType = mapTarget.SourceType;
        for (var i = 0; i < segments.Length; i++)
        {
            var symbol = FindTargetPathMember(currentType, segments[i], mapTarget.MapFields);
            if (symbol is null)
            {
                ReportSourcePathNotFound(mapTarget, annotatedMember, sourcePath);
                return null;
            }

            var member = symbol switch
            {
                IPropertySymbol property => new MapMemberInfo(property, sourcePath),
                IFieldSymbol field => new MapMemberInfo(field, sourcePath),
                _ => null,
            };
            if (member is null)
            {
                ReportSourcePathNotFound(mapTarget, annotatedMember, sourcePath);
                return null;
            }

            var segmentPath = string.Join(".", segments, 0, i + 1);
            if (member.IsStatic)
            {
                _classInfo.ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.SourcePathMemberIsStatic,
                        annotatedMember.Locations[0],
                        segmentPath
                    )
                );
                return null;
            }

            var isLeaf = i == segments.Length - 1;
            if (member.IsField)
            {
                if (IsMemberAccessible(member.Symbol) is false)
                {
                    ReportSourceAccessorDiagnostic(mapTarget, annotatedMember, "field");
                    return null;
                }

                if (
                    direction.HasFlag(MapDirections.From)
                    && member.IsReadOnly
                    && (isLeaf || member.Type.IsValueType)
                )
                {
                    _classInfo.ProductionContext.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.FieldIsReadOnly,
                            annotatedMember.Locations[0],
                            segmentPath,
                            nameof(MapDirections.From)
                        )
                    );
                    return null;
                }
            }
            else if (isLeaf)
            {
                if (
                    direction.HasFlag(MapDirections.To)
                    && (member.GetMethod is null || IsMemberAccessible(member.GetMethod) is false)
                )
                {
                    ReportSourceAccessorDiagnostic(mapTarget, annotatedMember, "getter");
                    return null;
                }
                if (
                    direction.HasFlag(MapDirections.From)
                    && (member.SetMethod is null || IsMemberAccessible(member.SetMethod) is false)
                )
                {
                    ReportSourceAccessorDiagnostic(mapTarget, annotatedMember, "setter");
                    return null;
                }
            }
            else
            {
                if (member.GetMethod is null || IsMemberAccessible(member.GetMethod) is false)
                {
                    ReportSourceAccessorDiagnostic(mapTarget, annotatedMember, "getter");
                    return null;
                }
                if (
                    direction.HasFlag(MapDirections.From)
                    && member.Type.IsValueType
                    && member.Symbol is IPropertySymbol property
                    && (property.ReturnsByRef is false || property.ReturnsByRefReadonly)
                )
                {
                    _classInfo.ProductionContext.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.SourcePathValueTypeNotWritable,
                            annotatedMember.Locations[0],
                            segmentPath,
                            sourcePath
                        )
                    );
                    return null;
                }
            }

            if (isLeaf)
                return member;

            currentType = UnwrapNullable(member.Type);
        }

        return null;
    }

    private MapMemberInfo? ResolveTargetPath(
        MapTargetInfo mapTarget,
        MapMemberInfo sourceProperty,
        string targetPath,
        MapDirections direction
    )
    {
        var segments = targetPath.Split('.');
        if (segments.Any(string.IsNullOrWhiteSpace))
        {
            ReportTargetPathNotFound(mapTarget, sourceProperty, targetPath);
            return null;
        }

        ITypeSymbol currentType = mapTarget.TargetType;
        for (var i = 0; i < segments.Length; i++)
        {
            var symbol = FindTargetPathMember(currentType, segments[i], mapTarget.MapFields);
            if (symbol is null)
            {
                ReportTargetPathNotFound(mapTarget, sourceProperty, targetPath);
                return null;
            }

            var member = symbol switch
            {
                IPropertySymbol property => new MapMemberInfo(property, targetPath),
                IFieldSymbol field => new MapMemberInfo(field, targetPath),
                _ => null,
            };
            if (member is null)
            {
                ReportTargetPathNotFound(mapTarget, sourceProperty, targetPath);
                return null;
            }

            var isLeaf = i == segments.Length - 1;
            if (isLeaf)
                return member;

            var segmentPath = string.Join(".", segments, 0, i + 1);
            if (member.IsStatic)
            {
                _classInfo.ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.TargetMemberIsStatic,
                        sourceProperty.Locations[0],
                        segmentPath
                    )
                );
                return null;
            }

            if (member.IsField)
            {
                if (IsMemberAccessible(member.Symbol) is false)
                {
                    ReportTargetAccessorDiagnostic(sourceProperty, member, "field");
                    return null;
                }

                if (
                    direction.HasFlag(MapDirections.To)
                    && member.Type.IsValueType
                    && member.IsReadOnly
                )
                {
                    _classInfo.ProductionContext.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.FieldIsReadOnly,
                            sourceProperty.Locations[0],
                            segmentPath,
                            nameof(MapDirections.To)
                        )
                    );
                    return null;
                }
            }
            else
            {
                if (member.GetMethod is null || IsMemberAccessible(member.GetMethod) is false)
                {
                    ReportTargetAccessorDiagnostic(sourceProperty, member, "getter");
                    return null;
                }

                if (
                    direction.HasFlag(MapDirections.To)
                    && member.Type.IsValueType
                    && member.Symbol is IPropertySymbol property
                    && (property.ReturnsByRef is false || property.ReturnsByRefReadonly)
                )
                {
                    _classInfo.ProductionContext.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.TargetPathValueTypeNotWritable,
                            sourceProperty.Locations[0],
                            segmentPath,
                            targetPath
                        )
                    );
                    return null;
                }
            }

            currentType = UnwrapNullable(member.Type);
        }

        return null;
    }

    private static ISymbol? FindTargetPathMember(
        ITypeSymbol containingType,
        string name,
        bool mapFields
    )
    {
        if (containingType is not INamedTypeSymbol namedType)
            return null;

        for (var type = namedType; type is not null; type = type.BaseType)
        {
            var property = type.GetMembers(name).OfType<IPropertySymbol>().FirstOrDefault();
            if (property is not null)
                return property;
        }

        if (mapFields is false)
            return null;

        for (var type = namedType; type is not null; type = type.BaseType)
        {
            var field = type.GetMembers(name)
                .OfType<IFieldSymbol>()
                .FirstOrDefault(x => x.IsImplicitlyDeclared is false);
            if (field is not null)
                return field;
        }

        return null;
    }

    private void ReportSourcePathNotFound(
        MapTargetInfo mapTarget,
        MapMemberInfo property,
        string sourcePath
    )
    {
        _classInfo.ProductionContext.ReportDiagnostic(
            Diagnostic.Create(
                Descriptors.SourcePathMemberDoesNotExist,
                GetPropertyDiagnosticLocation(mapTarget, property),
                mapTarget.SourceType.GetName() + "." + sourcePath
            )
        );
    }

    private void ReportTargetPathNotFound(
        MapTargetInfo mapTarget,
        MapMemberInfo property,
        string targetPath
    )
    {
        _classInfo.ProductionContext.ReportDiagnostic(
            Diagnostic.Create(
                Descriptors.TargetMemberDoesNotExist,
                GetPropertyDiagnosticLocation(mapTarget, property),
                mapTarget.TargetType.GetName() + "." + targetPath
            )
        );
    }

    private void ReportSourceAccessorDiagnostic(
        MapTargetInfo mapTarget,
        MapMemberInfo property,
        string accessor
    )
    {
        _classInfo.ProductionContext.ReportDiagnostic(
            Diagnostic.Create(
                Descriptors.SourceMemberAccessorInaccessible,
                GetPropertyDiagnosticLocation(mapTarget, property),
                property.Name,
                accessor
            )
        );
    }

    private void ReportTargetAccessorDiagnostic(
        MapMemberInfo source,
        MapMemberInfo target,
        string accessor
    )
    {
        _classInfo.ProductionContext.ReportDiagnostic(
            Diagnostic.Create(
                Descriptors.TargetMemberAccessorInaccessible,
                source.Locations[0],
                target.AccessPath,
                accessor
            )
        );
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

    private bool IsMemberAccessible(ISymbol member)
    {
        if (member.DeclaredAccessibility == Accessibility.Public)
            return true;
        return member.DeclaredAccessibility >= Accessibility.Internal
            && member.ContainingAssembly.SymbolEquals(_classInfo.Compilation.Assembly);
    }

    private Location GetPropertyDiagnosticLocation(MapTargetInfo mapTarget, MapMemberInfo property)
    {
        return
            _classInfo.ParentProperties.Contains(property)
            || _classInfo.ParentFields.Contains(property)
            ? mapTarget.MapTargetLocation
            : property.Locations[0];
    }
}
