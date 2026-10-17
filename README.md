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
using HKW.Mapper;

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

var createdDto = user.MapTo((UserDto?)null);
// 目标具有可访问无参构造函数时，传入 null 会创建并返回新实例

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

## 构造函数与创建式映射

目标具有可访问的无参构造函数时，现有写入式方法同时接受 nullable 目标：传入非 null 实例时保持原地写入，传入 null 时创建目标。需要调用有参构造函数时，在源类型上用可重复的 `[MapConstructor]` 精确声明目标与参数类型：

```csharp
[MapTarget(typeof(UserDto), Direction = MapDirections.To)]
[MapConstructor(typeof(UserDto), typeof(int))]
public sealed class User
{
    public required string Name { get; init; }
}

public sealed class UserDto(int revision)
{
    public int Revision { get; } = revision;
    public required string Name { get; init; }
}

var dto = user.MapTo((UserDto?)null, 3);
```

也可以使用目标名称选择构造函数：

```csharp
[MapConstructor("Api", typeof(int), typeof(string))]
```

参数类型序列必须与一个可访问的目标构造函数精确匹配。所有构造参数均由调用方提供。有参重载的 nullable 目标参数只用于选择目标；它必须为 null，传入现有实例会抛出 `ArgumentException`。无法由构造函数或对象初始化器满足 `required` 成员时，不生成对应创建重载并报告诊断。`init` 目标成员只在创建路径的对象初始化器中赋值，写入现有实例时不会修改。

## record 与 struct

`[MapTarget]` 可用于 class、record class、struct 和 record struct。可变 struct 源的 `MapFrom` 使用 `this ref`，因此接收者必须是可写变量；readonly struct 仅支持 `MapTo`，请求反向映射会产生诊断。反向映射始终修改现有源实例，不会重建不可变 record。

## Immutable 与 Frozen 集合

集合映射支持 `ImmutableArray<T>`、`ImmutableList<T>`、`ImmutableHashSet<T>`、`ImmutableDictionary<TKey,TValue>`、`FrozenSet<T>` 和 `FrozenDictionary<TKey,TValue>`。目标不可变集合通过工厂 API 创建新实例，不执行 `Clear`/`Add`；元素以及字典键值可以使用已有的嵌套映射。可空来源映射到可空不可变目标时保留 null；可空来源映射到非空不可变目标会报告诊断。

生成器仍以 `netstandard2.0` 为目标，并只在消费项目的编译中实际存在相应 Immutable/Frozen 类型时生成相关 API 调用；Frozen 集合通常要求消费项目使用支持 `System.Collections.Frozen` 的目标框架。

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

`MapParentProperties` 默认为 `false`；设置为 `true` 后，映射会包含源类型的可访问父类属性。也可以按映射目标批量忽略源属性；配置类中的忽略项会与 `MapTarget` 上的忽略项合并：

```csharp
[MapTarget(
    typeof(UserDto),
    typeof(UserMapperConfig),
    MapParentProperties = true,
    IgnoreProperties = new[] { nameof(User.InternalToken) }
)]
public sealed class User : UserBase;

public sealed class UserMapperConfig : MapperConfig<User, UserDto>
{
    public UserMapperConfig(User source, UserDto target)
        : base(source, target) { }

    public string[] IgnoreProperties { get; } = [nameof(User.AuditStamp)];
}
```

配置类的 `IgnoreProperties` 必须是可静态解析初始化器的 `string[]` 属性。

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

`PropertyName` 也可以是任意深度的点分目标成员路径。生成器从目标类型开始逐段解析属性或字段，并直接生成成员访问表达式：

```csharp
[MapTarget(typeof(UserDto))]
public sealed class User
{
    [MapProperty(typeof(UserDto), "Address.City.Name")]
    public string CityName { get; set; } = string.Empty;
}
```

路径不会自动创建中间对象，也不会生成 null 条件访问；若 `Address` 或 `City` 为 `null`，映射运行时会抛出 `NullReferenceException`。转换器收到的 `MapConvertArgs.TargetPropertyName` 是完整路径（例如 `Address.City.Name`），重复目标检测也以完整路径为准。

