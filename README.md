# HKW.Mapper

High-performance Mapper with SourceGenerator

## MapToAttribute

Mapping current object to target object

```csharp
[MapTo(typeof(TestTarget))]
public class TestSource
{
    public int Value { get; set; }
}

public class TestTarget
{
    public int Value { get; set; }
}
```

Generated code

```csharp
public static TestTarget MapToTestTarget(this TestSource source, TestTarget target)
{
    target.Value = source.Value;
    return target;
}
```

## MapFromAttribute

Mapping current object from target object

```csharp
[MapFrom(typeof(TestTarget))]
public class TestSource
{
    public int Value { get; set; }
}

public class TestTarget
{
    public int Value { get; set; }
}
```

Generated code

```csharp
public static TestSource MapFromTestTarget(this TestSource source, TestTarget target)
{
    source.Value = target.Value;
    return source;
}
```