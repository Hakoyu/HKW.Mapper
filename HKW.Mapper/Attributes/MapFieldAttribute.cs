using System.ComponentModel;

namespace HKW.Mapper;

/// <summary>
/// 配置当前字段在某个目标映射中的名称、转换器或引用类型策略
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class MapFieldAttribute : Attribute
{
    /// <inheritdoc/>
    public MapFieldAttribute(Type TargetType)
        : this(TargetType.Name) => this.TargetType = TargetType;

    /// <inheritdoc/>
    public MapFieldAttribute(string TargetName) => this.TargetName = TargetName;

    /// <inheritdoc/>
    public MapFieldAttribute(Type TargetType, string FieldName)
        : this(TargetType.Name, FieldName) => this.TargetType = TargetType;

    /// <inheritdoc/>
    public MapFieldAttribute(string TargetName, string FieldName)
        : this(TargetName) => this.FieldName = FieldName;

    /// <inheritdoc/>
    public MapFieldAttribute(Type TargetType, Type ConverterType)
        : this(TargetType) => this.ConverterType = ConverterType;

    /// <inheritdoc/>
    public MapFieldAttribute(string TargetName, Type ConverterType)
        : this(TargetName) => this.ConverterType = ConverterType;

    /// <inheritdoc/>
    public MapFieldAttribute(Type TargetType, string FieldName, Type ConverterType)
        : this(TargetType, FieldName) => this.ConverterType = ConverterType;

    /// <inheritdoc/>
    public MapFieldAttribute(string TargetName, string FieldName, Type ConverterType)
        : this(TargetName, FieldName) => this.ConverterType = ConverterType;

    /// <summary>
    /// 目标名称
    /// </summary>
    public string? TargetName { get; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type? TargetType { get; }

    /// <summary>
    /// 目标字段或属性名称
    /// </summary>
    public string? FieldName { get; }

    /// <summary>
    /// 实现 <see cref="IMapConverter{TSourceValue, TTargetValue}"/> 的转换器类型
    /// </summary>
    public Type? ConverterType { get; }

    /// <summary>
    /// 从源对象根开始的源成员路径
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// 是否忽略当前字段
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// 字段映射类型
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