`MapPropertyAttribute.SourcePath` 可以指定从源对象根开始的点分成员路径：

```csharp
[MapTarget(typeof(UserDto))]
public sealed class User
{
    [MapProperty(typeof(UserDto), nameof(UserDto.CityName), SourcePath = "Address.City.Name")]
    public Address Address { get; set; } = new();
}
```

`MapTo` 读取 `source.Address.City.Name`，`MapFrom` 写回同一路径。省略目标成员名时仍使用被标注成员名，不使用 `SourcePath` 的最后一段。源路径可以混合属性与字段，但包含字段时必须启用 `MapFields = true`。路径不会创建中间对象或生成 null 条件访问；转换器收到的 `MapConvertArgs.SourcePropertyName` 是完整源路径。

`MapCompositePropertyAttribute` 同样支持 `SourcePath`，并通过 `MapCompositeConvertArgs.SourcePropertyName` 传递完整路径。反向映射要求路径叶成员可写；值类型中间属性必须按引用返回，否则生成器会报告诊断。

### 条件映射

`MapProperty`、`MapField`、`MapCompositeProperty` 和 `MapCompositeField` 均可通过 `MapToCondition` 与 `MapFromCondition` 在各方向的成员映射执行前判断：

```csharp
[MapProperty(
    typeof(UserDto),
    MapToCondition = MapMemberCondition.SourceIsNotNullOrDefault,
    MapFromCondition = MapMemberCondition.SourceIsNullOrDefault)]
public string? DisplayName { get; set; }
```

可用的 `MapMemberCondition` 值：

- `None`：默认值，无条件执行映射。
- `SourceIsNullOrDefault` / `SourceIsNotNullOrDefault`：判断原始源对象的成员。
- `TargetIsNullOrDefault` / `TargetIsNotNullOrDefault`：判断原始目标对象的成员。

`Source` 和 `Target` 始终表示 `[MapTarget]` 声明中的原始源、目标角色，不会因 `MapTo` 或 `MapFrom` 方向而交换。因此，上例的 `MapFromCondition` 仅在 `source.DisplayName` 为 null 时把目标值写回。引用类型检查 null；值类型（包括 `Nullable<T>`）与 `default(T)` 比较。

复合映射的 Target 条件会同时检查全部目标成员：`TargetIsNullOrDefault` 仅在所有目标成员都为空/default 时成立，`TargetIsNotNullOrDefault` 是该整体条件的逻辑取反。条件不成立时不会调用转换器，也不会执行该成员的赋值或集合更新。条件式 `MapTo` 无法表示为对象初始化器中的 `init`/`required` 赋值，此时生成器报告 `MAP0046` 并跳过该方向。

## 字段配置

字段映射默认关闭。设置 `MapFields = true` 后，会映射可访问的 `public`/`internal` 实例字段，并支持字段↔字段、字段↔属性和属性↔字段；`static` 与 `const` 字段始终排除。同名目标成员同时存在时属性优先。

```csharp
[MapTarget(
    typeof(UserDto),
    MapFields = true,
    MapParentFields = true,
    IgnoreFields = new[] { nameof(User.InternalToken) }
)]
public sealed class User : UserBase
{
    [MapField(typeof(UserDto), nameof(UserDto.Name))]
    public string DisplayName = string.Empty;

    [MapIgnoreField]
    internal string InternalToken = string.Empty;
}

public sealed class UserDto
{
    public string Name { get; set; } = string.Empty;
    public int Revision;
}
```

`MapParentFields` 独立控制父类字段；配置类也可通过可静态解析的 `string[] IgnoreFields` 合并忽略项。readonly 字段可以读取：源 readonly 字段支持 `MapTo`，目标 readonly 字段支持 `MapFrom`。如果当前 `Direction` 需要写入 readonly 字段（包括默认的 `Both`），生成器会报告 `MAP0031` 并跳过该成员。

