using System;
using System.Collections.Generic;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.Mapper.SourceGenerator;

internal sealed class MappingActionBuilder
{
    private readonly MapperGenerationContext _context;

    internal MappingActionBuilder(MapperGenerationContext context)
    {
        _context = context;
    }

    internal MapDirections GetEffectiveDirection(
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
            _context.ClassInfo.ProductionContext.ReportDiagnostic(
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

    internal void RemoveDisabledDirectionContents(
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

    internal void MoveInitOnlyTargetAssignmentToCreation(
        MapTargetInfo mapTarget,
        MapMemberInfo targetMember,
        MapMemberInfo sourceMember,
        int contentStart
    )
    {
        if (targetMember.IsInitOnly is false && targetMember.IsRequired is false)
            return;

        var addedCount = mapTarget.MapToMethod.Contents.Count - contentStart;
        if (addedCount <= 0)
            return;
        if (targetMember.AccessPath.IndexOf('.') >= 0)
        {
            if (targetMember.IsInitOnly)
            {
                mapTarget.MapToMethod.Contents.RemoveRange(contentStart, addedCount);
                _context.ClassInfo.ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.InitTargetMemberCannotBeCreated,
                        sourceMember.Locations[0],
                        targetMember.AccessPath
                    )
                );
            }
            return;
        }
        var prefix = $"{MapTargetInfo.TargetParamName}.{targetMember.AccessPath} = ";
        var assignments = mapTarget
            .MapToMethod.Contents.Skip(contentStart)
            .Where(x =>
                x.StartsWith(prefix, StringComparison.Ordinal)
                && x.EndsWith(";", StringComparison.Ordinal)
            )
            .ToArray();
        if (targetMember.IsInitOnly)
            mapTarget.MapToMethod.Contents.RemoveRange(contentStart, addedCount);
        if (assignments.Length == 1)
        {
            mapTarget.CreationInitializers[targetMember.Name] = assignments[0]
                .Substring(prefix.Length)
                .TrimEnd(';');
            return;
        }

        if (targetMember.IsInitOnly)
        {
            _context.ClassInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.InitTargetMemberCannotBeCreated,
                    sourceMember.Locations[0],
                    targetMember.AccessPath
                )
            );
        }
    }

    internal void RemoveInitOnlySourceMapFrom(
        MapTargetInfo mapTarget,
        MapMemberInfo sourceMember,
        MapDirections direction,
        int contentStart
    )
    {
        if (sourceMember.IsInitOnly is false || direction.HasFlag(MapDirections.From) is false)
            return;
        var count = mapTarget.MapFromMethod.Contents.Count - contentStart;
        if (count > 0)
            mapTarget.MapFromMethod.Contents.RemoveRange(contentStart, count);
        _context.ClassInfo.ProductionContext.ReportDiagnostic(
            Diagnostic.Create(
                Descriptors.InitOnlySourceMemberCannotMapFrom,
                sourceMember.Locations[0],
                sourceMember.AccessPath
            )
        );
    }

    internal List<AttributeData>? GetMatchingAttributes(
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
            else if (_context.ClassInfo.MapTargets.All(x => x.TargetName != targetName))
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.MemberMappingTargetNotFound,
                    attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                        attribute.ApplicationSyntaxReference.Span
                    ),
                    targetName
                );
                _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
            }
        }
        if (matchingAttributes.Count <= 1)
            return matchingAttributes;

        var ambiguousDiagnostic = Diagnostic.Create(
            Descriptors.MemberMappingTargetAmbiguous,
            propertySymbol.Locations[0],
            propertySymbol.Name
        );
        _context.ClassInfo.ProductionContext.ReportDiagnostic(ambiguousDiagnostic);
        return null;
    }

    internal string GetCompositePropertyTargetName(AttributeData attribute)
    {
        var target = attribute.ConstructorArguments[0];
        return target.Value is INamedTypeSymbol targetType
            ? targetType.Name
            : target.Value as string ?? string.Empty;
    }

    internal void ReportConfigConverterOverridesAttribute(AttributeData attribute)
    {
        var syntaxReference = attribute.ApplicationSyntaxReference!;
        _context.ClassInfo.ProductionContext.ReportDiagnostic(
            Diagnostic.Create(
                Descriptors.MapConfigConverterOverridesAttribute,
                syntaxReference.SyntaxTree.GetLocation(syntaxReference.Span)
            )
        );
    }

    internal string GetMapPropertyTargetName(AttributeData attribute)
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

    internal void TryReplacePropertyAction(MapTargetInfo mapTarget, MapMemberInfo propertySymbol)
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

    internal void AddBeforePropertyAction(MapTargetInfo mapTarget, MapMemberInfo propertySymbol)
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

    internal void AddAfterPropertyAction(MapTargetInfo mapTarget, MapMemberInfo propertySymbol)
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

    internal void TryReplaceCompositePropertyAction(
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

    internal bool TryReserveTargetProperties(
        MapTargetInfo mapTarget,
        MapMemberInfo sourceProperty,
        IEnumerable<MapMemberInfo> targetProperties,
        MapDirections direction
    )
    {
        if (_context.MappedTargets.TryGetValue(mapTarget, out var mappedProperties) is false)
        {
            mappedProperties = new(StringComparer.Ordinal);
            _context.MappedTargets.Add(mapTarget, mappedProperties);
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
                _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
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

    internal void AddStartAction(MapTargetInfo mapTarget)
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

    internal void AddEndAction(MapTargetInfo mapTarget)
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
