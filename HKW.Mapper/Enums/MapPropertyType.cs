namespace HKW.Mapper;

/// <summary>
/// 属性映射类型
/// </summary>
public enum MapPropertyType
{
    /// <summary>
    /// 默认操作, 会自动使用 ICloneable ,会警告映射引用类型
    /// </summary>
    None,

    /// <summary>
    /// 强制映射引用类型
    /// </summary>
    Reference,

    /// <summary>
    /// 使用自身构造
    /// <para>例如: <c>source.List = new (target.List)</c></para>
    /// </summary>
    ConstructFromSelf,
}
