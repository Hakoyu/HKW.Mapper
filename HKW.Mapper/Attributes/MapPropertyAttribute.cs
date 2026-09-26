namespace HKW.HKWMapper;

/// <summary>
/// Map property attribute
/// <para>Source: <see cref="Object"/></para>
/// <para>Target: <see cref="Object"/></para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal sealed class MapPropertyAttribute : Attribute
{
    /// <inheritdoc/>
    public MapPropertyAttribute(string PropertyName)
    {
        this.PropertyName = PropertyName;
    }

    /// <inheritdoc/>
    public MapPropertyAttribute(Type ConverterType)
    {
        this.ConverterType = ConverterType;
    }

    /// <inheritdoc/>
    public MapPropertyAttribute(string PropertyName, Type ConverterType)
    {
        this.PropertyName = PropertyName;
        this.ConverterType = ConverterType;
    }

    /// <summary>
    /// 目标属性名称
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// 转换器类型
    /// </summary>
    public Type? ConverterType { get; }

    /// <summary>
    /// 忽略属性
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// 映射引用类型
    /// </summary>
    public MapPropertyTypes MapPropertyType { get; set; }

    ///// <summary>
    ///// 当右值不为 <see langword="NullOrDefault"/> 时才进行映射
    ///// <para>
    ///// 右值为值类型时进行非 <see langword="default"/> 检查, 右值为引用类型时进行非 <see langword="null"/> 检查
    ///// </para>
    ///// <para><code><![CDATA[
    ///// if(target.Value is not default)
    /////     source.Value = target.Value;
    ///// // OR
    ///// if(target.Value is not null)
    /////     source.Value = target.Value;
    ///// ]]></code></para>
    ///// </summary>
    //public bool CheckRightValue { get; set; }

    ///// <summary>
    ///// 当左值不为 <see langword="NullOrDefault"/> 时才进行映射
    ///// <para>
    ///// 左值为值类型时进行 <see langword="default"/> 检查, 左值为引用类型时进行 <see langword="null"/> 检查
    ///// </para>
    ///// <para><code><![CDATA[
    ///// if(source.Value is not default)
    /////     source.Value = target.Value;
    ///// // OR
    ///// if(source.Value is not null)
    /////     source.Value = target.Value;
    ///// ]]></code></para>
    ///// </summary>
    //public bool CheckLeftValue { get; set; }
}

/// <summary>
/// 属性映射类型
/// </summary>
public enum MapPropertyTypes
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
