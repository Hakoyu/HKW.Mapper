using System.Collections.Immutable;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWMapper.SourceGenerator;

internal class ClassInfo
{
    public ClassInfo(
        SourceProductionContext productionContext,
        Compilation compilation,
        ClassDeclarationSyntax classSyntax,
        INamedTypeSymbol classSymbol,
        List<AttributeData> attributeDatas
    )
    {
        ProductionContext = productionContext;
        Compilation = compilation;
        ClassSyntax = classSyntax;
        ClassSymbol = classSymbol;
        Name = classSymbol.Name;
        Namespace = classSymbol.ContainingNamespace.ToString();

        foreach (var attributeData in attributeDatas)
        {
            var mapTarget = new MapTargetInfo(this, classSymbol, attributeData);
            if (mapTarget.IsInvalid)
                continue;
            if (MapTargets.Add(mapTarget) is false)
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.SameMapMethodName,
                    attributeData?.ApplicationSyntaxReference?.SyntaxTree.GetLocation(
                        attributeData.ApplicationSyntaxReference.Span
                    ),
                    mapTarget.TargetName
                );
                ProductionContext.ReportDiagnostic(diagnostic);
            }
        }
        // 分析所有成员

        foreach (
            var propertySymbol in classSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(x => x.DeclaredAccessibility >= Accessibility.Internal)
        )
        {
            if (propertySymbol.IsStatic)
                continue;
            Properties.Add(propertySymbol);
        }
    }

    public Compilation Compilation { get; }
    public SourceProductionContext ProductionContext { get; }
    public string Namespace { get; }
    public string Name { get; }
    public string TypeName => $"{Name}{ClassSyntax.TypeParameterList}";
    public string FullName => $"{Namespace}.{Name}";
    public string FullTypeName => $"{Namespace}.{Name}{ClassSyntax.TypeParameterList}";
    public ClassDeclarationSyntax ClassSyntax { get; }
    public INamedTypeSymbol ClassSymbol { get; private set; }

    public HashSet<MapTargetInfo> MapTargets { get; } = [];

    public HashSet<IPropertySymbol> Properties { get; } = [];

    public List<FieldGenerateInfo> MapConverters { get; } = [];
    public List<FieldGenerateInfo> MapConfigs { get; } = [];

    public void AddConverters(INamedTypeSymbol typeSymbol, out string fieldName)
    {
        var typeFullName = typeSymbol.GetGlobalFullName();
        var baseFieldName = "_" + typeSymbol.GetName().FirstLetterToLower();
        fieldName = baseFieldName;
        var count = 0;
        foreach (var converter in MapConverters)
        {
            if (converter.Name != fieldName)
                continue;
            if (converter.TypeName == typeFullName)
                return;

            fieldName = $"{baseFieldName}_{count++}";
        }
        var field = new FieldGenerateInfo(typeSymbol, fieldName)
        {
            Default = "new()",
            IsStatic = true,
        };
        MapConverters.Add(field);
    }
}

