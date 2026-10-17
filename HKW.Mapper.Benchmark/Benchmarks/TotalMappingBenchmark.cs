using BenchmarkDotNet.Attributes;
using HKW.Mapper.Benchmark.Infrastructure;
using HKW.Mapper.Benchmark.Models;
using Mapster;

namespace HKW.Mapper.Benchmark.Benchmarks;

public class TotalMappingBenchmark : MappingBenchmarkBase
{
    private Person _person = null!;
    private Customer _customer = null!;
    private Foo _foo = null!;

    [GlobalSetup]
    public void Setup()
    {
        MappingConfiguration.EnsureMapsterConfigured();
        _person = BenchmarkData.CreatePerson();
        _customer = BenchmarkData.CreateCustomer();
        _foo = BenchmarkData.CreateFoo();

        MappingConfiguration.WarmMapster(_person);
        MappingConfiguration.WarmMapster(_customer);
        MappingConfiguration.WarmMapster(_foo);
        MappingCorrectness.Verify(_person);
        MappingCorrectness.Verify(_customer);
        MappingCorrectness.Verify(_foo);
    }

    [Benchmark(Baseline = true, Description = "Mapster 10.0.13")]
    public void MapsterDefault()
    {
        MappingLoop.Run(_person, static source => source.Adapt<PersonDto>(), MapOperations);
        MappingLoop.Run(_customer, static source => source.Adapt<CustomerDto>(), MapOperations);
        MappingLoop.Run(_foo, static source => source.Adapt<FooDto>(), MapOperations);
    }

    [Benchmark(Description = "HKW.Mapper 0.3.2")]
    public void HkwMapper()
    {
        MappingLoop.Run(
            _person,
            static source => source.MapTo(new PersonDto()),
            MapOperations
        );
        MappingLoop.Run(
            _customer,
            static source => source.MapTo(new CustomerDto()),
            MapOperations
        );
        MappingLoop.Run(_foo, static source => source.MapTo(new FooDto()), MapOperations);
    }
}
