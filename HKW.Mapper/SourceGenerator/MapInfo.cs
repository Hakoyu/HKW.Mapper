using Microsoft.CodeAnalysis;

namespace HKW.HKWMapper.SourceGenerator;

internal class MapInfo : IEquatable<MapInfo>
{
    public MapInfo(
        AttributeData attribute,
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        string methodName
    )
    {
        Attribute = attribute;
        SourceType = sourceType;
        TargetType = targetType;
        MethodName = methodName;
        PropertyAttributeFullName =
            $"{sourceType.ContainingNamespace}.{sourceType.Name}{methodName}PropertyAttribute";
    }

    public AttributeData Attribute { get; }

    /// <summary>
    /// 源类型
    /// </summary>
    public INamedTypeSymbol SourceType { get; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public INamedTypeSymbol TargetType { get; }

    /// <summary>
    /// 方法名称
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// 属性特性名称
    /// </summary>
    public string PropertyAttributeFullName { get; }

    /// <summary>
    /// 严格模式
    /// </summary>
    public bool ScrutinyMode { get; set; }

    /// <summary>
    /// 映射设置类型
    /// </summary>
    public INamedTypeSymbol? ConfigType { get; set; }

    /// <summary>
    /// 方法执行类型
    /// </summary>
    public MapMethodInvokeState InvokeState { get; set; }

    public Dictionary<INamedTypeSymbol, int> IndexByConverter { get; } =
        new(SymbolEqualityComparer.Default);

    /// <summary>
    /// 是映射至
    /// </summary>
    public bool IsMapTo { get; set; }

    /// <summary>
    /// 是异步
    /// </summary>
    public bool IsAsync { get; set; }

    public bool Equals(MapInfo? other)
    {
        if (other is null)
            return false;
        return SymbolEqualityComparer.Default.Equals(TargetType, other.TargetType)
            && MethodName == other.MethodName;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as MapInfo);
    }

    public override int GetHashCode()
    {
        int hashCode = 530843752;
        hashCode =
            hashCode * -1521134295
            + EqualityComparer<INamedTypeSymbol>.Default.GetHashCode(TargetType);
        hashCode =
            hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MethodName);
        return hashCode;
    }
}
