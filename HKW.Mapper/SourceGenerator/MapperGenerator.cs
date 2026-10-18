using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace HKW.Mapper.SourceGenerator;

internal sealed class MapperGenerator
{
    public static void Generate(
        ClassInfo classInfo,
        IReadOnlyDictionary<INamedTypeSymbol, HashSet<MapTargetInfo>> mapTargetDic
    )
    {
        new MapperGenerator(classInfo, mapTargetDic).Execute();
    }

    private readonly MapperGenerationContext _context;
    private readonly MappingActionBuilder _actions;
    private readonly CreationMethodFinalizer _creationFinalizer;
    private readonly MemberMappingProcessor _memberMappings;

    private MapperGenerator(
        ClassInfo classInfo,
        IReadOnlyDictionary<INamedTypeSymbol, HashSet<MapTargetInfo>> mapTargetDic
    )
    {
        _context = new MapperGenerationContext(classInfo, mapTargetDic);
        _actions = new MappingActionBuilder(_context);
        var converters = new ConverterMappingProcessor(_context);
        var resolver = new MemberPathResolver(_context);
        var collections = new CollectionMappingProcessor(_context);
        var composites = new CompositeMappingProcessor(_context, _actions, converters, resolver);
        _creationFinalizer = new CreationMethodFinalizer(_context);
        _memberMappings = new MemberMappingProcessor(_context, _actions, collections, composites, converters, resolver);
    }

    private void Execute()
    {
        foreach (var target in _context.ClassInfo.MapTargets)
        {
            ValidateIgnoredMembers(target);
            _creationFinalizer.TryAddConfig(target);
            _actions.AddStartAction(target);
            foreach (var property in _context.ClassInfo.Properties)
                _memberMappings.ParseProperty(target, property);
            if (target.MapParentProperties)
            {
                foreach (var property in _context.ClassInfo.ParentProperties)
                    _memberMappings.ParseProperty(target, property);
            }
            if (target.MapFields)
            {
                foreach (var field in _context.ClassInfo.Fields)
                    _memberMappings.ParseProperty(target, field);
                if (target.MapParentFields)
                {
                    foreach (var field in _context.ClassInfo.ParentFields)
                        _memberMappings.ParseProperty(target, field);
                }
            }
            _actions.AddEndAction(target);
            if (target.Direction.HasFlag(MapDirections.To))
                target.MapToMethod.Contents.Add($"return {MapTargetInfo.TargetParamName};");
            if (target.Direction.HasFlag(MapDirections.From))
                target.MapFromMethod.Contents.Add($"return {MapTargetInfo.SourceParamName};");
            _creationFinalizer.FinalizeCreationMethods(target);
        }
    }

    private void ValidateIgnoredMembers(MapTargetInfo mapTarget)
    {
        var propertyNames = new HashSet<string>(
            _context.ClassInfo.Properties.Select(x => x.Name),
            StringComparer.Ordinal
        );
        if (mapTarget.MapParentProperties)
            propertyNames.UnionWith(_context.ClassInfo.ParentProperties.Select(x => x.Name));
        var reportedUnavailableMembers = new HashSet<string>(StringComparer.Ordinal);
        ReportUnavailableIgnoredMembers(
            mapTarget,
            mapTarget.AttributeIgnoreProperties,
            propertyNames,
            "property",
            mapTarget.MapTargetLocation,
            reportedUnavailableMembers
        );
        if (mapTarget.ConfigInfo is not null)
        {
            ReportUnavailableIgnoredMembers(
                mapTarget,
                mapTarget.ConfigInfo.IgnoreProperties,
                propertyNames,
                "property",
                mapTarget.ConfigInfo.IgnorePropertiesLocation,
                reportedUnavailableMembers
            );
        }

        var fieldNames = mapTarget.MapFields
            ? new HashSet<string>(
                _context.ClassInfo.Fields.Select(x => x.Name),
                StringComparer.Ordinal
            )
            : [];
        if (mapTarget.MapFields && mapTarget.MapParentFields)
            fieldNames.UnionWith(_context.ClassInfo.ParentFields.Select(x => x.Name));
        ReportUnavailableIgnoredMembers(
            mapTarget,
            mapTarget.AttributeIgnoreFields,
            fieldNames,
            "field",
            mapTarget.MapTargetLocation,
            reportedUnavailableMembers
        );
        if (mapTarget.ConfigInfo is not null)
        {
            ReportUnavailableIgnoredMembers(
                mapTarget,
                mapTarget.ConfigInfo.IgnoreFields,
                fieldNames,
                "field",
                mapTarget.ConfigInfo.IgnoreFieldsLocation,
                reportedUnavailableMembers
            );
        }
    }

    private void ReportUnavailableIgnoredMembers(
        MapTargetInfo mapTarget,
        IEnumerable<string> ignoredNames,
        HashSet<string> availableNames,
        string memberKind,
        Location location,
        HashSet<string> reportedMembers
    )
    {
        foreach (var memberName in ignoredNames.Where(x => availableNames.Contains(x) is false))
        {
            if (reportedMembers.Add($"{memberKind}:{memberName}") is false)
                continue;
            _context.ClassInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.IgnoredSourceMemberNotFound,
                    location == Location.None ? mapTarget.MapTargetLocation : location,
                    memberKind,
                    memberName,
                    mapTarget.TargetName
                )
            );
        }
    }
}
