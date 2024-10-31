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
    /// <para>Default is current property name</para>
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// ConverterType
    /// </summary>
    public Type? ConverterType { get; set; }
}
