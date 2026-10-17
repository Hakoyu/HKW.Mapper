using System;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.Mapper.SourceGenerator;

internal sealed class MemberPathResolver
{
    private readonly MapperGenerationContext _context;

    internal MemberPathResolver(MapperGenerationContext context)
    {
        _context = context;
    }

    internal MapMemberInfo? GetTargetProperty(
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
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
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
            _context.ClassInfo.ProductionContext.ReportDiagnostic(
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
            _context.ClassInfo.ProductionContext.ReportDiagnostic(
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

    internal MapMemberInfo? ResolveSourcePath(
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
                _context.ClassInfo.ProductionContext.ReportDiagnostic(
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
                    _context.ClassInfo.ProductionContext.ReportDiagnostic(
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
                    _context.ClassInfo.ProductionContext.ReportDiagnostic(
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

            currentType = ConverterMappingProcessor.UnwrapNullable(member.Type);
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
                _context.ClassInfo.ProductionContext.ReportDiagnostic(
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
                    _context.ClassInfo.ProductionContext.ReportDiagnostic(
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
                    _context.ClassInfo.ProductionContext.ReportDiagnostic(
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

            currentType = ConverterMappingProcessor.UnwrapNullable(member.Type);
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
        _context.ClassInfo.ProductionContext.ReportDiagnostic(
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
        _context.ClassInfo.ProductionContext.ReportDiagnostic(
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
        _context.ClassInfo.ProductionContext.ReportDiagnostic(
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
        _context.ClassInfo.ProductionContext.ReportDiagnostic(
            Diagnostic.Create(
                Descriptors.TargetMemberAccessorInaccessible,
                source.Locations[0],
                target.AccessPath,
                accessor
            )
        );
    }

    private bool IsMemberAccessible(ISymbol member)
    {
        if (member.DeclaredAccessibility == Accessibility.Public)
            return true;
        return member.DeclaredAccessibility >= Accessibility.Internal
            && member.ContainingAssembly.SymbolEquals(_context.ClassInfo.Compilation.Assembly);
    }

    private Location GetPropertyDiagnosticLocation(MapTargetInfo mapTarget, MapMemberInfo property)
    {
        return
            _context.ClassInfo.ParentProperties.Contains(property)
            || _context.ClassInfo.ParentFields.Contains(property)
            ? mapTarget.MapTargetLocation
            : property.Locations[0];
    }
}
