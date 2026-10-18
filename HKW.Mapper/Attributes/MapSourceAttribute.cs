using System.ComponentModel;

namespace HKW.Mapper;

/// <summary>
/// 从外部扩展类声明源类型到目标类型的双向映射
/// <para>特性必须标记在顶级、非泛型的 <see langword="static"/> <see langword="partial"/> 类上，并在此类中生成双向映射扩展方法</para>
/// <para><see langword="TargetName"/> 默认为 <see langword="TargetType.Name"/>
/// <code><![CDATA[
/// MapTo(this Source, Target)
/// MapFrom(this Source, Target)
/// ]]></code>
/// 当 <see langword="TargetName"/> 设置为不等于 <see langword="TargetType.Name"/> 的自定义名称时：
/// <code><![CDATA[
/// MapTo{TargetName}(this Source, Target)
/// MapFrom{TargetName}(this Source, Target)
/// ]]></code>
/// </para>
/// <example>
/// <code><![CDATA[
/// [MapSource(typeof(Source), typeof(Target))]
/// public static partial class MapperExtensions;
/// ]]></code>
/// </example>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MapSourceAttribute : Attribute
{
    /// <inheritdoc/>
    /// <param name="SourceType">源类型</param>
    /// <param name="TargetType">目标类型</param>
    public MapSourceAttribute(Type SourceType, Type TargetType)
    {
        this.SourceType = SourceType;
        this.TargetType = TargetType;
    }

    /// <inheritdoc/>
    /// <param name="SourceType">源类型</param>
    /// <param name="TargetType">目标类型</param>
    /// <param name="Config">映射设置</param>
    public MapSourceAttribute(Type SourceType, Type TargetType, Type Config)
    {
        this.SourceType = SourceType;
        this.TargetType = TargetType;
        this.Config = Config;
    }

    /// <summary>
    /// 源类型
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// 目标名称, 默认为 <c>{TargetType.Name}</c>
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// 映射设置, 目标必须继承至 <see cref="MapperConfig{TSource, TTarget}"/>
    /// </summary>
    public Type? Config { get; }

    /// <summary>
    /// 要生成的映射方向
    /// </summary>
    [DefaultValue(MapDirections.Both)]
    public MapDirections Direction { get; set; } = MapDirections.Both;

    /// <summary>
    /// 是否映射源类型的字段
    /// </summary>
    [DefaultValue(false)]
    public bool MapFields { get; set; }

    /// <summary>
    /// 是否映射源类型的父类属性
    /// </summary>
    [DefaultValue(false)]
    public bool MapParentProperties { get; set; }

    /// <summary>
    /// 是否映射源类型的父类字段
    /// </summary>
    [DefaultValue(false)]
    public bool MapParentFields { get; set; }

    /// <summary>
    /// 要忽略的源属性名称
    /// </summary>
    public string[] IgnoreProperties { get; set; } = [];

    /// <summary>
    /// 要忽略的源字段名称
    /// </summary>
    public string[] IgnoreFields { get; set; } = [];
}
