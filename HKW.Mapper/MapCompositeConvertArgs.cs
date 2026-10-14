namespace HKW.Mapper;

/// <summary>
/// 复合转换参数
/// </summary>
public readonly struct MapCompositeConvertArgs
{
    /// <summary>
    /// 初始化复合转换参数
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="target">目标对象</param>
    /// <param name="sourcePropertyName">源属性名</param>
    /// <param name="targetPropertyNames">目标属性名</param>
    public MapCompositeConvertArgs(
        object source,
        object target,
        string sourcePropertyName,
        string[] targetPropertyNames
    )
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        SourcePropertyName = sourcePropertyName
            ?? throw new ArgumentNullException(nameof(sourcePropertyName));
        TargetPropertyNames = targetPropertyNames
            ?? throw new ArgumentNullException(nameof(targetPropertyNames));
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
    public string[] TargetPropertyNames { get; }
}
