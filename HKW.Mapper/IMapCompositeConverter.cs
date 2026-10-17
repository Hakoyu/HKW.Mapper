namespace HKW.Mapper;

/// <summary>
/// 将一个源属性双向转换为多个目标属性
/// </summary>
/// <remarks>
/// 源值和目标元组元素类型可以与映射属性类型相同，或与其具有基类、子类、接口引用或装箱转换关系
/// 需要显式引用转换时，实际值的运行时类型必须兼容
/// </remarks>
/// <typeparam name="TSourceValue">源属性类型</typeparam>
/// <typeparam name="TTargetValues">由多个目标属性值组成的值元组类型</typeparam>
public interface IMapCompositeConverter<TSourceValue, TTargetValues>
    where TTargetValues : struct
{
    /// <summary>
    /// 将源属性转换为多个目标属性值
    /// </summary>
    public TTargetValues Convert(TSourceValue value, MapCompositeConvertArgs args);

    /// <summary>
    /// 将多个目标属性值转换回源属性
    /// </summary>
    public TSourceValue ConvertBack(TTargetValues values, MapCompositeConvertArgs args);
}

/// <summary>
/// 映射设置复合属性转换器
/// </summary>
/// <typeparam name="TSourceValue">源值类型</typeparam>
/// <typeparam name="TTargetValues">由多个目标属性值组成的值元组类型</typeparam>
public sealed class MapConfigCompositePropertyConverter<TSourceValue, TTargetValues>
    : IMapCompositeConverter<TSourceValue, TTargetValues>
    where TTargetValues : struct
{
    /// <summary>
    /// 初始化映射设置复合属性转换器
    /// </summary>
    public MapConfigCompositePropertyConverter(
        string sourceProperty,
        string[] targetProperties,
        Func<TSourceValue, MapCompositeConvertArgs, TTargetValues> convert,
        Func<TTargetValues, MapCompositeConvertArgs, TSourceValue> convertBack
    )
    {
        if (string.IsNullOrWhiteSpace(sourceProperty))
            throw new ArgumentException("Source property is required.", nameof(sourceProperty));
        if (targetProperties is null)
            throw new ArgumentNullException(nameof(targetProperties));
        if (
            targetProperties.Length < 2
            || targetProperties.Any(string.IsNullOrWhiteSpace)
            || targetProperties.Distinct(StringComparer.Ordinal).Count() != targetProperties.Length
        )
            throw new ArgumentException(
                "At least two unique, non-empty target properties are required.",
                nameof(targetProperties)
            );
        if (convert is null)
            throw new ArgumentNullException(nameof(convert));
        if (convertBack is null)
            throw new ArgumentNullException(nameof(convertBack));

        _convert = convert;
        _convertBack = convertBack;
    }

    private readonly Func<TSourceValue, MapCompositeConvertArgs, TTargetValues> _convert;
    private readonly Func<TTargetValues, MapCompositeConvertArgs, TSourceValue> _convertBack;

    /// <inheritdoc/>
    public TTargetValues Convert(TSourceValue value, MapCompositeConvertArgs args) =>
        _convert(value, args);

    /// <inheritdoc/>
    public TSourceValue ConvertBack(TTargetValues values, MapCompositeConvertArgs args) =>
        _convertBack(values, args);
}

/// <summary>
/// 映射设置复合字段转换器
/// </summary>
public sealed class MapConfigCompositeFieldConverter<TSourceValue, TTargetValues>
    : IMapCompositeConverter<TSourceValue, TTargetValues>
    where TTargetValues : struct
{
    /// <inheritdoc/>
    public MapConfigCompositeFieldConverter(
        string sourceField,
        string[] targetFields,
        Func<TSourceValue, MapCompositeConvertArgs, TTargetValues> convert,
        Func<TTargetValues, MapCompositeConvertArgs, TSourceValue> convertBack
    )
    {
        if (string.IsNullOrWhiteSpace(sourceField))
            throw new ArgumentException("Source field is required.", nameof(sourceField));
        if (targetFields is null)
            throw new ArgumentNullException(nameof(targetFields));
        if (
            targetFields.Length < 2
            || targetFields.Any(string.IsNullOrWhiteSpace)
            || targetFields.Distinct(StringComparer.Ordinal).Count() != targetFields.Length
        )
            throw new ArgumentException(
                "At least two unique, non-empty target fields are required.",
                nameof(targetFields)
            );
        if (convert is null)
            throw new ArgumentNullException(nameof(convert));
        if (convertBack is null)
            throw new ArgumentNullException(nameof(convertBack));

        _convert = convert;
        _convertBack = convertBack;
    }

    private readonly Func<TSourceValue, MapCompositeConvertArgs, TTargetValues> _convert;
    private readonly Func<TTargetValues, MapCompositeConvertArgs, TSourceValue> _convertBack;

    /// <inheritdoc/>
    public TTargetValues Convert(TSourceValue value, MapCompositeConvertArgs args) =>
        _convert(value, args);

    /// <inheritdoc/>
    public TSourceValue ConvertBack(TTargetValues values, MapCompositeConvertArgs args) =>
        _convertBack(values, args);
}
