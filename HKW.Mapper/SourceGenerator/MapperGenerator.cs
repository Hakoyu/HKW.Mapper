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
}
