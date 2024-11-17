using System.Collections.Frozen;
using System.Linq.Expressions;

namespace HKW.HKWMapper;

/// <summary>
/// 映射设置
/// </summary>
/// <typeparam name="TSource">源类型</typeparam>
/// <typeparam name="TTarget">目标类型</typeparam>
public abstract class MapperConfig<TSource, TTarget>
{
    private Dictionary<string, Action<TSource, TTarget>> _propertyActions = [];
    private FrozenDictionary<string, Action<TSource, TTarget>> _frozenPropertyActions = null!;

    /// <summary>
    /// 添加映射
    /// <para><c>x=>x.Property,(s,t)=>{}</c></para>
    /// </summary>
    /// <param name="expression">映射属性表达式</param>
    /// <param name="action">映射行动</param>
    /// <exception cref="ArgumentException">映射格式错误</exception>
    protected void AddMap(
        Expression<Func<TSource, object>> expression,
        Action<TSource, TTarget> action
    )
    {
        if (expression.Body is MemberExpression member)
        {
            _propertyActions.Add(member.Member.Name, action);
        }
        else if (expression.Body is UnaryExpression unary)
        {
            if (unary.Operand is MemberExpression unaryMember)
                _propertyActions.Add(unaryMember.Member.Name, action);
        }
        else
            throw new ArgumentException("Expression error", nameof(expression));
    }

    /// <summary>
    /// 获取映射行动
    /// </summary>
    /// <param name="propertyName">名称</param>
    /// <returns>映射行动</returns>
    public Action<TSource, TTarget> GetMapAction(string propertyName)
    {
        return _frozenPropertyActions[propertyName];
    }

    /// <summary>
    /// 将已添加的映射冻结,以提高性能
    /// </summary>
    /// <returns></returns>
    public MapperConfig<TSource, TTarget> Frozen()
    {
        _frozenPropertyActions = FrozenDictionary.ToFrozenDictionary(_propertyActions);
        _propertyActions.Clear();
        _propertyActions = null!;
        return this;
    }

    /// <summary>
    /// 开始映射行动
    /// </summary>
    public virtual void BeginMapAction(TSource source, TTarget target) { }

    /// <summary>
    /// 结束映射行动
    /// </summary>
    public virtual void EndMapAction(TSource source, TTarget target) { }
}
