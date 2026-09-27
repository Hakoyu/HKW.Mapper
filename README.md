# HKW.Mapper

基于 Roslyn Source Generator 的高性能对象映射库。映射代码在编译期生成，不依赖运行时反射；支持双向映射、属性重命名、属性忽略、自定义转换器以及映射生命周期配置。

## 特性

- 编译期生成映射代码，减少运行时开销
- 根据 `[MapTarget]` 声明生成 `MapTo`、`MapFrom` 或双向扩展方法，默认生成双向方法
- 支持同步映射，以及在配置动作包含异步方法时生成异步映射方法
- 支持属性重命名、忽略属性和自定义类型转换器
- 支持映射配置的开始、结束、属性前、属性后和替换属性操作
- NuGet 包：`HKW.Mapper`

## 快速开始

在源类型上使用 `[MapTargetAttribute]`，参数为目标类型：

```csharp
using HKW.HKWMapper;

namespace Sample;

[MapTarget(typeof(UserDto))]
public sealed class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

编译后会生成以下扩展方法。写入式重载返回写入后的目标对象；当目标类型有可访问的无参构造函数时，还会生成无参创建式重载：

```csharp
var user = new User { Id = 1, Name = "Ada" };
var dto = new UserDto();

user.MapTo(dto);
// dto.Id == 1
// dto.Name == "Ada"

dto.Id = 2;
user.MapFrom(dto);
// user.Id == 2
```

目标名称默认使用目标类型名称，也可以显式指定 `TargetName`：

```csharp
[MapTarget(typeof(UserDto), TargetName = "User")]
public sealed class User
{
    public int Id { get; set; }
}
```

此时生成的方法名为 `MapToUser` 和 `MapFromUser`。同一个源类型可以通过多个 `[MapTargetAttribute]` 映射到多个目标类型，但每个 `TargetName` 必须不同。

## 属性配置

### 忽略属性

在源属性上添加 `[MapIgnorePropertyAttribute]` 后，该属性在两个方向都不会被映射：

```csharp
[MapTarget(typeof(UserDto))]
public sealed class User
{
    public int Id { get; set; }

    [MapIgnoreProperty]
    public string InternalToken { get; set; } = string.Empty;
}
```

### 重命名属性

使用固定的公共 `[MapPropertyAttribute]` 配置目标类型或目标名称，不再生成专用属性特性：

```text
[MapProperty(typeof(UserDto), "Name")]
```

例如，`User` 映射到 `UserDto` 时，可以这样把 `DisplayName` 映射到 `Name`：

```csharp
[MapTarget(typeof(UserDto))]
public sealed class User
{
    [MapProperty(typeof(UserDto), "Name")]
    public string DisplayName { get; set; } = string.Empty;
}
```

`MapProperty` 的目标类型或目标名称必须匹配当前源类型上的一个 `[MapTargetAttribute]`；未匹配或重复匹配会产生编译诊断。若不需要属性级设置，则同名属性会自动映射。

## 自定义转换器

### 属性级转换器

实现 `IMapConverter<TSourceValue, TTargetValue>`，然后在 `[MapPropertyAttribute]` 上指定转换器类型：

```csharp
public sealed class NumberTextConverter : IMapConverter<int, string>
{
    public string Convert(object source, int value) => $"number:{value}";

    public int ConvertBack(object target, string value) =>
        int.Parse(value[7..]);
}

[MapTarget(typeof(NumberDto))]
public sealed class NumberModel
{
    [MapProperty(typeof(NumberDto), "Text", ConverterType = typeof(NumberTextConverter))]
    public int Number { get; set; }
}

public sealed class NumberDto
{
    public string Text { get; set; } = string.Empty;
}
```

`Convert` 用于源到目标的映射，`ConvertBack` 用于目标到源的映射。转换器必须实现与属性类型完全匹配的泛型接口。

### 配置类转换器

需要为映射对集中管理转换逻辑时，可以传入继承自 `MapperConfig<TSource, TTarget>` 的配置类：

```csharp
[MapTarget(typeof(OrderDto), typeof(OrderMapperConfig))]
public sealed class Order
{
    public int Amount { get; set; }
}

public sealed class OrderDto
{
    public int Amount { get; set; }
}

public sealed class OrderMapperConfig : MapperConfig<Order, OrderDto>
{
    public OrderMapperConfig(Order source, OrderDto target)
        : base(source, target) { }

