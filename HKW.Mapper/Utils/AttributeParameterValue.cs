using Microsoft.CodeAnalysis;

namespace HKW.SourceGeneratorUtils;

/// <summary>
/// 特性参数值
/// </summary>
internal class AttributeParameterValue
{
    public AttributeParameterValue(TypedConstant typedConstant)
    {
        if (typedConstant.Kind is TypedConstantKind.Array)
        {
            Values = typedConstant.Values.Select(x => x.Value).ToArray();
        }
        else
        {
            Value = typedConstant.Value;
        }
    }

    public object? Value { get; set; } = null;
    public object?[]? Values { get; set; } = null;
}