internal class MapTargetInfo : IEquatable<MapTargetInfo>
{
    public const string SourceParamName = "source";
    public const string TargetParamName = "target";

#pragma warning disable CS8618
    public MapTargetInfo(
        ClassInfo classInfo,
        INamedTypeSymbol sourceType,
        AttributeData attributeData
    )
#pragma warning restore CS8618
    {
        SourceType = sourceType;

        var attributeInfo = attributeData.GetInfo()!;
        TargetType = attributeInfo.GetParam<INamedTypeSymbol>(
            nameof(MapTargetAttribute.TargetType)
        );
        if (TargetType is null)
            IsInvalid = true;

        if (
            attributeInfo.TryGetParam<INamedTypeSymbol>(
                nameof(MapTargetAttribute.Config),
                out var configType
            )
        )
        {
            if (configType.InheritedFrom(TypeFullNames.MapConfigClass) is false)
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.MapConfigTypeError,
                    attributeData?.ApplicationSyntaxReference?.SyntaxTree.GetLocation(
                        attributeData.ApplicationSyntaxReference.Span
                    ),
                    configType.GetName()
                );
                classInfo.ProductionContext.ReportDiagnostic(diagnostic);
                IsInvalid = true;
                return;
            }
            if (
                sourceType.SymbolEquals(configType.BaseType!.TypeArguments[0]) is false
                || TargetType?.SymbolEquals(configType.BaseType.TypeArguments[1]) is not true
            )
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.MapConfigGenericTypeError,
                    attributeData?.ApplicationSyntaxReference?.SyntaxTree.GetLocation(
                        attributeData.ApplicationSyntaxReference.Span
                    ),
                    configType.BaseType.TypeArguments[0].GetName(),
                    configType.BaseType.TypeArguments[1].GetName(),
                    sourceType.GetName(),
                    TargetType?.GetName()
                );
                classInfo.ProductionContext.ReportDiagnostic(diagnostic);
                IsInvalid = true;
                return;
            }

            ConfigInfo = new(classInfo, configType);
        }

        if (
            attributeInfo.TryGetParam<string>(
                nameof(MapTargetAttribute.TargetName),
                out var targetName
            )
        )
        {
            TargetName = targetName;
        }
        if (string.IsNullOrWhiteSpace(TargetName))
            TargetName = TargetType!.Name;
        if (
            attributeInfo.TryGetParam<MapDirections>(
                nameof(MapTargetAttribute.Direction),
                out var direction
            )
        )
            Direction = direction;
        else
            Direction = MapDirections.Both;

        if (IsInvalid)
            return;
        var resolvedTargetType = TargetType!;
        // 分析所有成员，派生类型中隐藏的属性优先于基类属性
        for (var type = resolvedTargetType; type is not null; type = type.BaseType)
        {
            foreach (
                var property in type.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(x => x.DeclaredAccessibility >= Accessibility.Internal)
            )
            {
                PropertyByName.TryAdd(property.Name, property);
            }
        }
        var lowestAccessibility = sourceType.GetLowestAccessibility(resolvedTargetType);
        // MapTo扩展方法
        MapToMethod = new(
            ConfigInfo?.MapTo.IsAsync is true
                ? $"async {GeneratorHelper.TaskTypeFullName}<{resolvedTargetType.GetGlobalFullName()}>"
                : resolvedTargetType.GetGlobalFullName(),
            MapToName,
            string.Empty
        )
        {
            IsStatic = true,
            Accessibility = lowestAccessibility,
            Params =
            [
                new(SourceType, SourceParamName) { GenerateType = ParameterGenerateType.This },
                new(resolvedTargetType, TargetParamName),
            ],
        };

        // MapFrom扩展方法
        MapFromMethod = new(
            ConfigInfo?.MapFrom.IsAsync is true
                ? $"async {GeneratorHelper.TaskTypeFullName}<{SourceType.GetGlobalFullName()}>"
                : SourceType.GetGlobalFullName(),
            MapFromName,
            string.Empty
        )
        {
            IsStatic = true,
            Accessibility = lowestAccessibility,
            Params =
            [
                new(SourceType, SourceParamName) { GenerateType = ParameterGenerateType.This },
                new(resolvedTargetType, TargetParamName),
            ],
        };
    }

    public INamedTypeSymbol SourceType { get; }
    public string TargetName { get; }
    public string MapToName =>
        $"MapTo"
        + $"{(TargetName == TargetType.Name ? "" : TargetName)}"
        + $"{(ConfigInfo?.MapTo.IsAsync is true ? "Async" : "")}";
    public string MapFromName =>
        $"MapFrom"
        + $"{(TargetName == TargetType.Name ? "" : TargetName)}"
        + $"{(ConfigInfo?.MapFrom.IsAsync is true ? "Async" : "")}";

    public Dictionary<string, IPropertySymbol> PropertyByName { get; } = [];

    public bool IsInvalid { get; }

    public INamedTypeSymbol TargetType { get; }
    public MapDirections Direction { get; } = MapDirections.Both;

    public MethodGenerateInfo MapToMethod { get; }
    public MethodGenerateInfo MapFromMethod { get; }

    public MapConfigInfo? ConfigInfo { get; }

    #region IEquatable
    public override int GetHashCode()
    {
        return TargetName.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as MapTargetInfo);
    }

    public bool Equals(MapTargetInfo? other)
    {
        if (other is null)
            return false;
        return TargetName == other.TargetName;
    }
    #endregion
}

internal sealed class MapConfigInfo
{
    public const string ConfigName = "__config__";

