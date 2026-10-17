using System;
using System.Collections.Generic;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.Mapper.SourceGenerator;

internal sealed class CompositeMappingProcessor
{
    private readonly MapperGenerationContext _context;
    private readonly MappingActionBuilder _actions;
    private readonly ConverterMappingProcessor _converters;
    private readonly MemberPathResolver _resolver;

    internal CompositeMappingProcessor(
        MapperGenerationContext context,
        MappingActionBuilder actions,
        ConverterMappingProcessor converters,
        MemberPathResolver resolver
    )
    {
        _context = context;
        _actions = actions;
        _converters = converters;
        _resolver = resolver;
    }

    internal bool TryUseConfigCompositePropertyConverter(
        MapTargetInfo mapTarget,
        MapMemberInfo propertySymbol,
        AttributeInfo? attributeInfo
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
        if (_converters.AreConverterTypesCompatible(converterSourceType, propertySymbol.Type) is false)
        {
            _context.ClassInfo.ProductionContext.ReportDiagnostic(
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
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
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
            mapTarget.Direction,
            attributeInfo
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
        MapDirections direction,
        AttributeInfo? attributeInfo
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
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        var targetProperties = new List<MapMemberInfo>(targetPropertyNames.Length);
        for (var i = 0; i < targetPropertyNames.Length; i++)
        {
            var targetProperty = _resolver.GetTargetProperty(
                mapTarget,
                propertySymbol,
                targetPropertyNames[i],
                direction
            );
            if (targetProperty is null)
                return;
            if (_converters.AreConverterTypesCompatible(tupleElements[i].Type, targetProperty.Type) is false)
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.CompositeTargetMemberTypeMismatch,
                    location,
                    targetProperty.Name,
                    targetProperty.Type.GetName(),
                    i + 1,
                    tupleElements[i].Type.GetName()
                );
                _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
                return;
            }
            targetProperties.Add(targetProperty);
        }

        var mapToCondition = MemberConditionBuilder.GetCondition(
            attributeInfo,
            nameof(MapCompositePropertyAttribute.MapToCondition)
        );
        var mapFromCondition = MemberConditionBuilder.GetCondition(
            attributeInfo,
            nameof(MapCompositePropertyAttribute.MapFromCondition)
        );
        if (
            mapToCondition is not MapMemberCondition.None
            && direction.HasFlag(MapDirections.To)
        )
        {
            var unsupportedTargets = targetProperties
                .Where(x => x.IsInitOnly || x.IsRequired)
                .ToArray();
            if (unsupportedTargets.Length > 0)
            {
                foreach (var targetProperty in unsupportedTargets)
                {
                    _context.ClassInfo.ProductionContext.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.ConditionalTargetMemberCannotBeInitialized,
                            location,
                            targetProperty.AccessPath
                        )
                    );
                }
                direction &= ~MapDirections.To;
            }
        }
        if (direction == 0)
            return;
        if (
            _actions.TryReserveTargetProperties(mapTarget, propertySymbol, targetProperties, direction)
            is false
        )
            return;

        var mapToContentStart = mapTarget.MapToMethod.Contents.Count;
        var mapFromContentStart = mapTarget.MapFromMethod.Contents.Count;
        _actions.AddBeforePropertyAction(mapTarget, actionProperty);
        var sourceValue = _converters.ConvertExpression(
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
            var convertedValue = _converters.ConvertExpression(
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
                    _converters.ConvertExpression(
                        $"{MapTargetInfo.TargetParamName}.{x.Name}",
                        x.Type,
                        tupleElements[i].Type
                    )
            )
        );
        var convertedSourceValue =
            $"{converterExpression}.{nameof(IMapCompositeConverter<object, ValueTuple>.ConvertBack)}(({targetValues}), {convertArgs})";
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath} = {_converters.ConvertExpression(convertedSourceValue, converterSourceType, propertySymbol.Type)};"
        );
        _actions.TryReplaceCompositePropertyAction(
            mapTarget,
            actionProperty,
            mapToContentStart,
            mapFromContentStart
        );
        MemberConditionBuilder.WrapMapping(
            mapTarget.MapToMethod.Contents,
            mapToContentStart,
            mapToCondition,
            propertySymbol,
            targetProperties
        );
        MemberConditionBuilder.WrapMapping(
            mapTarget.MapFromMethod.Contents,
            mapFromContentStart,
            mapFromCondition,
            propertySymbol,
            targetProperties
        );
        _actions.AddAfterPropertyAction(mapTarget, actionProperty);
        if (
            direction.HasFlag(MapDirections.To)
            && targetProperties.Any(x => x.IsInitOnly)
        )
        {
            mapTarget.MapToMethod.Contents.RemoveRange(
                mapToContentStart,
                mapTarget.MapToMethod.Contents.Count - mapToContentStart
            );
            foreach (var targetProperty in targetProperties.Where(x => x.IsInitOnly))
            {
                _context.ClassInfo.ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.InitTargetMemberCannotBeCreated,
                        location,
                        targetProperty.AccessPath
                    )
                );
            }
        }
        _actions.RemoveInitOnlySourceMapFrom(
            mapTarget,
            propertySymbol,
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

    internal void MapCompositeProperty(
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
        var direction = _actions.GetEffectiveDirection(
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
            sourceProperty = _resolver.ResolveSourcePath(mapTarget, propertySymbol, sourcePath, direction);
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
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
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
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        var converterSourceType = converterInterface.TypeArguments[0];
        if (_converters.AreConverterTypesCompatible(converterSourceType, sourceProperty.Type) is false)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.CompositeSourceTypeMismatch,
                location,
                converterInterface.TypeArguments[0].GetName(),
                sourceProperty.Type.GetName()
            );
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
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
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        _context.ClassInfo.AddConverters(converterType, out var fieldName);
        GenerateCompositeMapping(
            mapTarget,
            sourceProperty,
            propertySymbol,
            converterSourceType,
            targetPropertyNames,
            tupleType,
            fieldName,
            location,
            direction,
            attributeInfo
        );
    }
}
