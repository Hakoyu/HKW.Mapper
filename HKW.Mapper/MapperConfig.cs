using System.Collections.Frozen;
using System.Diagnostics;
using System.Linq.Expressions;

namespace HKW.HKWMapper;

/// <summary>
/// 映射配置
/// </summary>
/// <typeparam name="TSource">源类型</typeparam>
/// <typeparam name="TTarget">目标类型</typeparam>
public abstract class MapperConfig<TSource, TTarget>
{
    /// <inheritdoc/>
    /// <param name="source">源</param>
    /// <param name="target">目标</param>
    protected MapperConfig(TSource source, TTarget target)
    {
        Source = source;
        Target = target;
    }

    /// <summary>
    /// 源对象
    /// </summary>
    protected TSource Source { get; }

    /// <summary>
    /// 目标对象
    /// </summary>
    protected TTarget Target { get; }
}
