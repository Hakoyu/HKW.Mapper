namespace HKW.HKWMapper;

/// <summary>
/// 配置一个源属性与多个目标属性之间的双向转换
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class MapCompositePropertyAttribute : Attribute
{
    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    /// <param name="ConverterType">复合转换器类型</param>
    /// <param name="TargetProperties">按转换器值元组元素顺序排列的目标属性名</param>
    public MapCompositePropertyAttribute(
        Type TargetType,
        Type ConverterType,
        params string[] TargetProperties
    )
        : this(TargetType.Name, ConverterType, TargetProperties)
    {
        this.TargetType = TargetType;
    }

    /// <inheritdoc/>
    /// <param name="TargetName">目标名称</param>
    /// <param name="ConverterType">复合转换器类型</param>
    /// <param name="TargetProperties">按转换器值元组元素顺序排列的目标属性名</param>
    public MapCompositePropertyAttribute(
        string TargetName,
        Type ConverterType,
        params string[] TargetProperties
    )
    {
        this.TargetName = TargetName;
        this.ConverterType = ConverterType;
        this.TargetProperties = TargetProperties;
    }

    /// <summary>
    /// 目标名称
    /// </summary>
    public string? TargetName { get; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type? TargetType { get; }

    /// <summary>
    /// 复合转换器类型
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// 按转换器值元组元素顺序排列的目标属性名
    /// </summary>
    public string[] TargetProperties { get; }
}
