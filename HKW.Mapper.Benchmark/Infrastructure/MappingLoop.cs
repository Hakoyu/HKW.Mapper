namespace HKW.Mapper.Benchmark.Infrastructure;

internal static class MappingLoop
{
    public static void Run<TSource, TDestination>(
        TSource source,
        Func<TSource, TDestination> map,
        int count
    )
    {
        TDestination? result = default;
        for (var i = 0; i < count; i++)
            result = map(source);

        GC.KeepAlive(result);
    }
}
