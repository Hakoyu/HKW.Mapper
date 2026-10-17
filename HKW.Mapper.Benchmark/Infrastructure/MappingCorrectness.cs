using HKW.Mapper.Benchmark.Models;
using Mapster;

namespace HKW.Mapper.Benchmark.Infrastructure;

internal static class MappingCorrectness
{
    public static void Verify(Person source)
    {
        var hkw = source.MapTo(new PersonDto());
        var mapster = source.Adapt<PersonDto>();

        Require(hkw.Id == source.Id && hkw.Email == source.Email && hkw.Salary == source.Salary);
        Require(mapster.Id == hkw.Id && mapster.Email == hkw.Email && mapster.Salary == hkw.Salary);
    }

    public static void Verify(Customer source)
    {
        var hkw = source.MapTo(new CustomerDto());
        var mapster = source.Adapt<CustomerDto>();

        Require(hkw.AddressCity == source.Address.City);
        Require(mapster.AddressCity == hkw.AddressCity);
        Require(hkw.Addresses.Length == source.Addresses.Length);
        Require(mapster.Addresses.Length == hkw.Addresses.Length);
        Require(hkw.WorkAddresses.Count == source.WorkAddresses.Count);
        Require(mapster.WorkAddresses.Count == hkw.WorkAddresses.Count);
        Require(!ReferenceEquals(hkw.Address, source.Address));
        Require(!ReferenceEquals(mapster.Address, source.Address));
        Require(!ReferenceEquals(hkw.Addresses[0], source.Addresses[0]));
        Require(!ReferenceEquals(mapster.Addresses[0], source.Addresses[0]));
    }

    public static void Verify(Foo source)
    {
        var hkw = source.MapTo(new FooDto());
        var mapster = source.Adapt<FooDto>();

        Require(hkw.Child?.Name == source.Child?.Name);
        Require(mapster.Child?.Name == hkw.Child?.Name);
        Require(hkw.Children.Count == source.Children.Count);
        Require(mapster.Children.Count == hkw.Children.Count);
        Require(hkw.ChildArray.Length == source.ChildArray.Length);
        Require(mapster.ChildArray.Length == hkw.ChildArray.Length);
        Require(!ReferenceEquals(hkw.Child, source.Child));
        Require(!ReferenceEquals(mapster.Child, source.Child));
        Require(!ReferenceEquals(hkw.Children[0], source.Children[0]));
        Require(!ReferenceEquals(mapster.Children[0], source.Children[0]));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException("The benchmark mappers produced different results.");
    }
}
