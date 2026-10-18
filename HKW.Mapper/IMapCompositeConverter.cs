namespace HKW.Mapper;

/// <summary>
/// 将一个源成员双向转换为多个目标成员
/// </summary>
/// <remarks>
/// 源值和目标元组元素类型可以与映射成员类型相同，或与其具有基类、子类、接口引用或装箱转换关系
/// 需要显式引用转换时，实际值的运行时类型必须兼容
/// </remarks>
/// <typeparam name="TSourceValue">源成员类型</typeparam>
/// <typeparam name="TTargetValues">由多个目标成员值组成的值元组类型</typeparam>
public interface IMapCompositeConverter<TSourceValue, TTargetValues>
    where TTargetValues : struct
{
    /// <summary>
    /// 将源成员转换为多个目标成员值
    /// </summary>
    public TTargetValues Convert(TSourceValue value, MapCompositeConvertArgs args);

    /// <summary>
    /// 将多个目标成员值转换回源成员
    /// </summary>
    public TSourceValue ConvertBack(TTargetValues values, MapCompositeConvertArgs args);
}

/// <summary>
/// 映射设置复合成员转换器
/// </summary>
/// <typeparam name="TSourceValue">源值类型</typeparam>
/// <typeparam name="TTargetValues">由多个目标成员值组成的值元组类型</typeparam>
public sealed class MapConfigCompositeMemberConverter<TSourceValue, TTargetValues>
    : IMapCompositeConverter<TSourceValue, TTargetValues>
    where TTargetValues : struct
{
    /// <summary>
    /// 初始化映射设置复合成员转换器
    /// </summary>
    public MapConfigCompositeMemberConverter(
        string sourceMember,
        string[] targetMembers,
        Func<TSourceValue, MapCompositeConvertArgs, TTargetValues> convert,
        Func<TTargetValues, MapCompositeConvertArgs, TSourceValue> convertBack
    )
    {
        if (string.IsNullOrWhiteSpace(sourceMember))
            throw new ArgumentException("Source member is required.", nameof(sourceMember));
        if (targetMembers is null)
            throw new ArgumentNullException(nameof(targetMembers));
        if (
            targetMembers.Length < 2
            || targetMembers.Any(string.IsNullOrWhiteSpace)
            || targetMembers.Distinct(StringComparer.Ordinal).Count() != targetMembers.Length
        )
            throw new ArgumentException(
                "At least two unique, non-empty target members are required.",
                nameof(targetMembers)
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
