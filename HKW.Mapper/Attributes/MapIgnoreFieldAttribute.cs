namespace HKW.Mapper;

/// <summary>
/// 跳过字段映射。
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class MapIgnoreFieldAttribute : Attribute { }
