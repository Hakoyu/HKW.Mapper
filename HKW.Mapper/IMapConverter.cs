using System;
using System.Collections.Generic;
using System.Text;

namespace HKW.Mapper;

/// <summary>
/// 制图转换器接口
/// </summary>
/// <remarks>
/// 泛型值类型可以与映射成员类型相同，或与其具有基类、子类、接口引用或装箱转换关系
/// 需要显式引用转换时，实际值的运行时类型必须兼容
/// </remarks>
public interface IMapConverter<TSourceValue, TTargetValue>
{
    /// <summary>
    /// 转换, 在 <see langword="MapTo"/> 方法中使用
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="args">转换参数</param>
    /// <returns>转换后的值</returns>
    public TTargetValue Convert(TSourceValue value, MapConvertArgs args);

    /// <summary>
    /// 反转换, 在 <see langword="MapFrom"/> 方法中使用
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="args">转换参数</param>
    /// <returns>反转换后的值</returns>
    public TSourceValue ConvertBack(TTargetValue value, MapConvertArgs args);
}

/// <summary>
/// 映射设置成员转换器
/// </summary>
/// <typeparam name="TSourceValue">源值类型</typeparam>
/// <typeparam name="TTargetValue">目标值类型</typeparam>
public sealed class MapConfigMemberConverter<TSourceValue, TTargetValue>
    : IMapConverter<TSourceValue, TTargetValue>
{
    /// <inheritdoc/>
    /// <param name="memberName">成员名, 即源成员和目标成员同名</param>
    /// <param name="convert">转换</param>
    /// <param name="convertBack">反转换</param>
    /// <exception cref="ArgumentException">当 <paramref name="memberName"/> 为空时</exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="convert"/> 或 <paramref name="convertBack"/> 为 <see langword="null"/> 时</exception>
    public MapConfigMemberConverter(
        string memberName,
        Func<TSourceValue, MapConvertArgs, TTargetValue> convert,
        Func<TTargetValue, MapConvertArgs, TSourceValue> convertBack
    )
        : this(memberName, memberName, convert, convertBack) { }

    /// <inheritdoc/>
    /// <param name="sourceMember">源成员名</param>
    /// <param name="targetMember">目标成员名</param>
    /// <param name="convert">转换</param>
    /// <param name="convertBack">反转换</param>
    /// <exception cref="ArgumentException">当 <paramref name="sourceMember"/> 或 <paramref name="targetMember"/> 为空时</exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="convert"/> 或 <paramref name="convertBack"/> 为 <see langword="null"/> 时</exception>
    public MapConfigMemberConverter(
        string sourceMember,
        string targetMember,
        Func<TSourceValue, MapConvertArgs, TTargetValue> convert,
        Func<TTargetValue, MapConvertArgs, TSourceValue> convertBack
    )
    {
        if (string.IsNullOrWhiteSpace(sourceMember))
            throw new ArgumentException("Source member is required.", nameof(sourceMember));
        if (string.IsNullOrWhiteSpace(targetMember))
            throw new ArgumentException("Target member is required.", nameof(targetMember));
        if (convert is null)
            throw new ArgumentNullException(nameof(convert));
        if (convertBack is null)
            throw new ArgumentNullException(nameof(convertBack));

        _convert = convert;
        _convertBack = convertBack;
    }

    private readonly Func<TSourceValue, MapConvertArgs, TTargetValue> _convert;
    private readonly Func<TTargetValue, MapConvertArgs, TSourceValue> _convertBack;

    /// <inheritdoc/>
    public TTargetValue Convert(TSourceValue value, MapConvertArgs args) => _convert(value, args);

    /// <inheritdoc/>
    public TSourceValue ConvertBack(TTargetValue value, MapConvertArgs args) =>
        _convertBack(value, args);
}
