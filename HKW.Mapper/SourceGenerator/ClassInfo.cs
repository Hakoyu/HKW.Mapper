using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWMapper.SourceGenerator;

internal class ClassInfo
{
    public ClassInfo(
        ClassDeclarationSyntax classSyntax,
        INamedTypeSymbol classSymbol,
        List<AttributeData> attributeDatas
    )
    {
        ClassSyntax = classSyntax;
        ClassSymbol = classSymbol;
        Name = classSymbol.Name;
        Namespace = classSymbol.ContainingNamespace.ToString();

        foreach (var attributeData in attributeDatas)
        {
            var mapTarget = new MapTargetInfo(classSymbol, attributeData);
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
                GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            }
        }
    }

    public string Namespace { get; }
    public string Name { get; }
    public string TypeName => $"{Name}{ClassSyntax.TypeParameterList}";
    public string FullName => $"{Namespace}.{Name}";
    public string FullTypeName => $"{Namespace}.{Name}{ClassSyntax.TypeParameterList}";
    public ClassDeclarationSyntax ClassSyntax { get; }
    public INamedTypeSymbol ClassSymbol { get; private set; }

    public HashSet<MapTargetInfo> MapTargets { get; } = [];

    public List<IPropertySymbol> Properties { get; } = [];

    public List<FieldGenerateInfo> MapConverters { get; } = [];
    public List<FieldGenerateInfo> MapConfigs { get; } = [];

    public void AddConverters(INamedTypeSymbol typeSymbol, out string fieldName)
    {
        var typeFullName = typeSymbol.GetFullName();
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

    /// <summary>
    /// 在生成了映射器专用特性后才能更新, 否则无法解析属性使用的映射器专用特性
    /// </summary>
    /// <param name="classSymbol"></param>
    public void Update(INamedTypeSymbol classSymbol)
    {
        ClassSymbol = classSymbol;
        // 分析所有成员
        foreach (var propertySymbol in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            Properties.Add(propertySymbol);
        }
    }
}

