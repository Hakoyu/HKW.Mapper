using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace HKW.Mapper.SourceGenerator;

internal sealed class MapperGenerationContext
{
    internal MapperGenerationContext(
        ClassInfo classInfo,
        IReadOnlyDictionary<INamedTypeSymbol, HashSet<MapTargetInfo>> mapTargetDic
    )
    {
        ClassInfo = classInfo;
        MapTargetDic = mapTargetDic;
    }

    internal ClassInfo ClassInfo { get; }
    internal IReadOnlyDictionary<INamedTypeSymbol, HashSet<MapTargetInfo>> MapTargetDic { get; }
    internal Dictionary<
        MapTargetInfo,
        Dictionary<string, List<(MapMemberInfo Source, MapDirections Direction)>>
    > MappedTargets { get; } = [];
    internal HashSet<MapMemberInfo> ReportedIgnoreAttributeConflicts { get; } = [];
}
