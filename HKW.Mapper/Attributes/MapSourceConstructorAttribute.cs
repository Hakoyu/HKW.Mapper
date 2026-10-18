namespace HKW.Mapper;

/// <summary>
/// 指定 <see cref="MapSourceAttribute"/> 创建目标实例时所使用的目标构造函数
/// </summary>
/// <remarks>
/// 特性必须标记在声明对应 <see cref="MapSourceAttribute"/> 的顶级、非泛型
/// <see langword="static"/> <see langword="partial"/> 类上。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MapSourceConstructorAttribute : Attribute
{
    /// <summary>
    /// 为源类型到目标类型的映射指定构造函数
    /// </summary>
    /// <param name="sourceType">由 <see cref="MapSourceAttribute"/> 声明的源类型</param>
    /// <param name="targetType">由 <see cref="MapSourceAttribute"/> 声明的目标类型</param>
    /// <param name="parameterTypes">构造函数参数的精确类型</param>
    public MapSourceConstructorAttribute(
        Type sourceType,
        Type targetType,
        params Type[] parameterTypes
    )
    {
        SourceType = sourceType;
        TargetType = targetType;
        ParameterTypes = parameterTypes;
    }

    /// <summary>
    /// 为源类型及目标名称对应的映射指定构造函数
    /// </summary>
    /// <param name="sourceType">由 <see cref="MapSourceAttribute"/> 声明的源类型</param>
    /// <param name="targetName"><see cref="MapSourceAttribute.TargetName"/> 声明的目标名称</param>
    /// <param name="parameterTypes">构造函数参数的精确类型</param>
    public MapSourceConstructorAttribute(
        Type sourceType,
        string targetName,
        params Type[] parameterTypes
    )
    {
        SourceType = sourceType;
        TargetName = targetName;
        ParameterTypes = parameterTypes;
    }

    /// <summary>
    /// 获取指定的源类型
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// 获取指定的目标类型
    /// </summary>
    public Type? TargetType { get; }

    /// <summary>
    /// 获取指定的目标名称
    /// </summary>
    public string? TargetName { get; }

    /// <summary>
    /// 获取构造函数参数的精确类型
    /// </summary>
    public Type[] ParameterTypes { get; }
}