    public MapConfigInfo(ClassInfo classInfo, INamedTypeSymbol configType)
    {
        _classInfo = classInfo;
        Type = configType;
        foreach (var member in configType.GetMembers())
        {
            if (member is IPropertySymbol propertySymbol)
            {
                if (CheckProperty(propertySymbol, out var sourceName, out var targetName))
                {
                    Converters.Add(sourceName, (targetName, propertySymbol));
                }
                else if (
                    CheckCompositeProperty(propertySymbol, out sourceName, out var targetNames)
                )
                {
                    CompositeConverters.Add(sourceName, (targetNames, propertySymbol));
                }
            }
            else if (member is IMethodSymbol methodSymbol)
            {
                CheckMethod(methodSymbol);
            }
        }
    }

    private readonly ClassInfo _classInfo;

    public INamedTypeSymbol Type { get; }
    public Dictionary<string, (string TargetName, IPropertySymbol Property)> Converters { get; } =
    [];
    public Dictionary<
        string,
        (string[] TargetNames, IPropertySymbol Property)
    > CompositeConverters { get; } = [];
    public MapActionInfo MapTo { get; } = new();
    public MapActionInfo MapFrom { get; } = new();

    private bool CheckProperty(
        IPropertySymbol propertySymbol,
        out string sourceName,
        out string targetName
    )
    {
        // 获取转换器属性并检查
        sourceName = targetName = string.Empty;
        if (propertySymbol.Type.InheritedFrom(TypeFullNames.MapConfigPropertyConverter) is false)
            return false;
        if (propertySymbol.DeclaringSyntaxReferences.Length < 1)
            return false;
        var syntaxReference = propertySymbol.DeclaringSyntaxReferences[0];
        if (syntaxReference.GetSyntax() is not PropertyDeclarationSyntax propertySyntax)
            return false;

        var semanticModel = _classInfo.Compilation.GetSemanticModel(propertySyntax.SyntaxTree);
        // 获取属性初始化器来解析参数
        if (
            propertySyntax.Initializer?.Value is not ImplicitObjectCreationExpressionSyntax creation
        )
            return false;

        var arguments = creation.ArgumentList?.Arguments;

        if (arguments is null)
            return false;
        if (arguments.Value.Count == 3)
        {
            // 只有3个参数, 则源属性和目标属性同名
            sourceName = arguments.Value[0].Expression.GetConstantString(semanticModel)!;
            targetName = sourceName;
        }
        else if (arguments.Value.Count == 4)
        {
            sourceName = arguments.Value[0].Expression.GetConstantString(semanticModel)!;
            targetName = arguments.Value[1].Expression.GetConstantString(semanticModel)!;
        }
        if (string.IsNullOrWhiteSpace(sourceName) || string.IsNullOrWhiteSpace(targetName))
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.MapConfigPropertyConverterPropertyNameError,
                propertySymbol.Locations[0]
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return false;
        }