`MapFieldAttribute` 支持与 `MapPropertyAttribute` 相同的重命名、点分目标成员路径、根相对 `SourcePath`、`IMapConverter<TSourceValue, TTargetValue>`、`MapType` 和忽略设置。路径可以混合属性与字段，例如 `Address.City.Name` 的任一段都可以是字段；只要源或目标路径包含字段，就必须在 `MapTargetAttribute` 上启用 `MapFields = true`。路径遵循与属性路径相同的直接访问和 null 行为。一个源字段映射到多个目标成员时，使用 `MapCompositeFieldAttribute` 和 `IMapCompositeConverter<TSourceValue, TTargetValues>`；`MapCompositeFieldAttribute` 也支持 `SourcePath`，但组合映射的目标名称仍不支持点分路径。

配置类可使用字段专用转换器：

```csharp
public MapConfigFieldConverter<int, string> NumberConverter { get; } =
    new(
        nameof(Source.Number),
        nameof(Target.Text),
        (value, _) => value.ToString(),
        (value, _) => int.Parse(value)
    );

public MapConfigFieldCompositeConverter<Range, (int Low, int High)> RangeConverter { get; } =
    new(
        nameof(Source.Range),
        [nameof(Target.Low), nameof(Target.High)],
        (value, _) => (value.Low, value.High),
        (value, _) => new(value.Low, value.High)
    );
```

`FieldName` 也可以是任意深度的点分目标成员路径。生成器从目标类型开始逐段解析属性或字段，并直接生成成员访问表达式：

```csharp
[MapTarget(typeof(UserDto))]
public sealed class User
{
    [MapField(typeof(UserDto), "Address.City.Name")]
    public string CityName = string.Empty;
}
```

路径不会自动创建中间对象，也不会生成 null 条件访问；若 `Address` 或 `City` 为 `null`，映射运行时会抛出 `NullReferenceException`。转换器收到的 `MapConvertArgs.TargetPropertyName` 是完整路径（例如 `Address.City.Name`），重复目标检测也以完整路径为准。


## 自定义转换器

### 属性级转换器

实现 `IMapConverter<TSourceValue, TTargetValue>`，然后在 `[MapPropertyAttribute]` 上指定转换器类型：

```csharp
public sealed class NumberTextConverter : IMapConverter<int, string>
{
    public string Convert(int value, MapConvertArgs args) => $"number:{value}";

    public int ConvertBack(string value, MapConvertArgs args) =>
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

`Convert` 用于源到目标的映射，`ConvertBack` 用于目标到源的映射。两者接收的 `MapConvertArgs` 都包含原始 `Source`、`Target`、`SourcePropertyName` 和 `TargetPropertyName`；这些字段的语义不会随映射方向交换。转换器泛型类型可以与属性类型相同，也可以是存在引用或装箱转换关系的基类、子类或接口。例如，`List<int>` 属性可以使用基于 `IList<int>` 声明的转换器，枚举属性也可以使用基于 `Enum` 声明的转换器。仅能显式转换时，生成代码会执行强制转换；若运行时对象不是所需具体类型，将抛出 `InvalidCastException`。

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
            (value, _) => value + 10,
            (value, _) => value - 10
        );
}
```

配置类转换器按源属性名和目标属性名查找。对于同一属性，它的优先级高于属性级转换器。

### 复合属性转换器

一个源属性需要拆分到多个目标属性时，实现
`IMapCompositeConverter<TSourceValue, TTargetValues>`，并通过
`[MapCompositeProperty]` 按顺序声明目标属性。`TTargetValues` 必须是强类型值元组，
其元素数量必须与目标属性一致；元素类型可与目标属性类型相同，或具有基类、子类、接口引用或装箱转换关系：

```csharp
public readonly record struct Range(int Min, int Max);

public sealed class RangeConverter
    : IMapCompositeConverter<Range, (int Min, int Max)>
{
    public (int Min, int Max) Convert(Range value, MapCompositeConvertArgs args) =>
        (value.Min, value.Max);

    public Range ConvertBack((int Min, int Max) values, MapCompositeConvertArgs args) =>
        new(values.Min, values.Max);
}

[MapTarget(typeof(RangeDto))]
public sealed class RangeModel
{
    [MapCompositeProperty(
        typeof(RangeDto),
        typeof(RangeConverter),
        nameof(RangeDto.Min),
        nameof(RangeDto.Max))]
    public Range Range { get; set; }
}

public sealed class RangeDto
{
    public int Min { get; set; }
    public int Max { get; set; }
}
```

