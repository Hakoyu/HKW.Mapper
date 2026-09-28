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
    public static void Generate(
        ClassInfo classInfo,
        IReadOnlyDictionary<INamedTypeSymbol, HashSet<MapTargetInfo>> mapTargetDic
    )
    {
        var x = new MapperGenerator(classInfo, mapTargetDic);
        x.Execute();
    }

    public MapperGenerator(
        ClassInfo classInfo,
        IReadOnlyDictionary<INamedTypeSymbol, HashSet<MapTargetInfo>> mapTargetDic
    )
    {
        ClassInfo = classInfo;
        MapTargetDic = mapTargetDic;
    }

    public ClassInfo ClassInfo { get; }
    public IReadOnlyDictionary<INamedTypeSymbol, HashSet<MapTargetInfo>> MapTargetDic { get; }

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
        var matchingAttributes = new List<AttributeData>();
        foreach (var att in propertyAttributes)
        {
            var targetName = GetMapPropertyTargetName(att);
            if (mapTarget.TargetName == targetName)
            {
                matchingAttributes.Add(att);
            }
            else if (ClassInfo.MapTargets.All(x => x.TargetName != targetName))
            {
                var diagnostic = Diagnostic.Create(
                    Descriptors.MapPropertyTargetNotFound,
                    att.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                        att.ApplicationSyntaxReference.Span
                    ),
                    targetName
                );
                GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            }
        }
        if (matchingAttributes.Count > 1)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.MapPropertyTargetAmbiguous,
                propertySymbol.Locations[0],
                propertySymbol.Name
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return;
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

    private static string GetMapPropertyTargetName(AttributeData attribute)
    {
        var ps = attribute.GetParams();
        var targetName = string.Empty;
        if (
            ps.TryGetParam<INamedTypeSymbol>(
                nameof(MapPropertyAttribute.TargetType),
                out var targetType
            )
        )
        {
            targetName = targetType.Name;
        }
        else if (ps.TryGetParam<string>(nameof(MapPropertyAttribute.TargetName), out var name))
        {
            targetName = name;
        }

        return targetName;
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

        // 比较当前属性与目标属性的类型
        if (propertySymbol.Type.SymbolEquals(targetProperty.Type) is false)
        {
            if (TryMapNullableProperty(mapTarget, propertySymbol, targetProperty))
                return;
            // 如果当前属性类型与目标属性类型不一样, 则异常
            var diagnostic = Diagnostic.Create(
                Descriptors.TargetPropertyTypeError,
                propertySymbol.Locations[0],
                targetProperty.ToString()
            );
            GeneratorHelper.ProductionContext.ReportDiagnostic(diagnostic);
            return;
        }
        if (propertySymbol.Type.SpecialType != SpecialType.System_String)
        {
            if (propertySymbol.Type.HasInterface(TypeFullNames.ICloneable))
            {
                // 如果实现了 ICloneable, 则克隆
                mapTarget.MapToMethod.Contents.Add(
                    $"{MapTargetInfo.TargetParamName}.{targetProperty.Name} = ({propertySymbol.Type.GetFullName()}){MapTargetInfo.SourceParamName}.{propertySymbol.Name}?.{nameof(ICloneable.Clone)}()!;"
                );
                mapTarget.MapFromMethod.Contents.Add(
                    $"{MapTargetInfo.SourceParamName}.{propertySymbol.Name} = ({targetProperty.Type.GetFullName()}){MapTargetInfo.TargetParamName}.{targetProperty.Name}?.{nameof(ICloneable.Clone)}()!;"
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
        var toExpression =
            sourceNullable && targetNullable is false ? $"{source} ?? default!" : source;
        var fromExpression =
            targetNullable && sourceNullable is false ? $"{target} ?? default!" : target;
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
                TryGetElementMapping(
                    (INamedTypeSymbol)sourceArray.ElementType,
                    (INamedTypeSymbol)targetArray.ElementType,
                    out var map
                )
                is false
            )
                return false;
            var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.Name}";
            var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}";
            if (map.CanMapTo)
            {
                mapTarget.MapToMethod.Contents.Add("{");
                mapTarget.MapToMethod.Contents.Add(
                    $"{target} = new {targetArray.ElementType.GetFullName()}[{source}.Length];"
                );
                mapTarget.MapToMethod.Contents.Add($"for (var i = 0; i < {source}.Length; i++)");
                mapTarget.MapToMethod.Contents.Add(
                    $"    {target}[i] = {map.ToTarget!($"{source}[i]")};"
                );
                mapTarget.MapToMethod.Contents.Add("}");
            }

            if (map.CanMapFrom)
            {
                mapTarget.MapFromMethod.Contents.Add("{");
                mapTarget.MapFromMethod.Contents.Add(
                    $"{source} = new {sourceArray.ElementType.GetFullName()}[{target}.Length];"
                );
                mapTarget.MapFromMethod.Contents.Add($"for (var i = 0; i < {target}.Length; i++)");
                mapTarget.MapFromMethod.Contents.Add(
                    $"    {source}[i] = {map.ToSource!($"{target}[i]")};"
                );
                mapTarget.MapFromMethod.Contents.Add("}");
            }
            return true;
        }

        if (
            TryMapDictionaryProperty(
                mapTarget,
                sourceProperty,
                targetProperty,
                sourceType,
                targetType
            )
        )
            return true;

        if (
            TryMapCollectionProperty(
                mapTarget,
                sourceProperty,
                targetProperty,
                sourceType,
                targetType
            )
        )
            return true;

        return false;
    }

    private bool TryMapCollectionProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty,
        ITypeSymbol sourceType,
        ITypeSymbol targetType
    )
    {
        if (
            TryGetGeneric(sourceType, TypeFullNames.ICollectionT, out var sourceArgs) is false
            || TryGetGeneric(targetType, TypeFullNames.ICollectionT, out var targetArgs) is false
            || TryGetElementMapping(sourceArgs[0], targetArgs[0], out var map) is false
        )
            return false;
        var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.Name}";
        var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}";
        if (map.CanMapTo)
        {
            mapTarget.MapToMethod.Contents.Add("{");
            var targetItem = map.ToTarget!("item");
            mapTarget.MapToMethod.Contents.Add(
                $"if ({target} is null) {target} = new(); else {target}.Clear();"
            );
            mapTarget.MapToMethod.Contents.Add(
                $"foreach (var item in {source}) {target}.Add({targetItem});"
            );
            mapTarget.MapToMethod.Contents.Add("}");
        }
        if (map.CanMapFrom)
        {
            mapTarget.MapFromMethod.Contents.Add("{");
            var sourceItem = map.ToSource!("item");
            mapTarget.MapFromMethod.Contents.Add(
                $"if ({source} is null) {source} = new(); else {source}.Clear();"
            );
            mapTarget.MapFromMethod.Contents.Add(
                $"foreach (var item in {target}) {source}.Add({sourceItem});"
            );
            mapTarget.MapFromMethod.Contents.Add("}");
        }
        return true;
    }

    private bool TryMapDictionaryProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol sourceProperty,
        IPropertySymbol targetProperty,
        ITypeSymbol sourceType,
        ITypeSymbol targetType
    )
    {
        if (
            TryGetGeneric(sourceType, TypeFullNames.IDictionaryT, out var sourceArgs) is false
            || TryGetGeneric(targetType, TypeFullNames.IDictionaryT, out var targetArgs) is false
            || TryGetElementMapping(sourceArgs[0], targetArgs[0], out var keyMap) is false
            || TryGetElementMapping(sourceArgs[1], targetArgs[1], out var valueMap) is false
        )
            return false;
        var source = $"{MapTargetInfo.SourceParamName}.{sourceProperty.Name}";
        var target = $"{MapTargetInfo.TargetParamName}.{targetProperty.Name}";
        mapTarget.MapToMethod.Contents.Add("{");
        if (keyMap.CanMapTo && valueMap.CanMapTo)
        {
            var targetKey = keyMap.ToTarget!("item.Key");
            var targetValue = valueMap.ToTarget!("item.Value");
            mapTarget.MapToMethod.Contents.Add(
                $"if ({target} is null) {target} = new(); else {target}.Clear();"
            );
            mapTarget.MapToMethod.Contents.Add(
                $"foreach (var item in {source}) {target}.Add({targetKey}, {targetValue});"
            );
        }
        mapTarget.MapToMethod.Contents.Add("}");
        mapTarget.MapFromMethod.Contents.Add("{");
        if (keyMap.CanMapFrom && valueMap.CanMapFrom)
        {
            var sourceKey = keyMap.ToSource!("item.Key");
            var sourceValue = valueMap.ToSource!("item.Value");
            mapTarget.MapFromMethod.Contents.Add(
                $"if ({source} is null) {source} = new(); else {source}.Clear();"
            );
            mapTarget.MapFromMethod.Contents.Add(
                $"foreach (var item in {target}) {source}.Add({sourceKey}, {sourceValue});"
            );
        }
        mapTarget.MapFromMethod.Contents.Add("}");
        return true;
    }

    private bool TryGetElementMapping(
        INamedTypeSymbol sourceType,
        INamedTypeSymbol targetType,
        out ElementMapping mapping
    )
    {
        if (sourceType.SymbolEquals(targetType))
        {
            mapping = new(value => value, value => value, true, true);
            return true;
        }
        mapping = default;
        if (MapTargetDic.TryGetValue(sourceType, out var targets) is false)
            return false;
        var nested = targets.FirstOrDefault(t => t.TargetType.SymbolEquals(targetType));
        var canMapTo = nested?.Direction.HasFlag(MapDirections.To) is true;
        var canMapFrom = nested?.Direction.HasFlag(MapDirections.From) is true;
        if (
            (canMapTo || canMapFrom)
            && targetType is INamedTypeSymbol namedTarget
            && namedTarget.InstanceConstructors.Any(c =>
                c.Parameters.Length == 0 && c.DeclaredAccessibility >= Accessibility.Internal
            )
        )
        {
            var nestedTarget = nested!;
            var extensionType = nestedTarget
                .SourceType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                .Replace('.', '_');
            mapping = new(
                canMapTo
                    ? value =>
                        $"global::HKW.HKWMapper.{extensionType}MapExtensions.{nestedTarget.MapToName}({value}, new {targetType.GetFullName()}())"
                    : null,
                canMapFrom
                    ? value =>
                        $"global::HKW.HKWMapper.{extensionType}MapExtensions.{nestedTarget.MapFromName}(new {sourceType.GetFullName()}(), {value})"
                    : null,
                canMapTo,
                canMapFrom
            );
            return true;
        }
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
        out INamedTypeSymbol[] arguments
    )
    {
        arguments = [];
        if (type is not INamedTypeSymbol named)
            return false;
        var matchingInterface = named.AllInterfaces.FirstOrDefault(i =>
            i.OriginalDefinition.GetFullName() == metadataShape
        );
        if (matchingInterface is null)
            return false;
        arguments = matchingInterface.TypeArguments.Cast<INamedTypeSymbol>().ToArray();
        return true;
    }

    private readonly struct ElementMapping
    {
        public ElementMapping(
            Func<string, string>? toTarget,
            Func<string, string>? toSource,
            bool canMapTo,
            bool canMapFrom
        )
        {
            ToTarget = toTarget;
            ToSource = toSource;
            CanMapTo = canMapTo;
            CanMapFrom = canMapFrom;
        }

        public Func<string, string>? ToTarget { get; }
        public Func<string, string>? ToSource { get; }
        public bool CanMapTo { get; }
        public bool CanMapFrom { get; }
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
