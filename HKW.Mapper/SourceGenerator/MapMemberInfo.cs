using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace HKW.Mapper.SourceGenerator;

internal sealed class MapMemberInfo : IEquatable<MapMemberInfo>
{
    public MapMemberInfo(IPropertySymbol property)
    {
        Symbol = property;
        Type = property.Type;
        GetMethod = property.GetMethod;
        SetMethod = property.SetMethod;
    }

    public MapMemberInfo(IFieldSymbol field)
    {
        Symbol = field;
        Type = field.Type;
        IsReadOnly = field.IsReadOnly || field.IsConst;
    }

    public ISymbol Symbol { get; }
    public ITypeSymbol Type { get; }
    public string Name => Symbol.Name;
    public bool IsField => Symbol is IFieldSymbol;
    public bool IsStatic => Symbol.IsStatic;
    public bool IsReadOnly { get; }
    public IMethodSymbol? GetMethod { get; }
    public IMethodSymbol? SetMethod { get; }
    public ImmutableArray<Location> Locations => Symbol.Locations;
    public ImmutableArray<AttributeData> GetAttributes() => Symbol.GetAttributes();

    public override string ToString() => Symbol.ToString();

    public bool Equals(MapMemberInfo? other) =>
        other is not null && SymbolEqualityComparer.Default.Equals(Symbol, other.Symbol);

    public override bool Equals(object? obj) => Equals(obj as MapMemberInfo);

    public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(Symbol);
}
