namespace HKW.HKWMapper;

/// <summary>
/// 从目标类型制图
/// <para>会在当前类型生成一个扩展方法用于将目标类型映射当前类型</para>
/// <para>自动生成方法 <c>MapFrom{TargetClassName}</c> </para>
/// <para>自动生成特性 <c>{SourceClassName}{MethodName}PropertyAttribute</c>, 可用其对属性进行更详细的设置</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MapFromAttribute : Attribute
{
    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    /// <param name="MethodName">方法名称, 默认名称为 <c>MapFrom{TargetClassName}</c></param>
    public MapFromAttribute(Type TargetType, string? MethodName = null)
    {
        this.TargetType = TargetType;
        this.MethodName = MethodName;
    }

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type TargetType { get; set; }

    /// <summary>
    /// 方法名称, 默认名称为 <c>MapFrom{TargetClassName}</c>
    /// </summary>
    public string? MethodName { get; set; }
}
