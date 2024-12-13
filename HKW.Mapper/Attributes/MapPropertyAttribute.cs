namespace HKW.HKWMapper;

/// <summary>
/// MapPropertyAttribute
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class MapPropertyAttribute : Attribute
{
    /// <inheritdoc/>
    public MapPropertyAttribute(string PropertyName)
    {
        this.PropertyName = PropertyName;
    }

    /// <inheritdoc/>
    public MapPropertyAttribute(Type ConverterType)
    {
        this.ConverterType = ConverterType;
    }

    /// <summary>
    /// 目标属性名称
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// 转换器类型
    /// </summary>
    public Type? ConverterType { get; set; }

    /// <summary>
    /// 当右值不为 <see langword="NullOrDefault"/> 时才进行映射
    /// <para>
    /// 右值为值类型时进行非 <see langword="default"/> 检查, 右值为引用类型时进行非 <see langword="null"/> 检查
    /// </para>
    /// <para>
    /// <c>if(source.Value is not default) target.Value = source.Value</c>
    /// <para>
    /// <c>if(target.Value is not null) source.Value = target.Value</c>
    /// </para>
    /// </para>
    /// </summary>
    public bool MapWhenRValueNotNullOrDefault { get; set; }

    /// <summary>
    /// 当左值为 <see langword="NullOrDefault"/> 时才进行映射
    /// <para>
    /// 左值为值类型时进行 <see langword="default"/> 检查, 左值为引用类型时进行 <see langword="null"/> 检查
    /// </para>
    /// <para>
    /// <c>if(target.Value is default) target.Value = source.Value</c>
    /// </para>
    /// <para>
    /// <c>if(source.Value is null) source.Value = target.Value</c>
    /// </para>
    /// </summary>
    public bool MapWhenLValueNullOrDefault { get; set; }
}
