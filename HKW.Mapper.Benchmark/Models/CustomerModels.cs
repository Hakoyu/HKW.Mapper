namespace HKW.Mapper.Benchmark.Models;

[MapTarget(typeof(AddressDto), Direction = MapDirections.To)]
public sealed class Address
{
    public Address() { }

    public Address(Address source)
    {
        Id = source.Id;
        Street = source.Street;
        City = source.City;
        Country = source.Country;
    }

    public int Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public sealed class AddressDto
{
    public int Id { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

[MapTarget(typeof(CustomerDto), typeof(CustomerMappingConfig), Direction = MapDirections.To)]
public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Credit { get; set; }

    [MapProperty(typeof(CustomerDto), MapType = MapPropertyType.ConstructFromSelf)]
    public Address Address { get; set; } = new();

    [MapProperty(
        typeof(CustomerDto),
        nameof(CustomerDto.HomeAddress),
        typeof(AddressConverter)
    )]
    public Address HomeAddress { get; set; } = new();
    public Address[] Addresses { get; set; } = [];
    public List<Address> WorkAddresses { get; set; } = [];
}

public sealed class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Credit { get; set; }
    public Address Address { get; set; } = new();
    public AddressDto HomeAddress { get; set; } = new();
    public AddressDto[] Addresses { get; set; } = [];
    public List<AddressDto> WorkAddresses { get; set; } = [];
    public string AddressCity { get; set; } = string.Empty;
}

public sealed class AddressConverter : IMapConverter<Address, AddressDto>
{
    public AddressDto Convert(Address value, MapConvertArgs args) => value.MapTo(new AddressDto());

    public Address ConvertBack(AddressDto value, MapConvertArgs args) =>
        throw new NotSupportedException();
}

public sealed class CustomerMappingConfig : MapperConfig<Customer, CustomerDto>
{
    public CustomerMappingConfig(Customer source, CustomerDto target)
        : base(source, target) { }

    [MapToConfigAction(MapConfigActionMode.End)]
    public void FlattenAddressCity() => Target.AddressCity = Source.Address.City;
}
