using System;
using System.Collections.Generic;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.Mapper.SourceGenerator;

internal sealed class MemberMappingProcessor
{
    private readonly MapperGenerationContext _context;
    private readonly MappingActionBuilder _actions;
    private readonly CollectionMappingProcessor _collections;
    private readonly CompositeMappingProcessor _composites;
    private readonly ConverterMappingProcessor _converters;
    private readonly MemberPathResolver _resolver;

    internal MemberMappingProcessor(
        MapperGenerationContext context,
        MappingActionBuilder actions,
        CollectionMappingProcessor collections,
        CompositeMappingProcessor composites,
        ConverterMappingProcessor converters,
        MemberPathResolver resolver
    )
    {
        _context = context;
        _actions = actions;
        _collections = collections;
        _composites = composites;
        _converters = converters;
        _resolver = resolver;
    }

    internal void ParseProperty(MapTargetInfo mapTarget, MapMemberInfo propertySymbol)
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
                && _context.ReportedIgnoreAttributeConflicts.Add(propertySymbol)
            )
            {
                var syntaxReference = ignoreAttribute.ApplicationSyntaxReference!;
                _context.ClassInfo.ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.MapIgnoreOverridesMappingAttributes,
                        syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                    )
                );
            }
            return;
        }
        var targetCompositeAttributes = compositeAttributes
            .Where(attribute => _actions.GetCompositePropertyTargetName(attribute) == mapTarget.TargetName)
            .ToArray();
        if (
            targetCompositeAttributes.Length == 1
            && propertyAttributes.Any(attribute =>
                _actions.GetMapPropertyTargetName(attribute) == mapTarget.TargetName
            )
        )
        {
            var syntaxReference = targetCompositeAttributes[0].ApplicationSyntaxReference!;
            _context.ClassInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.CompositeMappingOverridesRegularMapping,
                    syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
                )
            );
        }
        if (_composites.TryUseConfigCompositePropertyConverter(mapTarget, propertySymbol))
        {
            foreach (var attribute in compositeAttributes)
            {
                if (_actions.GetCompositePropertyTargetName(attribute) == mapTarget.TargetName)
                    _actions.ReportConfigConverterOverridesAttribute(attribute);
            }
            return;
        }
        var matchingCompositeAttributes = _actions.GetMatchingAttributes(
            mapTarget,
            propertySymbol,
            compositeAttributes,
            _actions.GetCompositePropertyTargetName
        );
        if (matchingCompositeAttributes is null)
            return;
        if (matchingCompositeAttributes.Count == 1)
        {
            _composites.MapCompositeProperty(mapTarget, propertySymbol, matchingCompositeAttributes[0]);
            return;
        }

        // 获取当前映射目标有效的特性
        var matchingAttributes = new List<AttributeData>();
        foreach (var att in propertyAttributes)
        {
            var targetName = _actions.GetMapPropertyTargetName(att);
            if (mapTarget.TargetName == targetName)
            {
                matchingAttributes.Add(att);
            }
            else if (_context.ClassInfo.MapTargets.All(x => x.TargetName != targetName))
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.MemberMappingTargetNotFound,
                    att.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                        att.ApplicationSyntaxReference.Span
                    ),
                    targetName
                );
                _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
            }
        }
        if (matchingAttributes.Count > 1)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.MemberMappingTargetAmbiguous,
                propertySymbol.Locations[0],
                propertySymbol.Name
            );
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
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
            : _actions.GetEffectiveDirection(
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
            sourceProperty = _resolver.ResolveSourcePath(mapTarget, propertySymbol, sourcePath, direction);
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

        var targetProperty = _resolver.GetTargetProperty(
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
            && _actions.TryReserveTargetProperties(mapTarget, sourceProperty, [targetProperty], direction)
                is false
        )
            return;
        var mapToContentStart = mapTarget.MapToMethod.Contents.Count;
        var mapFromContentStart = mapTarget.MapFromMethod.Contents.Count;
        _actions.AddBeforePropertyAction(mapTarget, propertySymbol);
        if (_converters.TryUseConfigPropertyConverter(mapTarget, sourceProperty, targetProperty))
        {
            if (
                attributeInfo?.TryGetParam<INamedTypeSymbol>(
                    nameof(MapPropertyAttribute.ConverterType),
                    out var converterType
                )
                    is true
                && converterType is not null
            )
                _actions.ReportConfigConverterOverridesAttribute(attributeData!);
        }
        else if (
            _converters.TryUsePropertyConverter(mapTarget, sourceProperty, attributeInfo, targetProperty)
            is false
        )
        {
            MapProperty(mapTarget, sourceProperty, attributeInfo, targetProperty);
        }
        _actions.TryReplacePropertyAction(mapTarget, propertySymbol);
        _actions.AddAfterPropertyAction(mapTarget, propertySymbol);
        _actions.MoveInitOnlyTargetAssignmentToCreation(
            mapTarget,
            targetProperty,
            propertySymbol,
            mapToContentStart
        );
        _actions.RemoveInitOnlySourceMapFrom(
            mapTarget,
            sourceProperty,
            direction,
            mapFromContentStart
        );
        _actions.RemoveDisabledDirectionContents(
            mapTarget,
            direction,
            mapToContentStart,
            mapFromContentStart
        );
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

        if (_collections.TryMapComplexProperty(mapTarget, propertySymbol, targetProperty))
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
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
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
                _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
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
        var sourceType = ConverterMappingProcessor.UnwrapNullable(sourceProperty.Type);
        var targetType = ConverterMappingProcessor.UnwrapNullable(targetProperty.Type);
        if (sourceType.SymbolEquals(targetType) is false)
            return false;

        var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.AccessPath}";
        var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}";
        var sourceNullable = ConverterMappingProcessor.IsNullable(sourceProperty.Type);
        var targetNullable = ConverterMappingProcessor.IsNullable(targetProperty.Type);
        var toExpression =
            sourceNullable && targetNullable is false ? $"{source} ?? default!" : source;
        var fromExpression =
            targetNullable && sourceNullable is false ? $"{target} ?? default!" : target;
        mapTarget.MapToMethod.Contents.Add($"{target} = {toExpression};");
        mapTarget.MapFromMethod.Contents.Add($"{source} = {fromExpression};");
        return sourceNullable || targetNullable;
    }
}
