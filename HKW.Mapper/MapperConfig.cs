using System.Collections.Frozen;
using System.Diagnostics;
using System.Linq.Expressions;

namespace HKW.HKWMapper;

/// <summary>
/// 映射器设置接口
/// </summary>
public interface IMapperConfig
{
    /// <summary>
    /// 冻结
    /// </summary>
    void Frozen();
}

/// <summary>
/// 映射设置
/// </summary>
/// <typeparam name="TSource">源类型</typeparam>
/// <typeparam name="TTarget">目标类型</typeparam>
[DebuggerDisplay("Source = {TSource}, Target = {TTarget}")]
public abstract class MapperConfig<TSource, TTarget> : IMapperConfig
{
    private Dictionary<string, Action<TSource, TTarget>> _propertyActions = [];
    private Dictionary<string, Func<TSource, TTarget, Task>> _propertyActionAsyncs = [];
    private FrozenDictionary<string, Action<TSource, TTarget>> _frozenPropertyActions = null!;
    private FrozenDictionary<string, Func<TSource, TTarget, Task>> _frozenPropertyActionAsyncs =
        null!;

    /// <summary>
    /// 添加映射
    /// <para><c>x=>x.Property,(s,t)=>{}</c></para>
    /// <para><c>MapTo: t.Value = s.Value</c></para>
    /// <para><c>MapFrom: s.Value = t.Value</c></para>
    /// </summary>
    /// <param name="expression">映射属性表达式</param>
    /// <param name="action">映射行动</param>
    /// <exception cref="ArgumentException">映射格式错误</exception>
    protected internal void AddMap(
        Expression<Func<TSource, object>> expression,
        Action<TSource, TTarget> action
    )
    {
        _propertyActions.Add(GetName(expression), action);
    }

    /// <summary>
    /// 添加映射
    /// <para><c>x=>x.Property,(s,t)=>{}</c></para>
    /// <para><c>MapTo: t.Value = s.Value</c></para>
    /// <para><c>MapFrom: s.Value = t.Value</c></para>
    /// </summary>
    /// <param name="expression">映射属性表达式</param>
    /// <param name="action">映射行动</param>
    /// <exception cref="ArgumentException">映射格式错误</exception>
    protected internal void AddMapAsync(
        Expression<Func<TSource, object>> expression,
        Func<TSource, TTarget, Task> action
    )
    {
        _propertyActionAsyncs.Add(GetName(expression), action);
    }

    private static string GetName(Expression<Func<TSource, object>> expression)
    {
        if (expression.Body is MemberExpression member)
        {
            return member.Member.Name;
        }
        else if (
            expression.Body is UnaryExpression unary
            && unary.Operand is MemberExpression unaryMember
        )
        {
            return unaryMember.Member.Name;
        }
        else
            throw new ArgumentException("Expression error", nameof(expression));
    }

    /// <summary>
    /// 获取映射行动
    /// </summary>
    /// <param name="propertyName">名称</param>
    /// <param name="source">源</param>
    /// <param name="target">目标</param>
    /// <returns>映射行动</returns>
    public void GetMapAction(string propertyName, TSource source, TTarget target)
    {
        _frozenPropertyActions[propertyName](source, target);
    }

    /// <summary>
    /// 获取映射行动
    /// </summary>
    /// <param name="propertyName">名称</param>
    /// <param name="source">源</param>
    /// <param name="target">目标</param>
    /// <returns>映射行动</returns>
    public Task GetMapActionAsync(string propertyName, TSource source, TTarget target)
    {
        return _frozenPropertyActionAsyncs[propertyName](source, target);
    }

    /// <summary>
    /// 将已添加的映射冻结,以提高性能
    /// </summary>
    /// <returns></returns>
    public void Frozen()
    {
        _frozenPropertyActions = FrozenDictionary.ToFrozenDictionary(
            _propertyActions,
            x => string.Intern(x.Key),
            x => x.Value
        );
        _frozenPropertyActionAsyncs = FrozenDictionary.ToFrozenDictionary(
            _propertyActionAsyncs,
            x => string.Intern(x.Key),
            x => x.Value
        );
        _propertyActions.Clear();
        _propertyActions = null!;
        _propertyActionAsyncs.Clear();
        _propertyActionAsyncs = null!;
    }

    /// <summary>
    /// 开始映射行动
    /// </summary>
    public virtual void BeginMapAction(TSource source, TTarget target) { }

    /// <summary>
    /// 结束映射行动
    /// </summary>
    public virtual void EndMapAction(TSource source, TTarget target) { }

    /// <summary>
    /// 开始映射行动
    /// </summary>
    public virtual Task BeginMapActionAsync(TSource source, TTarget target)
    {
        return null!;
    }

    /// <summary>
    /// 结束映射行动
    /// </summary>
    public virtual Task EndMapActionAsync(TSource source, TTarget target)
    {
        return null!;
    }
}