        return true;
    }

    private bool CheckCompositeProperty(
        IPropertySymbol propertySymbol,
        out string sourceName,
        out string[] targetNames
    )
    {
        sourceName = string.Empty;
        targetNames = [];
        if (
            propertySymbol.Type.InheritedFrom(TypeFullNames.MapConfigCompositePropertyConverter)
            is false
        )
            return false;
        if (
            propertySymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
            is not PropertyDeclarationSyntax propertySyntax
        )
            return false;

        var argumentList = propertySyntax.Initializer?.Value switch
        {
            ImplicitObjectCreationExpressionSyntax creation => creation.ArgumentList,
            ObjectCreationExpressionSyntax creation => creation.ArgumentList,
            _ => null,
        };
        if (argumentList?.Arguments.Count != 4)
        {
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.MapConfigCompositeInitializerError,
                    propertySymbol.Locations[0]
                )
            );
            return false;
        }

        var semanticModel = _classInfo.Compilation.GetSemanticModel(propertySyntax.SyntaxTree);
        sourceName = argumentList.Arguments[0].Expression.GetConstantString(semanticModel)!;
        var targetExpression = argumentList.Arguments[1].Expression;
        IEnumerable<ExpressionSyntax>? targetExpressions = targetExpression switch
        {
            CollectionExpressionSyntax collection => collection
                .Elements.OfType<ExpressionElementSyntax>()
                .Select(x => x.Expression),
            ArrayCreationExpressionSyntax array => array.Initializer?.Expressions,
            ImplicitArrayCreationExpressionSyntax array => array.Initializer.Expressions,
            _ => null,
        };
        if (targetExpressions is null)
        {
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.MapConfigCompositeInitializerError,
                    propertySymbol.Locations[0]
                )
            );
            return false;
        }

        targetNames = targetExpressions
            .Select(x => x.GetConstantString(semanticModel) ?? string.Empty)
            .ToArray();
        if (
            string.IsNullOrWhiteSpace(sourceName)
            || targetNames.Length < 2
            || targetNames.Any(string.IsNullOrWhiteSpace)
            || targetNames.Distinct(StringComparer.Ordinal).Count() != targetNames.Length
        )
        {
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.MapConfigCompositePropertyNameError,
                    propertySymbol.Locations[0]
                )
            );
            return false;
        }
        return true;
    }

    private void CheckMethod(IMethodSymbol methodSymbol)
    {
        var mapToAtt = methodSymbol.GetFirstAttribute(TypeFullNames.MapToConfigActionAttribute);
        if (mapToAtt is not null)
            CheckMapAction(methodSymbol, mapToAtt, MapTo);

        var mapFromAtt = methodSymbol.GetFirstAttribute(TypeFullNames.MapFromConfigActionAttribute);
        if (mapFromAtt is not null)
            CheckMapAction(methodSymbol, mapFromAtt, MapFrom);
    }

    private void CheckMapAction(
        IMethodSymbol methodSymbol,
        AttributeData att,
        MapActionInfo actionInfo
    )
    {
        if (methodSymbol.Parameters.Length > 0)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.MapConfigActionCannotHaveParameters,
                methodSymbol.Locations[0]
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        if (methodSymbol.DeclaredAccessibility < Accessibility.Internal)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.MapConfigActionInsufficientAccessibility,
                methodSymbol.Locations[0]
            );
            _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        var attributeInfo = att.GetInfo()!;
        if (
            attributeInfo.TryGetParam<MapConfigActionMode>(
                nameof(MapToConfigActionAttribute.Mode),
                out var mode
            )
            is false
        )
            return;

        if (mode is MapConfigActionMode.Start)
            actionInfo.StartActions.Add(methodSymbol);
        else if (mode is MapConfigActionMode.End)
            actionInfo.EndActions.Add(methodSymbol);
        else
        {
            attributeInfo.TryGetParam<string>(
                nameof(MapToConfigActionAttribute.PropertyName),
                out var name
            );
            if (string.IsNullOrEmpty(name))
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.MapConfigActionModePropertyNameError,
                    methodSymbol.Locations[0]
                );
                _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
                return;
            }
            var result = mode switch
            {
                MapConfigActionMode.BeforeProperty => actionInfo.BeforePropertyActions.TryAdd(
                    name,
                    methodSymbol
                ),
                MapConfigActionMode.AfterProperty => actionInfo.AfterPropertyActions.TryAdd(
                    name,
                    methodSymbol
                ),
                MapConfigActionMode.ReplaceProperty => actionInfo.ReplacePropertyActions.TryAdd(
                    name,
                    methodSymbol
                ),
                _ => true,
            };
            if (result is false)
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.MapConfigActionHasSameProperty,
                    methodSymbol.Locations[0]
                );
                _classInfo.ProductionContext.ReportDiagnostic(diagnostic);
            }
        }

        if (actionInfo.IsAsync is false)
        {
            actionInfo.IsAsync = methodSymbol.ReturnType.InheritedFrom(
                GeneratorHelper.TaskTypeFullName
            );
        }
    }
}

internal class MapActionInfo
{
    public bool IsAsync { get; set; }
    public List<IMethodSymbol> StartActions { get; } = [];
    public List<IMethodSymbol> EndActions { get; } = [];
    public Dictionary<string, IMethodSymbol> BeforePropertyActions { get; } = [];
    public Dictionary<string, IMethodSymbol> AfterPropertyActions { get; } = [];
    public Dictionary<string, IMethodSymbol> ReplacePropertyActions { get; } = [];
}

internal readonly struct PropertyPair(string sourceProperty, string targetProperty)
    : IEquatable<PropertyPair>
{
    public readonly string SourceProperty { get; } = sourceProperty;
    public readonly string TargetProperty { get; } = targetProperty;

    #region IEquatable
    public override int GetHashCode()
    {
        return HashCodeHelper.Combine(SourceProperty, TargetProperty);
    }

    public override bool Equals(object obj)
    {
        return Equals((PropertyPair)obj);
    }

    public bool Equals(PropertyPair other)
    {
        return SourceProperty == other.SourceProperty && TargetProperty == other.TargetProperty;
    }
    #endregion
}
