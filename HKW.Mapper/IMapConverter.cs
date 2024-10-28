using System;
using System.Collections.Generic;
using System.Text;

namespace HKW.HKWMapper;

/// <summary>
/// 制图转换器接口
/// </summary>
internal interface IMapConverter
{
    /// <summary>
    /// 转换, 在<c> MapTo </c>方法中使用
    /// </summary>
    /// <param name="source">源</param>
    /// <param name="value">值</param>
    /// <returns>转换后的值</returns>
    public object Convert(object source, object value);

    /// <summary>
    /// 反转换, 在<c> MapFrom </c>方法中使用
    /// </summary>
    /// <param name="source">源</param>
    /// <param name="value">值</param>
    /// <returns>反转换后的值</returns>
    public object ConvertBack(object source, object value);
}

/// <summary>
/// 制图转换器接口
/// </summary>
public interface IMapConverter<TValue, TTarget>
{
    /// <summary>
    /// 转换, 在<c> MapTo </c>方法中使用
    /// </summary>
    /// <param name="source">源</param>
    /// <param name="value">值</param>
    /// <returns>转换后的值</returns>
    public TTarget Convert(object source, TValue value);

    /// <summary>
    /// 反转换, 在<c> MapFrom </c>方法中使用
    /// </summary>
    /// <param name="source">源</param>
    /// <param name="value">值</param>
    /// <returns>反转换后的值</returns>
    public TValue ConvertBack(object source, TTarget value);
}
