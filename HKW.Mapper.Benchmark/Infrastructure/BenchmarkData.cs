using HKW.Mapper.Benchmark.Models;

namespace HKW.Mapper.Benchmark.Infrastructure;

internal static class BenchmarkData
{
    public static Person CreatePerson() =>
        new()
        {
            Id = 42,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            Age = 36,
            BirthDate = new DateTime(1815, 12, 10),
            Salary = 12_345.67m,
            IsActive = true,
        };

    public static Customer CreateCustomer() =>
        new()
        {
            Id = 1,
            Name = "Example customer",
            Credit = 234.7m,
            Address = CreateAddress(1, "Main Street", "Istanbul"),
            HomeAddress = CreateAddress(2, "Home Street", "Ankara"),
            Addresses =
            [
                CreateAddress(3, "First Avenue", "Istanbul"),
                CreateAddress(4, "Second Avenue", "Izmir"),
            ],
            WorkAddresses =
            [
                CreateAddress(5, "Office Road", "Istanbul"),
                CreateAddress(6, "Factory Road", "Bursa"),
            ],
        };

    public static Foo CreateFoo() =>
        new()
        {
            Name = "root",
            Int32 = 12,
            Int64 = 123_123,
            NullableInt = 16,
            Float = 12.5f,
            Double = 2_312_112.25,
            DateTime = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            Child = new Foo { Name = "child", Int32 = 21 },
            Children =
            [
                new Foo { Name = "list-1", Int64 = 123, NullableInt = 321 },
                new Foo { Name = "list-2", Int32 = 12_345, NullableInt = 54_321 },
                new Foo { Name = "list-3", Int32 = 67_890, NullableInt = 98_765 },
            ],
            ChildArray = [new Foo { Name = "array-1" }, new Foo { Name = "array-2" }],
            IntArray = [1, 2, 3, 4, 5],
            Ints = [7, 8, 9],
        };

    private static Address CreateAddress(int id, string street, string city) =>
        new()
        {
            Id = id,
            Street = street,
            City = city,
            Country = "Turkey",
        };
}
