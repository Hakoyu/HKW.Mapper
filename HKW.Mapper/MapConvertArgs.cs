namespace HKW.Mapper;

/// <summary>
/// 转换参数
/// </summary>
public readonly struct MapConvertArgs
{
    /// <summary>
    /// 初始化转换参数
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="target">目标对象</param>
    /// <param name="sourcePropertyName">源属性名</param>
    /// <param name="targetPropertyName">目标属性名</param>
    public MapConvertArgs(
        object source,
        object target,
        string sourcePropertyName,
        string targetPropertyName
    )
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        SourcePropertyName = sourcePropertyName
            ?? throw new ArgumentNullException(nameof(sourcePropertyName));
        TargetPropertyName = targetPropertyName
            ?? throw new ArgumentNullException(nameof(targetPropertyName));
    }

    /// <summary>
    /// 源对象
    /// </summary>
    public object Source { get; }

    /// <summary>
    /// 目标对象
    /// </summary>
    public object Target { get; }

    /// <summary>
    /// 源属性名
    /// </summary>
    public string SourcePropertyName { get; }

    /// <summary>
    /// 目标属性名
    /// </summary>
    public string TargetPropertyName { get; }
}
