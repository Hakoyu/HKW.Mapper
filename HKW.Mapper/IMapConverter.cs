using System;
using System.Collections.Generic;
using System.Text;

namespace HKW.HKWMapper;

/// <summary>
/// 制图转换器接口
/// </summary>
public interface IMapConverter<TSourceValue, TTargetValue>
{
    /// <summary>
    /// 转换, 在 <see langword="MapTo"/> 方法中使用
    /// </summary>
    /// <param name="source">源</param>
    /// <param name="value">值</param>
    /// <returns>转换后的值</returns>
    public TTargetValue Convert(object source, TSourceValue value);

    /// <summary>
    /// 反转换, 在 <see langword="MapFrom"/> 方法中使用
    /// </summary>
    /// <param name="target">目标源</param>
    /// <param name="value">值</param>
    /// <returns>反转换后的值</returns>
    public TSourceValue ConvertBack(object target, TTargetValue value);
}

/// <summary>
/// 映射设置属性转换器
/// </summary>
/// <typeparam name="TSourceValue">源值类型</typeparam>
/// <typeparam name="TTargetValue">目标值类型</typeparam>
public sealed class MapConfigPropertyConverter<TSourceValue, TTargetValue>
    : IMapConverter<TSourceValue, TTargetValue>
{
    /// <inheritdoc/>
    /// <param name="propertyName">属性名, 即源属性和目标属性同名</param>
    /// <param name="convert">转换</param>
    /// <param name="convertBack">反转换</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="convert"/> 或 <paramref name="convertBack"/> 为 <see langword="null"/> 时</exception>
    public MapConfigPropertyConverter(
        string propertyName,
        Func<object, TSourceValue, TTargetValue> convert,
        Func<object, TTargetValue, TSourceValue> convertBack
    )
        : this(propertyName, propertyName, convert, convertBack) { }

    /// <inheritdoc/>
    /// <param name="sourceProperty">源属性名</param>
    /// <param name="targetProperty">目标属性名</param>
    /// <param name="convert">转换</param>
    /// <param name="convertBack">反转换</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="convert"/> 或 <paramref name="convertBack"/> 为 <see langword="null"/> 时</exception>
    public MapConfigPropertyConverter(
        string sourceProperty,
        string targetProperty,
        Func<object, TSourceValue, TTargetValue> convert,
        Func<object, TTargetValue, TSourceValue> convertBack
    )
    {
        if (convert is null)
            throw new ArgumentNullException(nameof(convert));
        if (convertBack is null)
            throw new ArgumentNullException(nameof(convertBack));

        _convert = convert;
        _convertBack = convertBack;
    }

    private readonly Func<object, TSourceValue, TTargetValue> _convert;
    private readonly Func<object, TTargetValue, TSourceValue> _convertBack;

    /// <inheritdoc/>
    public TTargetValue Convert(object source, TSourceValue value)
    {
        return _convert(source, value);
    }

    /// <inheritdoc/>
    public TSourceValue ConvertBack(object target, TTargetValue value)
    {
        return _convertBack(target, value);
    }
}
