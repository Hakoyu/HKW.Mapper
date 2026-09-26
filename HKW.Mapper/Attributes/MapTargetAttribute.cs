namespace HKW.HKWMapper;

/// <summary>
/// 从目标类型做双向映射
/// <para>自动为当前类型生成双向映射扩展方法</para>
/// <para><see langword="TargetName"/> 默认为 <see langword="TargetType.Name"/>
/// <code><![CDATA[
/// MapTo{TargetName}(Target)
/// MapFrom{TargetName}(Target)
/// ]]></code>
/// </para>
/// <para>自动为生成映射特性, 可对映射属性进行设置
/// <code><![CDATA[
/// {SourceName}MapFrom{TargetName}PropertyAttribute
/// ]]></code>
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MapTargetAttribute : Attribute
{
    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    public MapTargetAttribute(Type TargetType)
    {
        this.TargetType = TargetType;
    }

    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    /// <param name="Config">映射设置 </param>
    public MapTargetAttribute(Type TargetType, Type Config)
    {
        this.TargetType = TargetType;
        this.Config = Config;
    }

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// 目标名称, 默认为 <c>{TargetType.Name}</c>
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// 映射设置, 基于 <see cref="MapperConfig{TSource, TTarget}"/>
    /// </summary>
    public Type? Config { get; }

    /// <summary>
    /// 方法调用状态
    /// <para>会根据设置来生成对应的方法</para>
    /// </summary>
    public MapMethodInvokeModes InvokeMode { get; set; }
}
