namespace HKW.Mapper;

/// <summary>
/// 映射方法生成方向
/// </summary>
[Flags]
public enum MapDirections
{
    /// <summary>
    /// 映射至
    /// </summary>
    To = 1 << 0,

    /// <summary>
    /// 映射回源
    /// </summary>
    From = 1 << 1,

    /// <summary>
    /// 双向映射
    /// </summary>
    Both = To | From,
}
