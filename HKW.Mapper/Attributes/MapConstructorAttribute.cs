namespace HKW.Mapper;

/// <summary>
/// 指定 <see cref="MapTargetAttribute"/> 创建目标实例时所使用的目标构造函数
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = true,
    Inherited = false
)]
public sealed class MapConstructorAttribute : Attribute
{
    /// <summary>
    /// 为目标类型指定构造函数
    /// </summary>
    /// <param name="targetType">由 <see cref="MapTargetAttribute"/> 声明的目标类型</param>
    /// <param name="parameterTypes">构造函数参数的精确类型</param>
    public MapConstructorAttribute(Type targetType, params Type[] parameterTypes)
    {
        TargetType = targetType;
        ParameterTypes = parameterTypes;
    }

    /// <summary>
    /// 为 <see cref="MapTargetAttribute.TargetName"/> 声明的目标名称指定构造函数
    /// </summary>
    /// <param name="targetName">目标名称</param>
    /// <param name="parameterTypes">构造函数参数的精确类型</param>
    public MapConstructorAttribute(string targetName, params Type[] parameterTypes)
    {
        TargetName = targetName;
        ParameterTypes = parameterTypes;
    }

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
