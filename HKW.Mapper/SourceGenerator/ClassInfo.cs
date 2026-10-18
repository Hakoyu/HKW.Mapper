using System.Collections.Immutable;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.Mapper.SourceGenerator;

internal class ClassInfo
{
    public ClassInfo(
        SourceProductionContext productionContext,
        Compilation compilation,
        TypeDeclarationSyntax? classSyntax,
        INamedTypeSymbol classSymbol,
        List<AttributeData> attributeDatas,
        INamedTypeSymbol? outputContainerSymbol = null,
        TypeDeclarationSyntax? outputContainerSyntax = null
    )
    {
        ProductionContext = productionContext;
        Compilation = compilation;
        ClassSyntax = classSyntax;
        ClassSymbol = classSymbol;
        OutputContainerSymbol = outputContainerSymbol;
        OutputContainerSyntax = outputContainerSyntax;
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
                    Descriptors.DuplicateMapMethodName,
                    attributeData?.ApplicationSyntaxReference?.SyntaxTree.GetLocation(
                        attributeData.ApplicationSyntaxReference.Span
                    ),
                    mapTarget.TargetName
                );
                ProductionContext.ReportDiagnostic(diagnostic);
            }
        }
        ResolveMapConstructors();
        // 分析所有成员
        foreach (
            var propertySymbol in classSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(IsMappableMember)
        )
        {
            if (propertySymbol.IsStatic)
                continue;
            Properties.Add(new(propertySymbol));
        }
        foreach (
            var fieldSymbol in classSymbol
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(IsMappableField)
        )
            Fields.Add(new(fieldSymbol));

        if (MapTargets.Any(x => x.MapParentProperties || x.MapParentFields))
        {
            var memberNames = new HashSet<string>(
                Properties.Concat(Fields).Select(x => x.Name),
                StringComparer.Ordinal
            );
            for (var type = classSymbol.BaseType; type is not null; type = type.BaseType)
            {
                if (MapTargets.Any(x => x.MapParentProperties))
                {
                    foreach (
                        var propertySymbol in type.GetMembers()
                            .OfType<IPropertySymbol>()
                            .Where(IsMappableMember)
                    )
                    {
                        if (
                            propertySymbol.IsStatic || memberNames.Add(propertySymbol.Name) is false
                        )
                            continue;
                        ParentProperties.Add(new(propertySymbol));
                    }
                }
                if (MapTargets.Any(x => x.MapParentFields))
                {
                    foreach (
                        var fieldSymbol in type.GetMembers()
                            .OfType<IFieldSymbol>()
                            .Where(IsMappableField)
                    )
                    {
                        if (memberNames.Add(fieldSymbol.Name) is false)
                            continue;
                        ParentFields.Add(new(fieldSymbol));
                    }
                }
            }
        }
    }

    public Compilation Compilation { get; }
    public SourceProductionContext ProductionContext { get; }
    public string Namespace { get; }
    public string Name { get; }
    public string TypeName => ClassSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    public string FullName => ClassSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    public string FullTypeName => FullName;
    public TypeDeclarationSyntax? ClassSyntax { get; }
    public INamedTypeSymbol ClassSymbol { get; }
    public INamedTypeSymbol? OutputContainerSymbol { get; }
    public TypeDeclarationSyntax? OutputContainerSyntax { get; }
    public bool UsesExternalContainer => OutputContainerSymbol is not null;
    public string ExtensionClassFullName => UsesExternalContainer
        ? OutputContainerSymbol!.GetGlobalFullName()
        : $"global::HKW.Mapper.{ClassSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat).Replace('.', '_')}MapExtensions";

    public HashSet<MapTargetInfo> MapTargets { get; } = [];

    public HashSet<MapMemberInfo> Properties { get; } = [];
    public HashSet<MapMemberInfo> ParentProperties { get; } = [];
    public HashSet<MapMemberInfo> Fields { get; } = [];
    public HashSet<MapMemberInfo> ParentFields { get; } = [];

    public List<FieldGenerateInfo> MapConverters { get; } = [];
    public List<FieldGenerateInfo> MapConfigs { get; } = [];

    internal bool IsMappableMember(ISymbol member) =>
        UsesExternalContainer
            ? Compilation.IsSymbolAccessibleWithin(member, OutputContainerSymbol!)
            : member.DeclaredAccessibility >= Accessibility.Internal;

    private bool IsMappableField(IFieldSymbol field) =>
        field.IsStatic is false
        && field.IsConst is false
        && field.IsImplicitlyDeclared is false
        && IsMappableMember(field);

    private void ResolveMapConstructors()
    {
        var constructorAttributeName = UsesExternalContainer
            ? TypeFullNames.MapSourceConstructorAttribute
            : TypeFullNames.MapConstructorAttribute;
        var attributes = (UsesExternalContainer ? OutputContainerSymbol! : ClassSymbol)
            .GetAttributes()
            .Where(x => x.AttributeClass?.IsName(constructorAttributeName) is true);
        if (UsesExternalContainer)
        {
            attributes = attributes.Where(x =>
                x.ConstructorArguments[0].Value is INamedTypeSymbol sourceType
                && ClassSymbol.SymbolEquals(sourceType)
            );
        }

        foreach (var attribute in attributes)
        {
            var location =
                attribute.ApplicationSyntaxReference?.SyntaxTree.GetLocation(
                    attribute.ApplicationSyntaxReference.Span
                ) ?? Location.None;
            var selectorIndex = UsesExternalContainer ? 1 : 0;
            var parameterTypesIndex = UsesExternalContainer ? 2 : 1;
            var selector = attribute.ConstructorArguments[selectorIndex];
            var target = selector.Value is INamedTypeSymbol targetType
                ? MapTargets.FirstOrDefault(x => x.TargetType.SymbolEquals(targetType))
                : MapTargets.FirstOrDefault(x => x.TargetName == selector.Value as string);
            if (target is null)
            {
                ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        UsesExternalContainer
                            ? Descriptors.MapSourceConstructorTargetNotFound
                            : Descriptors.MapConstructorTargetNotFound,
                        location,
                        selector.Value?.ToString() ?? string.Empty,
                        ClassSymbol.GetName()
                    )
                );
                continue;
            }

            var parameterTypes =
                attribute.ConstructorArguments.Length > parameterTypesIndex
                    ? attribute
                        .ConstructorArguments[parameterTypesIndex]
                        .Values.Select(x => x.Value as ITypeSymbol)
                        .Where(x => x is not null)
                        .Cast<ITypeSymbol>()
                        .ToArray()
                    : [];
            var constructors = target
                .TargetType.InstanceConstructors.Where(x =>
                    x.Parameters.Length == parameterTypes.Length
                    && x.Parameters.Select(p => p.Type)
                        .Zip(parameterTypes, (left, right) => left.SymbolEquals(right))
                        .All(equal => equal)
                )
                .ToArray();
            if (constructors.Length == 0)
            {
                ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.MapConstructorNotFound,
                        location,
                        target.TargetType.GetName(),
                        string.Join(", ", parameterTypes.Select(x => x.GetName()))
                    )
                );
                continue;
            }

            var constructor = constructors[0];
            if (target.TargetType.IsAbstract || IsAccessible(constructor) is false)
            {
                ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.MapConstructorInaccessible,
                        location,
                        target.TargetType.GetName()
                    )
                );
                continue;
            }
            if (target.AddConstructor(constructor, isAutomatic: false, location) is false)
            {
                ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.DuplicateMapConstructor,
                        location,
                        string.Join(", ", parameterTypes.Select(x => x.GetName())),
                        target.TargetName
                    )
                );
            }
        }

        foreach (var target in MapTargets)
        {
            if (target.TargetType.IsAbstract)
                continue;
            var parameterless = target.TargetType.InstanceConstructors.FirstOrDefault(x =>
                x.Parameters.Length == 0 && IsAccessible(x)
            );
            if (parameterless is not null)
                target.AddConstructor(parameterless, isAutomatic: true, target.MapTargetLocation);
        }
    }

    private bool IsAccessible(ISymbol symbol) =>
        symbol.DeclaredAccessibility == Accessibility.Public
        || symbol.DeclaredAccessibility >= Accessibility.Internal
            && symbol.ContainingAssembly.SymbolEquals(Compilation.Assembly);

    public void AddConverters(INamedTypeSymbol typeSymbol, out string fieldName)
    {
        var typeFullName = typeSymbol.GetGlobalFullName();
        var baseFieldName =
            "_"
            + (UsesExternalContainer ? ClassSymbol.Name.FirstLetterToLower() + "_" : string.Empty)
            + typeSymbol.Name.FirstLetterToLower();
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

#pragma warning disable CS8618, S3776
    public MapTargetInfo(
        ClassInfo classInfo,
        INamedTypeSymbol sourceType,
        AttributeData attributeData
    )
#pragma warning restore CS8618, S3776
    {
        ClassInfo = classInfo;
        SourceType = sourceType;
        MapTargetLocation =
            attributeData.ApplicationSyntaxReference?.SyntaxTree.GetLocation(
                attributeData.ApplicationSyntaxReference.Span
            ) ?? Location.None;
        var attributeInfo = attributeData.GetInfo()!;

        if (
            attributeInfo.TryGetParam<INamedTypeSymbol>(
                nameof(MapTargetAttribute.TargetType),
                out var targetType
            )
        )
        {
            TargetType = targetType;
        }
        else
        {
            IsInvalid = true;
            return;
        }

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
                    Descriptors.InvalidMapConfigType,
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
                    Descriptors.MapConfigGenericTypeMismatch,
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

        TargetName = attributeInfo.GetParam<string>(nameof(MapTargetAttribute.TargetName));
        if (string.IsNullOrWhiteSpace(TargetName))
            TargetName = TargetType.Name;

        if (IsInvalid)
            return;

        if (
            attributeInfo.TryGetParam<MapDirections>(
                nameof(MapTargetAttribute.Direction),
                out var direction
            )
        )
            Direction = direction;

        if (
            SourceType.IsValueType
            && SourceType.IsReadOnly
            && Direction.HasFlag(MapDirections.From)
        )
        {
            classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.ReadOnlyStructCannotMapFrom,
                    MapTargetLocation,
                    SourceType.GetName()
                )
            );
            Direction &= ~MapDirections.From;
        }

        if (
            attributeInfo.TryGetParam<bool>(nameof(MapTargetAttribute.MapFields), out var mapFields)
        )
            MapFields = mapFields;
        if (
            attributeInfo.TryGetParam<bool>(
                nameof(MapTargetAttribute.MapParentProperties),
                out var mapParentProperties
            )
        )
            MapParentProperties = mapParentProperties;
        if (
            attributeInfo.TryGetParam<bool>(
                nameof(MapTargetAttribute.MapParentFields),
                out var mapParentFields
            )
        )
            MapParentFields = mapParentFields;

        if (
            attributeInfo.TryGetParams<string>(
                nameof(MapTargetAttribute.IgnoreProperties),
                out var ignoreProperties
            )
        )
        {
            AttributeIgnoreProperties.UnionWith(ignoreProperties);
            IgnoreProperties.UnionWith(ignoreProperties);
        }
        if (
            attributeInfo.TryGetParams<string>(
                nameof(MapTargetAttribute.IgnoreFields),
                out var ignoreFields
            )
        )
        {
            AttributeIgnoreFields.UnionWith(ignoreFields);
            IgnoreFields.UnionWith(ignoreFields);
        }

        if (ConfigInfo is not null)
        {
            IgnoreProperties.UnionWith(ConfigInfo.IgnoreProperties);
            IgnoreFields.UnionWith(ConfigInfo.IgnoreFields);
        }

        var resolvedTargetType = TargetType;
        // 属性优先于同名字段，派生类型成员优先于基类成员。
        for (var type = resolvedTargetType; type is not null; type = type.BaseType)
        {
            foreach (
                var property in type.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(classInfo.IsMappableMember)
            )
                MemberByName.TryAdd(property.Name, new(property));
        }
        if (MapFields)
        {
            for (var type = resolvedTargetType; type is not null; type = type.BaseType)
            {
                foreach (
                    var field in type.GetMembers()
                        .OfType<IFieldSymbol>()
                        .Where(x =>
                            x.IsStatic is false
                            && x.IsConst is false
                            && x.IsImplicitlyDeclared is false
                            && classInfo.IsMappableMember(x)
                        )
                )
                    MemberByName.TryAdd(field.Name, new(field));
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
                SourceType.IsValueType
                    ? new($"ref {SourceType.GetGlobalFullName()}", SourceParamName)
                    {
                        GenerateType = ParameterGenerateType.This,
                    }
                    : new(SourceType, SourceParamName)
                    {
                        GenerateType = ParameterGenerateType.This,
                    },
                new(resolvedTargetType, TargetParamName),
            ],
        };
    }

    public ClassInfo ClassInfo { get; }
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

    public Dictionary<string, MapMemberInfo> MemberByName { get; } = [];
    public HashSet<string> IgnoreProperties { get; } = new(StringComparer.Ordinal);
    public HashSet<string> IgnoreFields { get; } = new(StringComparer.Ordinal);
    public HashSet<string> AttributeIgnoreProperties { get; } = new(StringComparer.Ordinal);
    public HashSet<string> AttributeIgnoreFields { get; } = new(StringComparer.Ordinal);
    public Location MapTargetLocation { get; }

    public bool IsInvalid { get; }

    public INamedTypeSymbol TargetType { get; }
    public MapDirections Direction { get; private set; } = MapDirections.Both;
    public bool MapFields { get; }
    public bool MapParentProperties { get; }
    public bool MapParentFields { get; }

    public MethodGenerateInfo MapToMethod { get; }
    public MethodGenerateInfo MapFromMethod { get; }
    public List<MethodGenerateInfo> MapToCreationMethods { get; } = [];
    public List<MapConstructorInfo> Constructors { get; } = [];
    public Dictionary<string, string> CreationInitializers { get; } = new(StringComparer.Ordinal);
    public HashSet<string> IgnoredRequiredTargetMembers { get; } = new(StringComparer.Ordinal);

    public MapConfigInfo? ConfigInfo { get; }

    public bool AddConstructor(IMethodSymbol constructor, bool isAutomatic, Location location)
    {
        if (
            Constructors.Any(existing =>
                existing.Constructor.Parameters.Length == constructor.Parameters.Length
                && existing
                    .Constructor.Parameters.Select(x => x.Type)
                    .Zip(
                        constructor.Parameters.Select(x => x.Type),
                        (left, right) => left.SymbolEquals(right)
                    )
                    .All(equal => equal)
            )
        )
            return false;
        Constructors.Add(new(constructor, isAutomatic, location));
        return true;
    }

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

