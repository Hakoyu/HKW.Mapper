using System.Collections.Generic;
using HKW.SourceGeneratorUtils;

namespace HKW.Mapper.SourceGenerator;

internal static class MemberConditionBuilder
{
    internal static MapMemberCondition GetCondition(
        AttributeInfo? attributeInfo,
        string parameterName
    ) =>
        attributeInfo?.TryGetParam<MapMemberCondition>(parameterName, out var condition) is true
            ? condition
            : MapMemberCondition.None;

    internal static void WrapMapping(
        List<string> contents,
        int contentStart,
        MapMemberCondition condition,
        MapMemberInfo sourceMember,
        IReadOnlyList<MapMemberInfo> targetMembers
    )
    {
        if (condition is MapMemberCondition.None || contents.Count == contentStart)
            return;

        var expression = BuildExpression(condition, sourceMember, targetMembers);
        contents.Insert(contentStart, $"if ({expression})");
        contents.Insert(contentStart + 1, "{");
        contents.Add("}");
    }

    private static string BuildExpression(
        MapMemberCondition condition,
        MapMemberInfo sourceMember,
        IReadOnlyList<MapMemberInfo> targetMembers
    )
    {
        var useSource =
            condition
            is MapMemberCondition.SourceIsDefault
                or MapMemberCondition.SourceIsNotDefault;
        var negate =
            condition
            is MapMemberCondition.SourceIsNotDefault
                or MapMemberCondition.TargetIsNotDefault;
        var expressions = useSource
            ? new[] { BuildIsNullOrDefault(MapTargetInfo.SourceParamName, sourceMember) }
            : targetMembers
                .Select(x => BuildIsNullOrDefault(MapTargetInfo.TargetParamName, x))
                .ToArray();
        var allNullOrDefault = string.Join(" && ", expressions.Select(x => $"({x})"));
        return negate ? $"!({allNullOrDefault})" : allNullOrDefault;
    }

    private static string BuildIsNullOrDefault(string parameterName, MapMemberInfo member)
    {
        var expression = $"{parameterName}.{member.AccessPath}";
        if (member.Type.IsReferenceType)
            return $"{expression} is null";

        return $"global::System.Collections.Generic.EqualityComparer<{member.Type.GetGlobalFullName()}>.Default.Equals({expression}, default)";
    }
}
