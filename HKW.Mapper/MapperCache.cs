using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace HKW.HKWMapper;

/// <summary>
/// 映射器缓存
/// </summary>
public static class MapperCache
{
    /// <summary>
    /// 按类型分类的映射器设置
    /// <para>(MapperConfigType, MapperConfig)</para>
    /// </summary>
    public static ConcurrentDictionary<Type, IMapperConfig> ConfigByType { get; } = [];

    /// <summary>
    /// 获取映射器设置
    /// </summary>
    /// <typeparam name="TConfig">设置类型</typeparam>
    /// <returns>映射器</returns>
    public static TConfig GetConfig<TConfig>()
        where TConfig : class, IMapperConfig, new()
    {
        var type = typeof(TConfig);
        if (ConfigByType.TryGetValue(type, out var config) is false)
        {
            config = ConfigByType[type] = new TConfig();
            config.Frozen();
        }
        return (TConfig)config;
    }

    /// <summary>
    /// 按类型分类的转换器
    /// <para>(MapperConfigType, MapperConfig)</para>
    /// </summary>
    public static ConcurrentDictionary<Type, IMapConverter> ConverterByType { get; } = [];

    /// <summary>
    /// 获取转换器设置
    /// </summary>
    /// <typeparam name="TConverter">设置类型</typeparam>
    /// <returns>转换器</returns>
    public static TConverter GetConverter<TConverter>()
        where TConverter : class, IMapConverter, new()
    {
        var type = typeof(TConverter);
        if (ConverterByType.TryGetValue(type, out var converter) is false)
            converter = ConverterByType[type] = new TConverter();
        return (TConverter)converter;
    }
}