internal sealed class MapConstructorInfo(
    IMethodSymbol constructor,
    bool isAutomatic,
    Location location
)
{
    public IMethodSymbol Constructor { get; } = constructor;
    public bool IsAutomatic { get; } = isAutomatic;
    public Location Location { get; } = location;
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
                if (CheckIgnoredMembers(propertySymbol))
                    continue;
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
    public Dictionary<string, (string TargetName, IPropertySymbol Property)> Converters { get; } = [];
    public Dictionary<
        string,
        (string[] TargetNames, IPropertySymbol Property)
    > CompositeConverters { get; } = [];
    public MapActionInfo MapTo { get; } = new();
    public MapActionInfo MapFrom { get; } = new();
    public HashSet<string> IgnoreProperties { get; } = new(StringComparer.Ordinal);
    public HashSet<string> IgnoreFields { get; } = new(StringComparer.Ordinal);
    public Location IgnorePropertiesLocation { get; private set; } = Location.None;
    public Location IgnoreFieldsLocation { get; private set; } = Location.None;

    private bool CheckIgnoredMembers(IPropertySymbol propertySymbol)
    {
        var destination = propertySymbol.Name switch
        {
            nameof(MapTargetAttribute.IgnoreProperties) => IgnoreProperties,
            nameof(MapTargetAttribute.IgnoreFields) => IgnoreFields,
            _ => null,
        };
        if (
            destination is null
            || propertySymbol.Type
                is not IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_String }
            || propertySymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
                is not PropertyDeclarationSyntax propertySyntax
        )
            return false;

        if (propertySymbol.Name == nameof(MapTargetAttribute.IgnoreProperties))
            IgnorePropertiesLocation = propertySyntax.GetLocation();
        else
            IgnoreFieldsLocation = propertySyntax.GetLocation();

        IEnumerable<ExpressionSyntax>? expressions = propertySyntax.Initializer?.Value switch
        {
            CollectionExpressionSyntax collection => collection
                .Elements.OfType<ExpressionElementSyntax>()
                .Select(x => x.Expression),
            ArrayCreationExpressionSyntax array => array.Initializer?.Expressions,
            ImplicitArrayCreationExpressionSyntax array => array.Initializer.Expressions,
            _ => null,
        };
        if (expressions is null)
        {
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.MapConfigIgnoreInitializerCannotBeAnalyzed,
                    propertySyntax.GetLocation(),
                    propertySymbol.Name
                )
            );
            return true;
        }

        var semanticModel = _classInfo.Compilation.GetSemanticModel(propertySyntax.SyntaxTree);
        var reportedUnanalyzableInitializer = false;
        foreach (var expression in expressions)
        {
            var propertyName = expression.GetConstantString(semanticModel);
            if (string.IsNullOrWhiteSpace(propertyName) is false)
            {
                destination.Add(propertyName!);
                continue;
            }

            if (reportedUnanalyzableInitializer)
                continue;
            _classInfo.ProductionContext.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.MapConfigIgnoreInitializerCannotBeAnalyzed,
                    expression.GetLocation(),
                    propertySymbol.Name
                )
            );
            reportedUnanalyzableInitializer = true;
        }
        return true;
    }

    private bool CheckProperty(
        IPropertySymbol propertySymbol,
        out string sourceName,
        out string targetName
    )
    {
        // 获取转换器属性并检查
        sourceName = targetName = string.Empty;
        if (propertySymbol.Type.Name != nameof(MapConfigMemberConverter<object, object>))
            return false;
        if (propertySymbol.DeclaringSyntaxReferences.Length < 1)
            return false;
        var syntaxReference = propertySymbol.DeclaringSyntaxReferences[0];
        if (syntaxReference.GetSyntax() is not PropertyDeclarationSyntax propertySyntax)
            return false;

        var semanticModel = _classInfo.Compilation.GetSemanticModel(propertySyntax.SyntaxTree);
        // 获取属性初始化器来解析参数
        var argumentList = propertySyntax.Initializer?.Value switch
        {
            ImplicitObjectCreationExpressionSyntax creation => creation.ArgumentList,
            ObjectCreationExpressionSyntax creation => creation.ArgumentList,
            _ => null,
        };
        var arguments = argumentList?.Arguments;

        if (arguments is null)
            return false;
        if (arguments.Value.Count == 3)
        {
            // 只有3个参数, 则源成员和目标成员同名
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
                Descriptors.InvalidMapConfigConverterMemberName,
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
            propertySymbol.Type.Name
            != nameof(MapConfigCompositeMemberConverter<object, ValueTuple>)
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
                    Descriptors.MapConfigCompositeInitializerCannotBeAnalyzed,
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
                    Descriptors.MapConfigCompositeInitializerCannotBeAnalyzed,
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
                    Descriptors.InvalidMapConfigCompositeMemberNames,
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
                Descriptors.MapConfigActionMethodInaccessible,
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
                    Descriptors.InvalidMapConfigActionMemberName,
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
                    Descriptors.DuplicateMapConfigActionForMember,
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