`MapTo` 会拆分转换器返回的元组并依次写入 `Min`、`Max`；`MapFrom` 会按相同顺序
读取目标属性、组成元组并调用 `ConvertBack`。两个方向都会通过 `MapCompositeConvertArgs`
传入原始源对象、目标对象、源属性名以及有序目标属性名。目标属性声明顺序是映射契约的一部分。

也可以在 `MapperConfig<TSource, TTarget>` 中使用复合配置转换器，无需在源属性上添加特性：

```csharp
public sealed class RangeMapperConfig : MapperConfig<RangeModel, RangeDto>
{
    public RangeMapperConfig(RangeModel source, RangeDto target)
        : base(source, target) { }

    public MapConfigPropertyCompositeConverter<Range, (int Min, int Max)> RangeConverter { get; } =
        new(
            nameof(RangeModel.Range),
            [nameof(RangeDto.Min), nameof(RangeDto.Max)],
            (value, _) => (value.Min, value.Max),
            (values, _) => new(values.Min, values.Max));
}
```

若同一个源属性同时配置了 `MapConfigPropertyCompositeConverter` 和
`[MapCompositeProperty]`，配置类中的目标属性列表及转换逻辑具有更高优先级。
目标属性数组必须直接写在配置属性初始化器中，以便源生成器在编译期分析。

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

## 性能基准

`HKW.Mapper.Benchmark` 使用 BenchmarkDotNet 对比 HKW.Mapper 编译期生成代码与 Mapster 默认 `Adapt` 路径，包含以下场景：

- `Flat`：只包含标量属性的扁平对象
- `Complex`：嵌套对象、数组、集合以及成员扁平化
- `Recursive`：递归对象、递归集合和值类型集合
- `Total`：依次执行以上三种映射

两种映射器使用相同的输入对象并创建新的目标对象。Mapster 会在 `GlobalSetup` 中完成首次表达式编译，因此测量结果表示预热后的稳态性能，不包含 Mapster 的首次编译成本。Setup 还会验证关键值、集合数量和深拷贝结果，防止比较不同的映射语义。

运行完整的短作业基准：

```powershell
dotnet run --configuration Release --project .\HKW.Mapper.Benchmark\HKW.Mapper.Benchmark.csproj -- --filter "*MappingBenchmark*"
```

快速验证全部基准是否能够运行：

```powershell
dotnet run --configuration Release --project .\HKW.Mapper.Benchmark\HKW.Mapper.Benchmark.csproj -- --filter "*MappingBenchmark*" --job Dry
```

只运行扁平对象场景：

```powershell
dotnet run --configuration Release --project .\HKW.Mapper.Benchmark\HKW.Mapper.Benchmark.csproj -- --filter "*FlatMappingBenchmark*"
```

报告会导出为 Markdown、CSV 和 HTML，并包含 `Ns/Map`、`Bytes/Map`、耗时比率和内存比率。结果位于 `BenchmarkDotNet.Artifacts`。基准数字取决于 CPU、运行时版本、操作系统和对象形状，不应直接推广到其他应用负载。

## 从源码构建

仓库包含库项目、MSTest 测试项目和 BenchmarkDotNet 基准项目。需要安装 .NET SDK 10.0 或兼容当前测试项目的 SDK：

```bash
dotnet restore HKW.Mapper.sln
dotnet build HKW.Mapper.sln --configuration Release
dotnet test HKW.Mapper.sln
```

库项目目标框架为 `netstandard2.0`，测试项目当前目标框架为 `net10.0`。构建库项目还会生成 NuGet 包，包内包含 README 和源生成器分析器组件。

## 许可证

本项目使用 MIT License，详见 [LICENSE.txt](LICENSE.txt)。