    public MapConfigPropertyConverter<int, int> AmountConverter { get; } =
        new(
            nameof(Order.Amount),
            (_, value) => value + 10,
            (_, value) => value - 10
        );
}
```

配置类转换器按源属性名和目标属性名查找。对于同一属性，它的优先级高于属性级转换器。

## 引用类型属性

为避免意外共享对象引用，普通引用类型默认不会自动复制，并会产生编译诊断。集合和已注册的嵌套映射会递归生成；数组、`List<T>` 和 `Dictionary<TKey,TValue>` 使用编译期生成的循环映射。nullable 使用统一策略：目标可空类型保留 null，目标不可空类型对 null 使用 `default`。

```csharp
public sealed class Address
{
    public string City { get; set; } = string.Empty;

    public Address() { }

    public Address(Address other) => City = other.City;
}

[MapTarget(typeof(UserDto))]
public sealed class User
{
    [MapProperty(typeof(UserDto), nameof(Address), MapType = MapPropertyType.ConstructFromSelf)]
    public Address Address { get; set; } = new(new Address { City = "" });
}
```

可用的 `MapPropertyType` 值：

- `None`：默认行为；引用类型不会自动映射，值类型按普通赋值映射，如果属性类型实现了 `ICloneable`，则会调用 `Clone()` 进行复制。
- `Reference`：允许直接复制引用
- `ConstructFromSelf`：使用目标类型可用的自身构造函数创建新对象

## 映射生命周期配置

配置类的方法可以使用以下特性参与映射流程：

```csharp
public sealed class OrderMapperConfig : MapperConfig<Order, OrderDto>
{
    public OrderMapperConfig(Order source, OrderDto target)
        : base(source, target) { }

    [MapToConfigAction(MapConfigActionMode.Start)]
    [MapFromConfigAction(MapConfigActionMode.Start)]
    public void Start()
    {
        // 映射开始时执行
    }

    [MapToConfigAction(MapConfigActionMode.BeforeProperty, nameof(Order.Amount))]
    public void BeforeAmount()
    {
        // Amount 映射前执行
    }

    [MapToConfigAction(MapConfigActionMode.AfterProperty, nameof(Order.Amount))]
    public void AfterAmount()
    {
        // Amount 映射后执行
    }

    [MapToConfigAction(MapConfigActionMode.ReplaceProperty, nameof(Order.Amount))]
    public void ReplaceAmount()
    {
        // 替换 Amount 的默认属性映射
    }

    [MapToConfigAction(MapConfigActionMode.End)]
    public void End()
    {
        // 映射结束时执行
    }
}
```

`MapToConfigAction` 作用于源到目标，`MapFromConfigAction` 作用于目标到源。配置方法可以返回 `Task`；当对应映射流程包含异步配置方法时，源生成器会同时生成 `MapTo...Async` 和 `MapFrom...Async` 方法。

## 生成的方法规则

对 `[MapTarget(typeof(TargetType))]`：

| 声明 | 生成的方法 |
| --- | --- |
| 默认目标名称（`TargetName == TargetType.Name`） | `MapTo`、`MapFrom` |
| 设置 `TargetName = "Custom"` | `MapToCustom`、`MapFromCustom` |
| 异步配置动作 | 额外生成对应的 `MapTo...Async`、`MapFrom...Async` |

映射方法接收一个已创建的目标对象并写入其属性，同时返回该目标对象。目标类型支持无参初始化时，`MapToXxx()` 会创建并返回目标对象。

## 映射方向和边界

可以通过 `[MapTarget(typeof(UserDto), Direction = MapDirections.To)]` 只生成源到目标方法，也可以使用 `MapDirections.From` 或默认的 `MapDirections.Both`。数组、`IList<T>`、`IDictionary<TKey, TValue>` 和 `ISet<T>` 会分别生成专用映射代码；集合目标需要可写、可清空并支持添加元素。嵌套对象要求子类型存在对应的 `[MapTarget]` 映射，并且目标可无参初始化；嵌套映射的 To/From 方向分别检查。多态映射仅对编译期已声明的具体源/目标映射生成分派代码，不会通过反射发现未知派生类型。

## 从源码构建

仓库包含库项目和 MSTest 测试项目。需要安装 .NET SDK 10.0 或兼容当前测试项目的 SDK：

```bash
dotnet restore HKW.Mapper.sln
dotnet build HKW.Mapper.sln --configuration Release
dotnet test HKW.Mapper.sln
```

库项目目标框架为 `netstandard2.0`，测试项目当前目标框架为 `net10.0`。构建库项目还会生成 NuGet 包，包内包含 README 和源生成器分析器组件。

## 许可证

本项目使用 MIT License，详见 [LICENSE.txt](LICENSE.txt)。
