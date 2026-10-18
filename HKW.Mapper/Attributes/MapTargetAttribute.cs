using System.ComponentModel;

namespace HKW.Mapper;

/// <summary>
/// 从目标类型做双向映射
/// <para>自动为当前类型生成双向映射扩展方法</para>
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
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class MapTargetAttribute : Attribute
{
    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    public MapTargetAttribute(Type TargetType)
    {
        this.TargetType = TargetType;
        this.Direction = Direction;
    }

    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    /// <param name="Config">映射设置 </param>
    public MapTargetAttribute(Type TargetType, Type Config)
    {
        this.TargetType = TargetType;
        this.Config = Config;
        this.Direction = Direction;
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
    /// 映射设置, 目标必须继承至 <see cref="MapperConfigBase{TSource, TTarget}"/>
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
