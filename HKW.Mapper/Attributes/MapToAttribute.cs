﻿namespace HKW.HKWMapper;

/// <summary>
/// 制图到目标类型
/// <para>会在当前类型生成一个扩展方法用于映射到目标类型</para>
/// <para>自动生成方法 <c>MapTo{<see cref="TargetName"/>}</c> </para>
/// <para>自动生成特性 <c>{SourceClassName}MapTo{<see cref="TargetName"/>}PropertyAttribute</c>, 可用其对属性进行更详细的设置</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MapToAttribute : Attribute
{
    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    public MapToAttribute(Type TargetType)
    {
        this.TargetType = TargetType;
    }

    /// <inheritdoc/>
    /// <param name="TargetType">目标类型</param>
    /// <param name="MapperConfig">映射设置</param>
    public MapToAttribute(Type TargetType, Type MapperConfig)
    {
        this.TargetType = TargetType;
        this.MapperConfig = MapperConfig;
    }

    /// <summary>
    /// 目标类型
    /// </summary>
    public Type TargetType { get; set; }

    /// <summary>
    /// 目标名称, 默认为 <c>{TargetType.Name}</c>
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// 严格模式
    /// </summary>
    public bool ScrutinyMode { get; set; }

    /// <summary>
    /// 映射设置
    /// </summary>
    public Type? MapperConfig { get; set; }

    /// <summary>
    /// 方法调用状态
    /// <para>会根据设置来生成对应的方法</para>
    /// </summary>
    public MapMethodInvokeState InvokeState { get; set; }
}
