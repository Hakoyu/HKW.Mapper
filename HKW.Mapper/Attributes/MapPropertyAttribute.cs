using System.ComponentModel;

namespace HKW.Mapper;

/// <summary>
/// 配置当前属性在某个目标映射中的名称、转换器或引用类型策略
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class MapPropertyAttribute : Attribute
{
    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    public MapPropertyAttribute(Type TargetType)
        : this(TargetType.Name)
    {
        this.TargetType = TargetType;
    }

    /// <inheritdoc/>
    /// <param name="TargetName">目标名称</param>
    public MapPropertyAttribute(string TargetName)
    {
        this.TargetName = TargetName;
    }

    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    /// <param name="PropertyName">属性名称</param>
    public MapPropertyAttribute(Type TargetType, string PropertyName)
        : this(TargetType.Name, PropertyName)
    {
        this.TargetType = TargetType;
    }

    /// <inheritdoc/>
    /// <param name="TargetName">目标名称</param>
    /// <param name="PropertyName">属性名称</param>
    public MapPropertyAttribute(string TargetName, string PropertyName)
        : this(TargetName)
    {
        this.PropertyName = PropertyName;
    }

    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    /// <param name="ConverterType">转换器类型</param>
    public MapPropertyAttribute(Type TargetType, Type ConverterType)
        : this(TargetType)
    {
        this.ConverterType = ConverterType;
    }

    /// <inheritdoc/>
    /// <param name="TargetName">目标名称</param>
    /// <param name="ConverterType">转换器类型</param>
    public MapPropertyAttribute(string TargetName, Type ConverterType)
        : this(TargetName)
    {
        this.ConverterType = ConverterType;
    }

    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    /// <param name="PropertyName">属性名称</param>
    /// <param name="ConverterType">转换器类型</param>
    public MapPropertyAttribute(Type TargetType, string PropertyName, Type ConverterType)
        : this(TargetType, PropertyName)
    {
        this.ConverterType = ConverterType;
    }

    /// <inheritdoc/>
    /// <param name="TargetName">目标名称</param>
    /// <param name="PropertyName">属性名称</param>
    /// <param name="ConverterType">转换器类型</param>
    public MapPropertyAttribute(string TargetName, string PropertyName, Type ConverterType)
        : this(TargetName, PropertyName)
    {
        this.ConverterType = ConverterType;
    }

    /// <summary>
    /// 目标名称
    /// </summary>
    public string? TargetName { get; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type? TargetType { get; }

    /// <summary>
    /// 属性名称
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// 转换器类型, 目标必须实现 <see cref="IMapConverter{TSourceValue, TTargetValue}"/>
    /// </summary>
    public Type? ConverterType { get; }

    /// <summary>
    /// 从源对象根开始的源成员路径
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// 忽视属性
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// 映射类型
    /// </summary>
    public MapPropertyType MapType { get; set; }

    /// <summary>
    /// 此成员要参与的映射方向
    /// </summary>
    [DefaultValue(MapDirections.Both)]
    public MapDirections Direction { get; set; } = MapDirections.Both;

    /// <summary>
    /// 映射至目标成员前的执行条件
    /// </summary>
    [DefaultValue(MapMemberCondition.None)]
    public MapMemberCondition MapToCondition { get; set; }

    /// <summary>
    /// 从目标成员映射回源成员前的执行条件
    /// </summary>
    [DefaultValue(MapMemberCondition.None)]
    public MapMemberCondition MapFromCondition { get; set; }
}
