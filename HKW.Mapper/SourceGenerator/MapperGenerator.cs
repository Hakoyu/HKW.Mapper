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
    public static MapperGenerator Generate(ClassInfo classInfo)
    {
        var x = new MapperGenerator(classInfo);
        x.Execute();
        return x;
    }

    public MapperGenerator(ClassInfo classInfo)
    {
        ClassInfo = classInfo;
    }

    public ClassInfo ClassInfo { get; }

    /// <summary>
    /// (ObjectType, (memberName, isAsync))
    /// </summary>
    public Dictionary<INamedTypeSymbol, Dictionary<string, bool>> MapperConfigInfoByType
    {
        get;
        private set;
    } = null!;
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
        }
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

    private void ParseProperty(MapTargetInfo mapTarget, IPropertySymbol propertySymbol)
    {
        var atts = propertySymbol.GetAttributes();
        if (atts.Any(a => a.AttributeClass?.GetFullName() == TypeFullNames.MapTargetAttribute))
            return;
        var attributeData = atts.FirstOrDefault(a =>
            a.AttributeClass?.GetFullName() == mapTarget.PropertyAttributeFullName
        );

        var attributeInfo = attributeData is null ? null : new AttributeInfo(attributeData);
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
            mapTarget.MapFromMethod.Contents.RemoveAt(mapTarget.MapToMethod.Contents.Count - 1);
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

    private static void MapProperty(
        MapTargetInfo mapTarget,
        IPropertySymbol propertySymbol,
        AttributeInfo? attributeInfo,
        IPropertySymbol targetProperty
    )
    {
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

        if (
            propertySymbol.Type.IsReferenceType
            && (
                attributeInfo?.TryGetParam<bool>(
                    nameof(MapPropertyAttribute.MapPropertyType),
                    out var mapRef
                )
                    is not true
                || mapRef is not true
            )
        )
        {
            // 如果是引用类型, 则错误
            var diagnostic = Diagnostic.Create(
                Descriptors.PropertyIsReferenceType,
                propertySymbol.Locations[0],
                mapTarget.PropertyAttributeName
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

        // 判断转换器是否实现转换器接口
        if (converterType.ImplementInterface(TypeFullNames.MapConverterInterface) is false)
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
            converterType.TypeArguments[0].SymbolEquals(propertySymbol.Type) is false
            || converterType.TypeArguments[1].SymbolEquals(targetProperty.Type) is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterGenericTypeError,
                attributeInfo.Data.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                    attributeInfo.Data.ApplicationSyntaxReference.Span
                ),
                converterType.TypeArguments[0].GetName(),
                converterType.TypeArguments[1].GetName(),
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
            mapTarget.MapToMethod.Contents.Add(
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
            mapTarget.MapToMethod.Contents.Add(
                action.BuildInvocationStatement(MapConfigInfo.ConfigName)
            );
        }
    }
}
