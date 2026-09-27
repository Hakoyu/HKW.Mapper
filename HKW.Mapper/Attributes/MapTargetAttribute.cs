using System.ComponentModel;

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
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
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
    /// 映射设置, 目标必须继承至 <see cref="MapperConfig{TSource, TTarget}"/>
    /// </summary>
    public Type? Config { get; }

    /// <summary>
    /// 要生成的映射方向
    /// </summary>
    [DefaultValue(MapDirections.Both)]
    public MapDirections Direction { get; set; } = MapDirections.Both;
}

#pragma warning disable S2346
/// <summary>
/// 映射方法生成方向
/// </summary>
[Flags]
public enum MapDirections
{
    /// <summary>
    /// 映射至
    /// </summary>
    To = 0 << 1,

    /// <summary>
    /// 映射回源
    /// </summary>
    From = 0 << 2,

    /// <summary>
    /// 双向映射
    /// </summary>
    Both = To | From,
}
#pragma warning restore S2346
