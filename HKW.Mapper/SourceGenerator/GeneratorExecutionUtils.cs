﻿using System;
using System.Collections.Generic;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWMapper.SourceGenerator;

internal partial class GeneratorExecution
{
    private Dictionary<INamedTypeSymbol, MapperConfigInfo> GetMapperConfigInfos(
        GeneratorExecutionContext context
    )
    {
        var mapperConfigInfoByType = new Dictionary<INamedTypeSymbol, MapperConfigInfo>(
            SymbolEqualityComparer.Default
        );
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            return mapperConfigInfoByType;
        foreach (var classDeclaration in receiver.CandidateClasses)
        {
            var semanticModel = Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
            if (
                SymbolEqualityComparer.Default.Equals(
                    classSymbol?.BaseType?.OriginalDefinition,
                    MapConfigType
                )
                is false
            )
                continue;
            var mapConfigInfo = new MapperConfigInfo();
            mapperConfigInfoByType.Add(classSymbol!, mapConfigInfo);
            var ctor = classDeclaration
                .Members.OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(x => x.ParameterList.Parameters.Count == 0);

            if (ctor is null)
                continue;
            foreach (var invocation in ctor.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (
                    invocation.Expression is not IdentifierNameSyntax identifier
                    || identifier.Identifier.Text != "AddMap"
                )
                {
                    continue;
                }
                if (
                    invocation.ArgumentList.Arguments[0].Expression
                    is not SimpleLambdaExpressionSyntax lambdaExpression
                )
                {
                    continue;
                }
                var memberName = string.Empty;
                if (lambdaExpression.Body is MemberAccessExpressionSyntax body)
                {
                    memberName = body.Name.ToString();
                }
                if (lambdaExpression.Body is PostfixUnaryExpressionSyntax postfix)
                {
                    if (postfix.Operand is not MemberAccessExpressionSyntax member)
                        continue;
                    memberName = member.Name.ToString();
                }
                if (
                    string.IsNullOrWhiteSpace(memberName) is false
                    && mapConfigInfo.AddedMapProperties.Add(memberName) is false
                )
                {
                    var errorDiagnostic = Diagnostic.Create(
                        Descriptors.SameMapPropertyConfig,
                        lambdaExpression.Body.GetLocation(),
                        memberName
                    );
                    ExecutionContext.ReportDiagnostic(errorDiagnostic);
                }
            }
        }
        return mapperConfigInfoByType;
    }

