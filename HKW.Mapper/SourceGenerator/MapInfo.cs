using Microsoft.CodeAnalysis;

namespace HKW.HKWMapper.SourceGenerator;

internal class MapInfo(INamedTypeSymbol type, string methodName, bool scrutiny)
    : IEquatable<MapInfo>
{
    public INamedTypeSymbol Type { get; set; } = type;

    public string MethodName { get; set; } = methodName;

    public bool Scrutiny { get; set; } = scrutiny;

    public bool Equals(MapInfo? other)
    {
        if (other is null)
            return false;
        return SymbolEqualityComparer.Default.Equals(Type, other.Type)
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
            hashCode * -1521134295 + EqualityComparer<INamedTypeSymbol>.Default.GetHashCode(Type);
        hashCode =
            hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MethodName);
        return hashCode;
    }
}
