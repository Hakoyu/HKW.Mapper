namespace HKW.HKWMapper;

/// <summary>
/// 方法调用状态
/// </summary>
[Flags]
public enum MapMethodInvokeModes
{
    /// <summary>
    /// 同步
    /// </summary>
    Sync = 1 << 0,

    /// <summary>
    /// 异步
    /// </summary>
    Async = 1 << 1,

    /// <summary>
    /// 同步和异步
    /// </summary>
    Both = Sync | Async,
}
