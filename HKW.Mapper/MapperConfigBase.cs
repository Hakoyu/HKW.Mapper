using System.Collections.Frozen;
using System.Diagnostics;
using System.Linq.Expressions;

namespace HKW.Mapper;

/// <summary>
/// 映射配置
/// <para>
/// 支持类型为 <see langword="string[]"/> 名称为 <see langword="IgnoreProperties"/> / <see langword="IgnoreFields"/> 并初始化集合的忽略成员属性
/// </para>
/// <para>
/// </para>
/// 支持类型为 <see cref="MapConfigMemberConverter{TSourceValue,TTargetValue}"/> 的成员转换器属性
/// <para>
/// </para>
/// 支持类型为 <see cref="MapConfigCompositeMemberConverter{TSourceValue, TTargetValues}"/> 的组合成员转换器属性
/// <para>
/// 支持标记 <see cref="MapToConfigActionAttribute"/> 或 <see cref="MapFromConfigActionAttribute"/> 的映射行动方法
/// </para>
/// </summary>
/// <typeparam name="TSource">源类型</typeparam>
/// <typeparam name="TTarget">目标类型</typeparam>
public abstract class MapperConfigBase<TSource, TTarget>
{
    /// <inheritdoc/>
    /// <param name="source">源</param>
    /// <param name="target">目标</param>
    protected MapperConfigBase(TSource source, TTarget target)
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
