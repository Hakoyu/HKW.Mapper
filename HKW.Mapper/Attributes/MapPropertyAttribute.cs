namespace HKW.HKWMapper;

/// <summary>
/// MapPropertyAttribute
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

    /// <summary>
    /// PropertyName
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// PropertyAction
    /// <para>Default is <c>[source|target].{value}</c></para>
    /// <para>e.g. <c>source.{value}.ToString()</c>, <c>int.Parse(target.{value})</c> etc.</para>
    /// </summary>
    public string? PropertyAction { get; set; }

    /// <summary>
    /// ConverterType
    /// </summary>
    public Type? ConverterType { get; set; }
}
