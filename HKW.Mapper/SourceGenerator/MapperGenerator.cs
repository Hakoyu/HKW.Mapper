using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWMapper.SourceGenerator;

internal class MapperGenerator
{
    public static MapperGenerator Generate(
        ClassInfo classInfo,
        IReadOnlyList<MapTargetInfo> mapTargets
    )
    {
        var x = new MapperGenerator(classInfo, mapTargets);
        x.Execute();
        return x;
    }

    public MapperGenerator(ClassInfo classInfo, IReadOnlyList<MapTargetInfo> mapTargets)
    {
        ClassInfo = classInfo;
        AllMapTargets = mapTargets;
    }

    public ClassInfo ClassInfo { get; }
    public IReadOnlyList<MapTargetInfo> AllMapTargets { get; }

    public INamedTypeSymbol MapConverterType { get; private set; } = null!;
    public INamedTypeSymbol MapConfigType { get; private set; } = null!;

    public void Execute()
    {
        foreach (var target in ClassInfo.MapTargets)
        {
            TryAddConfig(target);
            AddStartAction(target);
            foreach (var property in ClassInfo.Properties)
            {
                ParseProperty(target, property);
            }
            AddEndAction(target);
            if (target.Direction.HasFlag(MapDirections.To))
                target.MapToMethod.Contents.Add($"return {MapTargetInfo.TargetParamName};");
            if (target.Direction.HasFlag(MapDirections.From))
                target.MapFromMethod.Contents.Add($"return {MapTargetInfo.SourceParamName};");
        }
    }

