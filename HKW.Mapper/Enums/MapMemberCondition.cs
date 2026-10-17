namespace HKW.Mapper;

/// <summary>
/// 成员映射执行条件
/// </summary>
public enum MapMemberCondition
{
    /// <summary>
    /// 始终执行映射
    /// </summary>
    None,

    /// <summary>
    /// 原始源成员为 <see langword="null"/>（引用类型）或 <see langword="default"/>（值类型）时执行映射
    /// </summary>
    SourceIsDefault,

    /// <summary>
    /// 原始源成员不为 <see langword="null"/>（引用类型）或不为 <see langword="default"/>（值类型）时执行映射
    /// </summary>
    SourceIsNotDefault,

    /// <summary>
    /// 原始目标成员为 <see langword="null"/>（引用类型）或 <see langword="default"/>（值类型）时执行映射
    /// </summary>
    TargetIsDefault,

    /// <summary>
    /// 原始目标成员不为 <see langword="null"/>（引用类型）或不为 <see langword="default"/>（值类型）时执行映射
    /// </summary>
    TargetIsNotDefault,
}
