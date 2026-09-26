namespace HKW.HKWMapper;

/// <summary>
/// 映射至设置行动
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MapToConfigActionAttribute : Attribute
{
    /// <inheritdoc/>
    /// <param name="Mode">模式</param>
    /// <param name="PropertyName">属性名</param>
    public MapToConfigActionAttribute(MapConfigActionMode Mode, string PropertyName = "")
    {
        this.Mode = Mode;
        this.PropertyName = PropertyName;
    }

    /// <summary>
    /// 行动模式
    /// </summary>
    public MapConfigActionMode Mode { get; }

    /// <summary>
    /// 属性名称
    /// <para>仅适用于: <see cref="MapConfigActionMode.BeforeProperty"/>, <see cref="MapConfigActionMode.AfterProperty"/>, <see cref="MapConfigActionMode.ReplaceProperty"/></para>
    /// </summary>
    public string PropertyName { get; }
}

/// <summary>
/// 映射回源设置行动
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MapFromConfigActionAttribute : Attribute
{
    /// <inheritdoc/>
    /// <param name="Mode">模式</param>
    /// <param name="PropertyName">属性名</param>
    public MapFromConfigActionAttribute(MapConfigActionMode Mode, string PropertyName = "")
    {
        this.Mode = Mode;
        this.PropertyName = PropertyName;
    }

    /// <summary>
    /// 行动模式
    /// </summary>
    public MapConfigActionMode Mode { get; }

    /// <summary>
    /// 属性名称
    /// <para>仅适用于: <see cref="MapConfigActionMode.BeforeProperty"/>, <see cref="MapConfigActionMode.AfterProperty"/>, <see cref="MapConfigActionMode.ReplaceProperty"/></para>
    /// </summary>
    public string PropertyName { get; }
}

/// <summary>
/// 映射设置行动模式
/// </summary>
public enum MapConfigActionMode
{
    /// <summary>
    /// 当映射开始时
    /// </summary>
    Start,

    /// <summary>
    /// 当映射结束时
    /// </summary>
    End,

    /// <summary>
    /// 在属性映射之前
    /// </summary>
    BeforeProperty,

    /// <summary>
    /// 在属性映射之后
    /// </summary>
    AfterProperty,

    /// <summary>
    /// 替换属性映射
    /// </summary>
    ReplaceProperty,
}
