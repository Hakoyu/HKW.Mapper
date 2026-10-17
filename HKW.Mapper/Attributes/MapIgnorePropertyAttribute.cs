namespace HKW.Mapper;

/// <summary>
/// 跳过属性映射
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MapIgnorePropertyAttribute : Attribute { }