    private void ParseProperty(MapTargetInfo mapTarget, IPropertySymbol propertySymbol)
    {
        var atts = propertySymbol.GetAttributes();
        // 检查全局忽略属性
        if (
            atts.Any(a =>
                a.AttributeClass?.GetFullName() == TypeFullNames.MapIgnorePropertyAttribute
            )
        )
            return;
        var propertyAttributes = atts.Where(a =>
                a.AttributeClass?.GetFullName() == TypeFullNames.MapPropertyAttribute
            )
            .ToArray();
        // 获取当前映射目标有效的特性
        var matchingAttributes = propertyAttributes.Where(a => IsForTarget(a, mapTarget)).ToArray();
        if (propertyAttributes.Length > 0 && matchingAttributes.Length == 0)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.MapPropertyTargetNotFound,
                propertySymbol.Locations[0],
                GetMapPropertyTargetText(propertyAttributes[0]),
                mapTarget.SourceType.GetName()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
        }
        if (matchingAttributes.Length > 1)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.MapPropertyTargetAmbiguous,
                propertySymbol.Locations[0],
                propertySymbol.Name
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
        }
        var attributeData = matchingAttributes.FirstOrDefault();

        var attributeInfo = attributeData is null ? null : new AttributeInfo(attributeData);
        // 指定特性忽略
        if (
            attributeInfo?.TryGetParam<bool>(nameof(MapPropertyAttribute.Ignore), out var ignore)
                is true
            && ignore is true
        )
            return;

        if (
            attributeInfo?.TryGetParam<string>(
                nameof(MapPropertyAttribute.PropertyName),
                out var targetPropertyName
            )
            is not true
        )
            targetPropertyName = propertySymbol.Name;

        var targetProperty = GetTargetProperty(mapTarget, propertySymbol, targetPropertyName);
        if (targetProperty is null)
            return;
        AddBeforePropertyAction(mapTarget, propertySymbol);
        if (
            TryUseConfigPropertyConverter(mapTarget, propertySymbol, targetProperty) is false
            && TryUsePropertyConverter(mapTarget, propertySymbol, attributeInfo, targetProperty)
                is false
        )
        {
            MapProperty(mapTarget, propertySymbol, attributeInfo, targetProperty);
        }
        TryReplacePropertyAction(mapTarget, propertySymbol);
        AddAfterPropertyAction(mapTarget, propertySymbol);
    }

    private static bool IsForTarget(AttributeData attribute, MapTargetInfo mapTarget)
    {
        if (attribute.ConstructorArguments.Length == 0)
            return false;

        var argument = attribute.ConstructorArguments[0];
        if (argument.Kind is TypedConstantKind.Type)
            return argument.Value is INamedTypeSymbol type
                && type.SymbolEquals(mapTarget.TargetType);
        if (argument.Kind is TypedConstantKind.Primitive)
            return string.Equals(
                argument.Value?.ToString(),
                mapTarget.TargetName,
                StringComparison.Ordinal
            );
        return false;
    }

    private static string GetMapPropertyTargetText(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
            return "<missing>";
        var argument = attribute.ConstructorArguments[0];
        return argument.Kind is TypedConstantKind.Type
            ? argument.Value is INamedTypeSymbol type
                ? type.GetFullName()
                : "<invalid>"
            : argument.Value?.ToString() ?? "<invalid>";
    }

    private static void TryReplacePropertyAction(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol
    )
    {
        if (mapTarget.ConfigInfo is null)
            return;
        if (
            mapTarget.ConfigInfo.MapTo.ReplacePropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapToMethod
            )
        )
        {
            mapTarget.MapToMethod.Contents.RemoveAt(mapTarget.MapToMethod.Contents.Count - 1);
            mapTarget.MapToMethod.Contents.Add($"// Replace {propertySymbol.Name}");
            mapTarget.MapToMethod.Contents.Add(
                mapToMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }

        if (
            mapTarget.ConfigInfo.MapFrom.ReplacePropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapFromMethod
            )
        )
        {
            mapTarget.MapFromMethod.Contents.RemoveAt(mapTarget.MapFromMethod.Contents.Count - 1);
            mapTarget.MapFromMethod.Contents.Add($"// Replace {propertySymbol.Name}");
            mapTarget.MapFromMethod.Contents.Add(
                mapFromMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }

    private static void AddBeforePropertyAction(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol
    )
    {
        if (mapTarget.ConfigInfo is null)
            return;
        if (
            mapTarget.ConfigInfo.MapTo.BeforePropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapToMethod
            )
        )
        {
            mapTarget.MapToMethod.Contents.Add(
                mapToMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }

        if (
            mapTarget.ConfigInfo.MapFrom.BeforePropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapFromMethod
            )
        )
        {
            mapTarget.MapFromMethod.Contents.Add(
                mapFromMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }

    private static void AddAfterPropertyAction(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol
    )
    {
        if (mapTarget.ConfigInfo is null)
            return;
        if (
            mapTarget.ConfigInfo.MapTo.AfterPropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapToMethod
            )
        )
        {
            mapTarget.MapToMethod.Contents.Add(
                mapToMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }

        if (
            mapTarget.ConfigInfo.MapFrom.AfterPropertyActions.TryGetValue(
                propertySymbol.Name,
                out var mapFromMethod
            )
        )
        {
            mapTarget.MapFromMethod.Contents.Add(
                mapFromMethod.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }

    private void MapProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        AttributeInfo? attributeInfo,
        IPropertySymbol targetProperty
    )
    {
        if (
            attributeInfo?.TryGetParam<MapPropertyType>(
                nameof(MapPropertyAttribute.MapType),
                out var mapType
            )
            is not true
        )
            mapType = default;

        if (mapType is MapPropertyType.ConstructFromSelf)
        {
            mapTarget.MapToMethod.Contents.Add(
                $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = new({MapTargetInfo.SourceParamName}.{propertySymbol.Name});"
            );
            mapTarget.MapFromMethod.Contents.Add(
                $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = new({MapTargetInfo.TargetParamName}.{targetProperty.Name});"
            );
            return;
        }

        if (TryMapComplexProperty(mapTarget, propertySymbol, targetProperty))
            return;

        if (TryMapNullableProperty(mapTarget, propertySymbol, targetProperty))
            return;

        // 比较当前属性与目标属性的类型
        if (propertySymbol.Type.SymbolEquals(targetProperty.Type) is false)
        {
            // 如果当前属性类型与目标属性类型不一样, 则异常
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyTypeError,
                propertySymbol.Locations[0],
                targetProperty.ToString()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        if (propertySymbol.Type.InheritedFrom(TypeFullNames.ICloneable))
        {
            // 如果实现了 ICloneable, 则克隆
            mapTarget.MapToMethod.Contents.Add(
                $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = ({propertySymbol.Type.GetFullName()}){MapTargetInfo.SourceParamName}.{propertySymbol.Name}.Clone();"
            );
            mapTarget.MapFromMethod.Contents.Add(
                $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = ({targetProperty.Type.GetFullName()}){MapTargetInfo.TargetParamName}.{targetProperty.Name}.Clone();"
            );
            return;
        }
        if (propertySymbol.Type.IsReferenceType && mapType is not MapPropertyType.Reference)
        {
            // 如果是引用类型, 则错误
            var diagnostic = Diagnostic.Create(
                Descriptors.PropertyIsReferenceType,
                propertySymbol.Locations[0],
                $"{mapTarget.SourceType.Name}.{propertySymbol.Name}"
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }

        // 正常赋值
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = {MapTargetInfo.SourceParamName}.{propertySymbol.Name};"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = {MapTargetInfo.TargetParamName}.{targetProperty.Name};"
        );
    }

    private static bool TryMapNullableProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty
    )
    {
        var sourceType = UnwrapNullable(sourceProperty.Type);
        var targetType = UnwrapNullable(targetProperty.Type);
        if (sourceType.SymbolEquals(targetType) is false)
            return false;

        var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.Name}";
        var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}";
        var sourceNullable = IsNullable(sourceProperty.Type);
        var targetNullable = IsNullable(targetProperty.Type);
        var toExpression = sourceNullable && !targetNullable ? $"{source} ?? default!" : source;
        var fromExpression = targetNullable && !sourceNullable ? $"{target} ?? default!" : target;
        mapTarget.MapToMethod.Contents.Add($"{target} = {toExpression};");
        mapTarget.MapFromMethod.Contents.Add($"{source} = {fromExpression};");
        return sourceNullable || targetNullable;
    }

    private static bool IsNullable(ITypeSymbol type)
    {
        return type.NullableAnnotation == NullableAnnotation.Annotated
            || type is INamedTypeSymbol named
                && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    private bool TryMapComplexProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty
    )
    {
        var sourceType = UnwrapNullable(sourceProperty.Type);
        var targetType = UnwrapNullable(targetProperty.Type);

        if (
            sourceType is IArrayTypeSymbol sourceArray
            && targetType is IArrayTypeSymbol targetArray
        )
        {
            if (
                !TryGetElementMapping(sourceArray.ElementType, targetArray.ElementType, out var map)
            )
                return false;
            var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.Name}";
            var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}";
            mapTarget.MapToMethod.Contents.Add("{");
            mapTarget.MapToMethod.Contents.Add(
                $"{target} = new {targetArray.GetFullName()}[{source}.Length];"
            );
            mapTarget.MapToMethod.Contents.Add($"for (var i = 0; i < {source}.Length; i++)");
            mapTarget.MapToMethod.Contents.Add(
                $"    {target}[i] = {map.ToTarget($"{source}[i]")};"
            );
            mapTarget.MapToMethod.Contents.Add("}");

            mapTarget.MapFromMethod.Contents.Add("{");
            mapTarget.MapFromMethod.Contents.Add(
                $"{source} = new {sourceArray.GetFullName()}[{target}.Length];"
            );
            mapTarget.MapFromMethod.Contents.Add($"for (var i = 0; i < {target}.Length; i++)");
            mapTarget.MapFromMethod.Contents.Add(
                $"    {source}[i] = {map.ToSource($"{target}[i]")};"
            );
            mapTarget.MapFromMethod.Contents.Add("}");
            return true;
        }

        if (
            TryGetGeneric(sourceType, TypeFullNames.ICollectionT, out var sourceCollection)
            && TryGetGeneric(targetType, TypeFullNames.ICollectionT, out var targetCollection)
        )
        {
            if (!TryGetElementMapping(sourceCollection[0], targetCollection[0], out var map))
                return false;
            var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.Name}";
            var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}";
            var genericName = sourceCollection[0].GetFullName();

            mapTarget.MapToMethod.Contents.Add("{");
            mapTarget.MapToMethod.Contents.Add($"if({target} is null) {target} = new();");
            mapTarget.MapToMethod.Contents.Add($"else if({target}.Count > 0) {target}.Clear();");
            mapTarget.MapToMethod.Contents.Add(
                $"foreach (var item in {source}) (({TypeFullNames.ICollectionNeedGeneric}<{genericName}>){target}).Add({map.ToTarget("item")});"
            );
            mapTarget.MapToMethod.Contents.Add("}");

            mapTarget.MapFromMethod.Contents.Add("{");
            mapTarget.MapFromMethod.Contents.Add($"if({source} is null) {source} = new();");
            mapTarget.MapFromMethod.Contents.Add($"else if({source}.Count > 0) {source}.Clear();");
            mapTarget.MapFromMethod.Contents.Add(
                $"foreach (var item in {target}) (({TypeFullNames.ICollectionNeedGeneric}<{genericName}>){source}).Add({map.ToSource("item")});"
            );
            mapTarget.MapFromMethod.Contents.Add("}");
            return true;
        }

        return false;
    }

    private bool TryGetElementMapping(
        ITypeSymbol sourceType,
        ITypeSymbol targetType,
        out ElementMapping mapping
    )
    {
        if (sourceType.SymbolEquals(targetType))
        {
            mapping = new(value => value, value => value);
            return true;
        }

        var nested = AllMapTargets.FirstOrDefault(x =>
            x.SourceType.SymbolEquals(sourceType) && x.TargetType.SymbolEquals(targetType)
        );
        if (
            nested is not null
            && nested.Direction.HasFlag(MapDirections.To)
            && targetType is INamedTypeSymbol namedTarget
            && namedTarget.InstanceConstructors.Any(c =>
                c.Parameters.Length == 0 && c.DeclaredAccessibility >= Accessibility.Internal
            )
        )
        {
            var extensionType = nested.SourceType.GetFullName().Replace('.', '_');
            mapping = new(
                value =>
                    $"global::HKW.HKWMapper.{extensionType}MapExtensions.{nested.MapToName}({value}, new {targetType.GetFullName()}())",
                value =>
                    $"global::HKW.HKWMapper.{extensionType}MapExtensions.{nested.MapFromName}(new {sourceType.GetFullName()}(), {value})"
            );
            return true;
        }

        mapping = default;
        return false;
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (
            type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        )
            return named.TypeArguments[0];
        return type;
    }

    private static bool TryGetGeneric(
        ITypeSymbol type,
        string metadataShape,
        out ITypeSymbol[] arguments
    )
    {
        arguments = [];
        if (type is not INamedTypeSymbol named || named.TypeArguments.Length == 0)
            return false;
        var expected = metadataShape.Substring(0, metadataShape.IndexOf('<'));
        var name = named.ContainingNamespace.ToDisplayString() + "." + named.Name;
        if (!string.Equals(name, expected, StringComparison.Ordinal))
            return false;
        arguments = named.TypeArguments.ToArray();
        return true;
    }

    private readonly struct ElementMapping
    {
        public ElementMapping(Func<string, string> toTarget, Func<string, string> toSource)
        {
            ToTarget = toTarget;
            ToSource = toSource;
        }

        public Func<string, string> ToTarget { get; }
        public Func<string, string> ToSource { get; }
    }

    private static void TryAddConfig(MapTargetInfo mapTarget)
    {
        if (mapTarget.ConfigInfo is null)
            return;
        mapTarget.MapToMethod.Contents.Add(
            $"var {MapConfigInfo.ConfigName} = new {mapTarget.ConfigInfo.Type.GetFullName()}({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName});"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"var {MapConfigInfo.ConfigName} = new {mapTarget.ConfigInfo.Type.GetFullName()}({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName});"
        );
    }

    private static bool TryUseConfigPropertyConverter(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        IPropertySymbol targetProperty
    )
    {
        if (
            mapTarget.ConfigInfo?.Converters.TryGetValue(propertySymbol.Name, out var data)
            is not true
        )
            return false;

        var converterType = (INamedTypeSymbol)data.Property.Type;
        // 判断转换器泛型类型是否与映射类型相同
        if (
            converterType.TypeArguments[0].SymbolEquals(propertySymbol.Type) is false
            || converterType.TypeArguments[1].SymbolEquals(targetProperty.Type) is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterGenericTypeError,
                data.Property.Locations[0],
                converterType.TypeArguments[0].GetName(),
                converterType.TypeArguments[1].GetName(),
                propertySymbol.Type.GetName(),
                targetProperty.Type.GetName()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return false;
        }
        var convertName = $"{MapConfigInfo.ConfigName}.{data.Property.Name}";
        // 使用转换器转换
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = {convertName}.{nameof(IMapConverter<,>.Convert)}({MapTargetInfo.SourceParamName},{MapTargetInfo.SourceParamName}.{propertySymbol.Name});"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = {convertName}.{nameof(IMapConverter<,>.ConvertBack)}({MapTargetInfo.TargetParamName},{MapTargetInfo.TargetParamName}.{targetProperty.Name});"
        );
        return true;
    }

    private bool TryUsePropertyConverter(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        AttributeInfo? attributeInfo,
        IPropertySymbol targetProperty
    )
    {
        if (
            attributeInfo?.TryGetParam<INamedTypeSymbol>(
                nameof(MapPropertyAttribute.ConverterType),
                out var converterType
            )
                is not true
            || converterType is null
        )
            return false;

        var converterInterface = converterType.Interfaces.FirstOrDefault(i =>
            i.OriginalDefinition.GetFullName() == TypeFullNames.MapConverterInterface
        );
        // 判断转换器是否实现转换器接口
        if (converterInterface is null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterNotImplementIMapConverter,
                attributeInfo.Data.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                    attributeInfo.Data.ApplicationSyntaxReference.Span
                ),
                targetProperty.ToString()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return false;
        }
        // 判断转换器泛型类型是否与映射类型相同
        if (
            converterInterface.TypeArguments[0].SymbolEquals(propertySymbol.Type) is false
            || converterInterface.TypeArguments[1].SymbolEquals(targetProperty.Type) is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterGenericTypeError,
                attributeInfo.Data.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                    attributeInfo.Data.ApplicationSyntaxReference.Span
                ),
                converterInterface.TypeArguments[0].GetName(),
                converterInterface.TypeArguments[1].GetName(),
                propertySymbol.Type.GetName(),
                targetProperty.Type.GetName()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return false;
        }
        ClassInfo.AddConverters(converterType, out var fieldName);
        // 使用转换器转换
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = {fieldName}.{nameof(IMapConverter<,>.Convert)}({MapTargetInfo.SourceParamName},{MapTargetInfo.SourceParamName}.{propertySymbol.Name});"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = {fieldName}.{nameof(IMapConverter<,>.ConvertBack)}({MapTargetInfo.TargetParamName},{MapTargetInfo.TargetParamName}.{targetProperty.Name});"
        );
        return true;
    }

    private static IPropertySymbol? GetTargetProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol property,
        string targetPropertyName
    )
    {
        // 目标属性不存在
        if (
            mapTarget.PropertyByName.TryGetValue(targetPropertyName, out var targetProperty)
            is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyNotExists,
                property.Locations[0],
                mapTarget.TargetType.GetName() + "." + property.Name
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        // 只读属性
        if (targetProperty.SetMethod is null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyIsReadOnly,
                property.Locations[0],
                targetProperty.ToString()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        // 静态属性
        if (targetProperty.IsStatic)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyIsStatic,
                property.Locations[0],
                targetProperty.ToString()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        if (targetProperty.SetMethod.DeclaredAccessibility == Accessibility.Public)
            return targetProperty;
        // 可访问性为本地或保护本地
        if (
            targetProperty.SetMethod.DeclaredAccessibility == Accessibility.Internal
            || targetProperty.SetMethod.DeclaredAccessibility == Accessibility.ProtectedOrInternal
        )
        {
            // 在主程序集
            if (
                targetProperty.SetMethod.ContainingAssembly.SymbolEquals(
                    GeneratorHelper.Compilation.Assembly
                )
            )
                return targetProperty;

            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyAccessibilityInsufficient,
                property.Locations[0],
                targetProperty.ToString()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        // 更低的可访问性
        if (targetProperty.SetMethod.DeclaredAccessibility < Accessibility.Internal)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyAccessibilityInsufficient,
                property.Locations[0],
                targetProperty.ToString()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return null;
        }
        return targetProperty;
    }

    private static void AddStartAction(MapTargetInfo mapTarget)
    {
        if (mapTarget.ConfigInfo is null)
            return;
        foreach (var action in mapTarget.ConfigInfo.MapTo.StartActions)
        {
            mapTarget.MapToMethod.Contents.Add(
                action.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }

        foreach (var action in mapTarget.ConfigInfo.MapFrom.StartActions)
        {
            mapTarget.MapFromMethod.Contents.Add(
                action.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }

    private static void AddEndAction(MapTargetInfo mapTarget)
    {
        if (mapTarget.ConfigInfo is null)
            return;
        foreach (var action in mapTarget.ConfigInfo.MapTo.EndActions)
        {
            mapTarget.MapToMethod.Contents.Add(
                action.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }

        foreach (var action in mapTarget.ConfigInfo.MapFrom.EndActions)
        {
            mapTarget.MapFromMethod.Contents.Add(
                action.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }
}
