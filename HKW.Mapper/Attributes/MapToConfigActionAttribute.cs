namespace HKW.Mapper;

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
