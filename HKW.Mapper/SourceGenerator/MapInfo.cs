using Microsoft.CodeAnalysis;

namespace HKW.HKWMapper.SourceGenerator;

internal class MapInfo(
    INamedTypeSymbol targetType,
    string methodName,
    bool scrutinyMode,
    INamedTypeSymbol? mapConfigType
) : IEquatable<MapInfo>
{
    public INamedTypeSymbol TargetType { get; set; } = targetType;

    public string MethodName { get; set; } = methodName;

    public bool ScrutinyMode { get; set; } = scrutinyMode;

    public INamedTypeSymbol? MapConfigType { get; set; } = mapConfigType;

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
