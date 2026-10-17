using HKW.Mapper.Benchmark.Models;
using Mapster;

namespace HKW.Mapper.Benchmark.Infrastructure;

internal static class MappingConfiguration
{
    private static bool _configured;
    private static readonly Lock SyncRoot = new();

    public static void EnsureMapsterConfigured()
    {
        if (_configured)
            return;

        lock (SyncRoot)
        {
            if (_configured)
                return;

            TypeAdapterConfig<Customer, CustomerDto>
                .NewConfig()
                .Map(destination => destination.AddressCity, source => source.Address.City);

            _configured = true;
        }
    }

    public static void WarmMapster(Person source) => _ = source.Adapt<PersonDto>();

    public static void WarmMapster(Customer source) => _ = source.Adapt<CustomerDto>();

    public static void WarmMapster(Foo source) => _ = source.Adapt<FooDto>();
}
