namespace HKW.Mapper;

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
