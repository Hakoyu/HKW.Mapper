using System.ComponentModel;

namespace HKW.Mapper;

/// <summary>
/// 配置一个源字段与多个目标成员之间的双向转换
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class MapCompositeFieldAttribute : Attribute
{
    /// <inheritdoc/>
    public MapCompositeFieldAttribute(
        Type TargetType,
        Type ConverterType,
        params string[] TargetFields
    )
        : this(TargetType.Name, ConverterType, TargetFields) => this.TargetType = TargetType;

    /// <inheritdoc/>
    public MapCompositeFieldAttribute(
        string TargetName,
        Type ConverterType,
        params string[] TargetFields
    )
    {
        this.TargetName = TargetName;
        this.ConverterType = ConverterType;
        this.TargetFields = TargetFields;
    }

    /// <summary>目标名称</summary>
    public string? TargetName { get; }

    /// <summary>目标类型</summary>
    public Type? TargetType { get; }

    /// <summary>实现 <see cref="IMapCompositeConverter{TSourceValue, TTargetValues}"/> 的转换器类型</summary>
    public Type ConverterType { get; }

    /// <summary>从源对象根开始的源成员路径</summary>
    public string? SourcePath { get; set; }

    /// <summary>按转换器值元组顺序排列的目标字段或属性名称</summary>
    public string[] TargetFields { get; }

    /// <summary>此成员要参与的映射方向</summary>
    [DefaultValue(MapDirections.Both)]
    public MapDirections Direction { get; set; } = MapDirections.Both;
}