internal class MapTargetInfo : IEquatable<MapTargetInfo>
{
    public const string SourceParamName = "source";
    public const string TargetParamName = "target";

#pragma warning disable CS8618
    public MapTargetInfo(INamedTypeSymbol sourceType, AttributeData attributeData)
#pragma warning restore CS8618
    {
        SourceType = sourceType;

        var ps = attributeData.GetParams();
        if (
            ps.TryGetParam<INamedTypeSymbol>(
                nameof(MapTargetAttribute.TargetType),
                out var targetType
            )
        )
        {
            TargetType = targetType;
            if (TargetType is null)
                IsInvalid = true;
        }

        if (ps.TryGetParam<INamedTypeSymbol>(nameof(MapTargetAttribute.Config), out var configType))
        {
            if (configType.InheritedFrom(TypeFullNames.MapConfigClass) is false)
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.WrongMapConfigType,
                    attributeData?.ApplicationSyntaxReference?.SyntaxTree.GetLocation(
                        attributeData.ApplicationSyntaxReference.Span
                    ),
                    configType.GetName()
                );
                GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
                IsInvalid = true;
                return;
            }
            ConfigInfo = new(configType);
        }

        if (ps.TryGetParam<string>(nameof(MapTargetAttribute.TargetName), out var targetName))
        {
            TargetName = targetName;
        }
        if (string.IsNullOrWhiteSpace(TargetName))
            TargetName = TargetType!.Name;

        if (IsInvalid)
            return;
        // 分析所有成员
        foreach (var property in TargetType!.GetMembers().OfType<IPropertySymbol>())
        {
            PropertyByName.Add(property.Name, property);
        }
        var lowestAccessibility = sourceType.GetLowestAccessibility(targetType);
        // 映射器专用特性
        Attribute = new(PropertyAttributeName, ObjectGenerateType.SealedClass)
        {
            Attributes =
            [
                new("[global::System.AttributeUsage(global::System.AttributeTargets.Property)]"),
            ],
            Accessibility = Accessibility.Internal,
            Comment = $"""
                /// <summary>
                /// Map property attribute
                /// <para>Source: <see cref="{sourceType.GetFullName()}"/></para>
                /// <para>Target: <see cref="{TargetType.GetFullName()}"/></para>
                /// </summary>
                """,
            Inherits = [nameof(System.Attribute)],
            Constructors =
            [
                new(
                    PropertyAttributeName,
                    new ParameterGenerateInfo(
                        GeneratorHelper.StringName,
                        nameof(MapPropertyAttribute.PropertyName)
                    )
                )
                {
                    Accessibility = Accessibility.Public,
                    Comment = "/// <inheritdoc/>",
                    Contents =
                    [
                        $"this.{nameof(MapPropertyAttribute.PropertyName)} = {nameof(MapPropertyAttribute.PropertyName)};",
                    ],
                },
                new(
                    PropertyAttributeName,
                    new ParameterGenerateInfo(
                        TypeFullNames.Type,
                        nameof(MapPropertyAttribute.ConverterType)
                    )
                )
                {
                    Accessibility = Accessibility.Public,
                    Comment = "/// <inheritdoc/>",
                    Contents =
                    [
                        $"this.{nameof(MapPropertyAttribute.ConverterType)} = {nameof(MapPropertyAttribute.ConverterType)};",
                    ],
                },
                new(
                    PropertyAttributeName,
                    new ParameterGenerateInfo(GeneratorHelper.StringName, "PropertyName"),
                    new ParameterGenerateInfo(
                        TypeFullNames.Type,
                        nameof(MapPropertyAttribute.ConverterType)
                    )
                )
                {
                    Accessibility = Accessibility.Public,
                    Comment = "/// <inheritdoc/>",
                    Contents =
                    [
                        $"this.{nameof(MapPropertyAttribute.PropertyName)} = {nameof(MapPropertyAttribute.PropertyName)};",
                        $"this.{nameof(MapPropertyAttribute.ConverterType)} = {nameof(MapPropertyAttribute.ConverterType)};",
                    ],
                },
            ],
        };
        Attribute.Members.Add(
            new PropertyGenerateInfo(
                GeneratorHelper.StringName + "?",
                nameof(MapPropertyAttribute.PropertyName),
                new()
            )
            {
                Accessibility = Accessibility.Public,
            }
        );
        Attribute.Members.Add(
            new PropertyGenerateInfo(
                TypeFullNames.Type + "?",
                nameof(MapPropertyAttribute.ConverterType),
                new()
            )
            {
                Accessibility = Accessibility.Public,
            }
        );
        Attribute.Members.Add(
            new PropertyGenerateInfo(
                GeneratorHelper.BoolName,
                nameof(MapPropertyAttribute.Ignore),
                new()
            )
            {
                Accessibility = Accessibility.Public,
                SetMethod = new(),
            }
        );
        Attribute.Members.Add(
            new PropertyGenerateInfo(
                TypeFullNames.MapPropertyType,
                nameof(MapPropertyAttribute.MapType),
                new()
            )
            {
                Accessibility = Accessibility.Public,
                SetMethod = new(),
            }
        );

        // MapTo扩展方法
        MapToMethod = new(
            ConfigInfo?.MapTo.IsAsync is true
                ? $"async {GeneratorHelper.TaskTypeFullName}"
                : GeneratorHelper.VoidName,
            MapToName,
            string.Empty
        )
        {
            IsStatic = true,
            Accessibility = lowestAccessibility,
            Params =
            [
                new(SourceType, SourceParamName) { GenerateType = ParameterGenerateType.This },
                new(TargetType, TargetParamName),
            ],
        };

        // MapFrom扩展方法
        MapFromMethod = new(
            ConfigInfo?.MapFrom.IsAsync is true
                ? $"async {GeneratorHelper.TaskTypeFullName}"
                : GeneratorHelper.VoidName,
            MapFromName,
            string.Empty
        )
        {
            IsStatic = true,
            Accessibility = lowestAccessibility,
            Params =
            [
                new(SourceType, SourceParamName) { GenerateType = ParameterGenerateType.This },
                new(TargetType, TargetParamName),
            ],
        };
    }

    public INamedTypeSymbol SourceType { get; }
    public string TargetName { get; }
    public string MapToName => "MapTo" + TargetName;
    public string MapFromName => "MapFrom" + TargetName;

    public string PropertyAttributeFullName =>
        $"{SourceType.GetFullName()}MapTarget{TargetName}PropertyAttribute";
    public string PropertyAttributeName =>
        $"{SourceType.GetName()}MapTarget{TargetName}PropertyAttribute";

    public Dictionary<string, IPropertySymbol> PropertyByName { get; } = [];

    public bool IsInvalid { get; }

    public INamedTypeSymbol TargetType { get; }

    public MethodGenerateInfo MapToMethod { get; }
    public MethodGenerateInfo MapFromMethod { get; }

    public ObjectGenerateInfo Attribute { get; }

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

    public MapConfigInfo(INamedTypeSymbol configType)
    {
        Type = configType;
        foreach (var member in configType.GetMembers())
        {
            if (member is IPropertySymbol propertySymbol)
            {
                if (CheckProperty(propertySymbol, out var sourceName, out var targetName) is false)
                    continue;

                Converters.Add(sourceName, (targetName, propertySymbol));
            }
            else if (member is IMethodSymbol methodSymbol)
            {
                CheckMethod(methodSymbol);
            }
        }
    }

    public INamedTypeSymbol Type { get; }
    public Dictionary<string, (string TargetName, IPropertySymbol Property)> Converters { get; } =
    [];
    public MapActionInfo MapTo { get; } = new();
    public MapActionInfo MapFrom { get; } = new();

    private static bool CheckProperty(
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

        var semanticModel = GeneratorHelper.Compilation.GetSemanticModel(propertySyntax.SyntaxTree);
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
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
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

    private static void CheckMapAction(
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
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        if (methodSymbol.DeclaredAccessibility < Accessibility.Internal)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.MapConfigActionInsufficientAccessibility,
                methodSymbol.Locations[0]
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        var aps = att.GetParams();
        if (
            aps.TryGetParam<MapConfigActionMode>(
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
            aps.TryGetParam<string>(nameof(MapToConfigActionAttribute.PropertyName), out var name);
            if (string.IsNullOrEmpty(name))
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.MapConfigActionModePropertyNameError,
                    methodSymbol.Locations[0]
                );
                GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
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
                GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
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
