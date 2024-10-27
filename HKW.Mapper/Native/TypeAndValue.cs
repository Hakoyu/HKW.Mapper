// Source from https://github.com/SparkyTD/ReactiveCommand.SourceGenerator

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace HKW.HKWMapper;

/// <summary>
/// 名称类型和数据
/// </summary>
internal class TypeAndValue
{
    public TypeAndValue(TypedConstant value)
    {
        Value = value;
    }

    public TypeAndValue(ImmutableArray<TypedConstant> values)
    {
        Values = values;
    }

    /// <summary>
    /// 数据
    /// </summary>
    public TypedConstant? Value { get; }

    /// <summary>
    /// 数据
    /// </summary>
    public ImmutableArray<TypedConstant>? Values { get; }
}
