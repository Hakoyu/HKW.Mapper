using System;

namespace HKW.Mapper.SourceGenerator;

/// <summary>
/// 生成器内部镜像, 避免分析器程序集在编译期加载运行时 DLL
/// </summary>
internal enum MapConfigActionMode
{
    Start,
    End,
    BeforeProperty,
    AfterProperty,
    ReplaceProperty,
}

/// <summary>
/// 生成器内部镜像, 避免分析器程序集在编译期加载运行时 DLL
/// </summary>
[Flags]
internal enum MapDirections
{
    To = 1 << 0,
    From = 1 << 1,
    Both = To | From,
}

/// <summary>
/// 生成器内部镜像, 避免分析器程序集在编译期加载运行时 DLL
/// </summary>
internal enum MapMemberCondition
{
    None,
    SourceIsDefault,
    SourceIsNotDefault,
    TargetIsDefault,
    TargetIsNotDefault,
}

/// <summary>
/// 生成器内部镜像, 避免分析器程序集在编译期加载运行时 DLL
/// </summary>
internal enum MapPropertyType
{
    None,
    Reference,
    ConstructFromSelf,
}