    private HashSet<string> GetMapMethods(SyntaxTree compilationSyntaxTree)
    {
        var mapMethods = new HashSet<string>();
        var semanticModel = ExecutionContext.Compilation.GetSemanticModel(compilationSyntaxTree);
        var declaredClasses = compilationSyntaxTree
            .GetRoot()
            .DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>();
        foreach (var declaredClass in declaredClasses)
        {
            var classSymbol = (INamedTypeSymbol)
                ModelExtensions.GetDeclaredSymbol(semanticModel, declaredClass)!;
            var attributeDatas = classSymbol
                .GetAttributes()
                .Where(x =>
                    x.AttributeClass!.ToString() is string name
                    && (
                        name == typeof(MapToAttribute).FullName
                        || name == typeof(MapFromAttribute).FullName
                    )
                );
            foreach (var attribute in attributeDatas)
            {
                var attributeName = attribute.AttributeClass!.GetFullName();
                if (attributeName == typeof(MapToAttribute).FullName)
                {
                    var parameters = attribute.GetAttributeParameters();
                    if (parameters.Count == 0)
                        continue;
                    if (
                        parameters.TryGetValue(nameof(MapToAttribute.TargetType), out var value)
                        is false
                    )
                        continue;
                    parameters.TryGetValue(nameof(MapToAttribute.MethodName), out var methodData);
                    var methodName = methodData?.Value?.ToString();
                    var type = (INamedTypeSymbol)value.Value!;
                    if (string.IsNullOrWhiteSpace(methodName))
                        methodName = $"{classSymbol.Name}MapTo{type.Name}";
                    if (mapMethods.Add(methodName!) is false)
                    {
                        var errorDiagnostic = Diagnostic.Create(
                            Descriptors.SameMethodName,
                            attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                                attribute.ApplicationSyntaxReference.Span
                            ),
                            methodName
                        );
                        ExecutionContext.ReportDiagnostic(errorDiagnostic);
                    }
                }
                else if (attributeName == typeof(MapFromAttribute).FullName)
                {
                    var parameters = attribute.GetAttributeParameters();
                    if (
                        parameters.TryGetValue(nameof(MapToAttribute.TargetType), out var value)
                        is false
                    )
                        continue;
                    parameters.TryGetValue(nameof(MapToAttribute.MethodName), out var methodData);
                    var methodName = methodData?.Value?.ToString();
                    var type = (INamedTypeSymbol)value.Value!;
                    if (string.IsNullOrWhiteSpace(methodName))
                        methodName = $"{classSymbol.Name}MapFrom{type.Name}";
                    if (mapMethods.Add(methodName!) is false)
                    {
                        var errorDiagnostic = Diagnostic.Create(
                            Descriptors.SameMethodName,
                            classSymbol.Locations[0],
                            methodName
                        );
                        ExecutionContext.ReportDiagnostic(errorDiagnostic);
                    }
                }
            }
        }
        return mapMethods;
    }

    private IPropertySymbol? CheckProperty(
        MapInfo target,
        IPropertySymbol property,
        string? targetPropertyName
    )
    {
        // 目标属性不存在
        if (
            target.TargetType.GetMember<IPropertySymbol>(targetPropertyName!)
            is not IPropertySymbol targetProperty
        )
        {
            if (target.ScrutinyMode)
            {
                var errorDiagnostic = Diagnostic.Create(
                    Descriptors.TargetPropertyNotExists,
                    property.Locations[0],
                    target.TargetType.GetFullNameAndGeneric() + "." + property.Name
                );
                ExecutionContext.ReportDiagnostic(errorDiagnostic);
            }
            return null;
        }
        // 只读属性
        if (targetProperty.SetMethod is null)
        {
            if (target.ScrutinyMode)
            {
                var errorDiagnostic = Diagnostic.Create(
                    Descriptors.TargetPropertyIsReadOnly,
                    property.Locations[0],
                    targetProperty.ToString()
                );
                ExecutionContext.ReportDiagnostic(errorDiagnostic);
            }
            return null;
        }
        // 静态属性
        if (targetProperty.IsStatic)
        {
            if (target.ScrutinyMode)
            {
                var errorDiagnostic = Diagnostic.Create(
                    Descriptors.TargetPropertyIsStatic,
                    property.Locations[0],
                    targetProperty.ToString()
                );
                ExecutionContext.ReportDiagnostic(errorDiagnostic);
            }
            return null;
        }
        if (targetProperty.SetMethod.DeclaredAccessibility == Accessibility.Public)
            return targetProperty;
        // 可访问性为本地或保护本地
        if (
            (
                targetProperty.SetMethod.DeclaredAccessibility == Accessibility.Internal
                || targetProperty.SetMethod.DeclaredAccessibility
                    == Accessibility.ProtectedOrInternal
            )
        )
        {
            // 在主程序集
            if (
                SymbolEqualityComparer.Default.Equals(
                    targetProperty.SetMethod.ContainingAssembly,
                    Compilation.Assembly
                )
            )
                return targetProperty;
            if (target.ScrutinyMode)
            {
                var errorDiagnostic = Diagnostic.Create(
                    Descriptors.TargetPropertyInsufficientAccessibility,
                    property.Locations[0],
                    targetProperty.ToString()
                );
                ExecutionContext.ReportDiagnostic(errorDiagnostic);
            }
            return null;
        }
        // 更低的可访问性
        if (targetProperty.SetMethod.DeclaredAccessibility < Accessibility.Internal)
        {
            if (target.ScrutinyMode)
            {
                var errorDiagnostic = Diagnostic.Create(
                    Descriptors.TargetPropertyInsufficientAccessibility,
                    property.Locations[0],
                    targetProperty.ToString()
                );
                ExecutionContext.ReportDiagnostic(errorDiagnostic);
            }
            return null;
        }
        return targetProperty;
    }

    private bool CheckConverter(
        IPropertySymbol property,
        IPropertySymbol targetProperty,
        AttributeData attributeData,
        INamedTypeSymbol converterType
    )
    {
        var iconverter = converterType.Interfaces.First(x =>
            SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, IMapConverterType)
        );
        var converterCurrentType = iconverter.TypeArguments[0];
        // 比较转换器当前属性与当前属性的类型
        if (SymbolEqualityComparer.Default.Equals(property.Type, converterCurrentType) is false)
        {
            var errorDiagnostic = Diagnostic.Create(
                Descriptors.ConverterCurrentTypeDifferentNoMethod,
                attributeData.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                    attributeData.ApplicationSyntaxReference.Span
                ),
                converterCurrentType.GetFullNameAndGeneric(),
                property.Type.GetFullNameAndGeneric()
            );
            ExecutionContext.ReportDiagnostic(errorDiagnostic);
            return false;
        }
        var converterTargetType = iconverter.TypeArguments[1];
        // 比较转换器目标属性与目标属性的类型
        if (
            SymbolEqualityComparer.Default.Equals(targetProperty.Type, converterTargetType) is false
        )
        {
            var errorDiagnostic = Diagnostic.Create(
                Descriptors.ConverterTargetTypeDifferentNoMethod,
                attributeData.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                    attributeData.ApplicationSyntaxReference.Span
                ),
                converterTargetType.GetFullNameAndGeneric(),
                targetProperty.Type.GetFullNameAndGeneric()
            );
            ExecutionContext.ReportDiagnostic(errorDiagnostic);
            return false;
        }
        return true;
    }

    private void GetDataFromPropertyAttribute(
        AttributeData attributeData,
        ref string? targetPropertyName,
        ref INamedTypeSymbol? converterType,
        out Dictionary<string, AttributeParameterValue> parameters
    )
    {
        parameters = attributeData.GetAttributeParameters();
        if (parameters.Count == 0)
            return;

        if (parameters.TryGetValue(nameof(MapPropertyAttribute.PropertyName), out var nameData))
        {
            var name = nameData.Value?.ToString();
            if (string.IsNullOrWhiteSpace(name) is false)
                targetPropertyName = name;
        }
        if (
            parameters.TryGetValue(
                nameof(MapPropertyAttribute.ConverterType),
                out var converterData
            )
        )
        {
            converterType = converterData.Value as INamedTypeSymbol;
            if (converterType is not null)
            {
                if (
                    converterType.Interfaces.Any(x =>
                        SymbolEqualityComparer.Default.Equals(
                            x.OriginalDefinition,
                            IMapConverterType
                        )
                    )
                    is false
                )
                {
                    var errorDiagnostic = Diagnostic.Create(
                        Descriptors.ConverterError,
                        attributeData.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                            attributeData.ApplicationSyntaxReference.Span
                        ),
                        converterType.Name
                    );
                    ExecutionContext.ReportDiagnostic(errorDiagnostic);
                }
                Converters.Add(converterType);
            }
        }
    }

    private Dictionary<
        INamedTypeSymbol,
        (HashSet<MapInfo> ToMethods, HashSet<MapInfo> FromMethods)
    > GetMapTargetAndMethod(
        SemanticModel semanticModel,
        IEnumerable<ClassDeclarationSyntax> declaredClasses
    )
    {
        var mapMethodsByType = new Dictionary<
            INamedTypeSymbol,
            (HashSet<MapInfo> ToMethods, HashSet<MapInfo> FromMethods)
        >(SymbolEqualityComparer.Default);
        foreach (var declaredClass in declaredClasses)
        {
            var classSymbol = (INamedTypeSymbol)
                ModelExtensions.GetDeclaredSymbol(semanticModel, declaredClass)!;
            var attributeDatas = classSymbol
                .GetAttributes()
                .Where(x =>
                    x.AttributeClass!.ToString() is string name
                    && (
                        name == typeof(MapToAttribute).FullName
                        || name == typeof(MapFromAttribute).FullName
                    )
                );
            foreach (var attribute in attributeDatas)
            {
                var attributeName = attribute.AttributeClass!.GetFullName();
                if (attributeName == typeof(MapToAttribute).FullName)
                {
                    var parameters = attribute.GetAttributeParameters();
                    if (parameters.Count == 0)
                        continue;
                    if (
                        parameters.TryGetValue(nameof(MapToAttribute.TargetType), out var value)
                        is false
                    )
                        continue;
                    parameters.TryGetValue(nameof(MapToAttribute.MethodName), out var method);
                    parameters.TryGetValue(nameof(MapToAttribute.ScrutinyMode), out var scrutiny);
                    parameters.TryGetValue(nameof(MapToAttribute.MapperConfig), out var mapConfig);
                    var targetType = (INamedTypeSymbol)value.Value!;
                    var methodName = method?.Value?.ToString() ?? $"MapTo{targetType.Name}";
                    var mapConfigType = mapConfig?.Value as INamedTypeSymbol;
                    if (mapMethodsByType.TryGetValue(classSymbol, out var targets) is false)
                        targets = mapMethodsByType[classSymbol] = ([], []);
                    mapConfigType = CheckMapConfig(
                        classSymbol,
                        attribute,
                        targetType,
                        mapConfigType
                    );
                    targets.ToMethods.Add(
                        new(targetType, methodName, scrutiny?.Value is true, mapConfigType)
                    );
                }
                else if (attributeName == typeof(MapFromAttribute).FullName)
                {
                    var parameters = attribute.GetAttributeParameters();
                    if (parameters.Count == 0)
                        continue;
                    if (
                        parameters.TryGetValue(nameof(MapToAttribute.TargetType), out var value)
                        is false
                    )
                        continue;
                    parameters.TryGetValue(nameof(MapToAttribute.MethodName), out var method);
                    parameters.TryGetValue(nameof(MapToAttribute.ScrutinyMode), out var scrutiny);
                    parameters.TryGetValue(nameof(MapToAttribute.MapperConfig), out var mapConfig);
                    var targetType = (INamedTypeSymbol)value.Value!;
                    var mapConfigType = mapConfig?.Value as INamedTypeSymbol;
                    var methodName = method?.Value?.ToString() ?? $"MapFrom{targetType.Name}";
                    if (mapMethodsByType.TryGetValue(classSymbol, out var targets) is false)
                        targets = mapMethodsByType[classSymbol] = ([], []);
                    mapConfigType = CheckMapConfig(
                        classSymbol,
                        attribute,
                        targetType,
                        mapConfigType
                    );
                    targets.FromMethods.Add(
                        new(targetType, methodName, scrutiny?.Value is true, mapConfigType)
                    );
                }
            }
        }
        return mapMethodsByType;
    }

    private INamedTypeSymbol? CheckMapConfig(
        INamedTypeSymbol classSymbol,
        AttributeData attribute,
        INamedTypeSymbol targetType,
        INamedTypeSymbol? mapConfigType
    )
    {
        if (
            SymbolEqualityComparer.Default.Equals(
                mapConfigType?.BaseType?.OriginalDefinition,
                MapConfigType
            )
        )
        {
            if (mapConfigType?.BaseType is INamedTypeSymbol baseType)
            {
                var configSourceType = baseType.TypeArguments[0];
                var configTargetType = baseType.TypeArguments[1];
                // 检查IMapConfig的类型
                if (SymbolEqualityComparer.Default.Equals(classSymbol, configSourceType))
                {
                    if (SymbolEqualityComparer.Default.Equals(targetType, configTargetType))
                    {
                        return mapConfigType;
                    }
                    var errorDiagnostic = Diagnostic.Create(
                        Descriptors.MapConfigTargetTypeDifferent,
                        attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                            attribute.ApplicationSyntaxReference.Span
                        ),
                        configTargetType.GetFullNameAndGeneric(),
                        targetType.GetFullNameAndGeneric()
                    );
                    ExecutionContext.ReportDiagnostic(errorDiagnostic);
                }
                else
                {
                    var errorDiagnostic = Diagnostic.Create(
                        Descriptors.MapConfigSourceTypeDifferent,
                        attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                            attribute.ApplicationSyntaxReference.Span
                        ),
                        configTargetType.GetFullNameAndGeneric(),
                        classSymbol.GetFullNameAndGeneric()
                    );
                    ExecutionContext.ReportDiagnostic(errorDiagnostic);
                }
            }
        }
        return null;
    }
}
