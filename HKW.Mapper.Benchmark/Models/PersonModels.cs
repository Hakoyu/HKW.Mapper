namespace HKW.Mapper.Benchmark.Models;

[MapTarget(typeof(PersonDto), Direction = MapDirections.To)]
public sealed class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public DateTime BirthDate { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class PersonDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public DateTime BirthDate { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
}
