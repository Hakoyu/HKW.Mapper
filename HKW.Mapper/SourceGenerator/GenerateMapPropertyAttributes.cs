using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace HKW.HKWMapper.SourceGenerator;

internal class GenerateMapPropertyAttributes
{
    public static Compilation? Execute(GeneratorExecutionContext context)
    {
        var x = new GenerateMapPropertyAttributes() { ExecutionContext = context };
        return x.ParseContext();
    }

    public GeneratorExecutionContext ExecutionContext { get; private set; }

    private Compilation? ParseContext()
    {
        var compilation = ExecutionContext.Compilation;
        var mapInfoByNamespace = new Dictionary<
            INamespaceSymbol,
            HashSet<MapMethodAndAttributeName>
        >(SymbolEqualityComparer.Default);
        foreach (var compilationSyntaxTree in compilation.SyntaxTrees)
        {
            foreach (var spaceAndMethods in GetMapMethods(compilationSyntaxTree))
            {
                if (mapInfoByNamespace.TryGetValue(spaceAndMethods.Key, out var methods))
                {
                    foreach (var method in spaceAndMethods.Value)
                        methods.Add(method);
                }
                else
                    mapInfoByNamespace.Add(spaceAndMethods.Key, spaceAndMethods.Value);
            }
        }

        if (mapInfoByNamespace.Count == 0)
            return null;
        var stringStream = Generate(mapInfoByNamespace);

        // 将生成的代码添加到生成器中, 否则将无法正确识别创建的特性
        var attributeSourceText = SourceText.From(
            stringStream.ToString(),
            System.Text.Encoding.UTF8
        );
        ExecutionContext.AddSource($"MapAttributes.g.cs", attributeSourceText);
        var options = ((CSharpCompilation)ExecutionContext.Compilation).SyntaxTrees[0].Options;
        var attributeTree = CSharpSyntaxTree.ParseText(
            attributeSourceText,
            (CSharpParseOptions)options
        );
        return ExecutionContext.Compilation.AddSyntaxTrees(attributeTree);
    }

    private Dictionary<INamespaceSymbol, HashSet<MapMethodAndAttributeName>> GetMapMethods(
        SyntaxTree compilationSyntaxTree
    )
    {
        // (NameSpace, MapMethodAndAttributeNames)
        var mapMethods = new Dictionary<INamespaceSymbol, HashSet<MapMethodAndAttributeName>>(
            SymbolEqualityComparer.Default
        );
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
                    var parameters = attribute.GetAttributeParameterInfos();
                    if (parameters.Count == 0)
                        continue;
                    if (
                        parameters.TryGetValue(nameof(MapToAttribute.TargetType), out var value)
                        is false
                    )
                        continue;
                    parameters.TryGetValue(nameof(MapToAttribute.TargetName), out var targetData);
                    var targetType = (INamedTypeSymbol)value.Value!;
                    var targetName = targetData?.Value?.ToString() ?? targetType.Name;
                    if (
                        mapMethods.TryGetValue(classSymbol.ContainingNamespace, out var methods)
                        is false
                    )
                        methods = mapMethods[classSymbol.ContainingNamespace] = [];
                    var methodName = $"MapTo{targetName}";
                    var propertyAttributeName = $"{classSymbol.Name}{methodName}PropertyAttribute";
                    methods.Add(new(methodName, propertyAttributeName));
                    //if (methods.Add(methodName!) is false)
                    //{
                    //    var errorDiagnostic = Diagnostic.Create(
                    //        Descriptors.SameMethodName,
                    //        attribute.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                    //            attribute.ApplicationSyntaxReference.Span
                    //        ),
                    //        methodName
                    //    );
                    //    ExecutionContext.ReportDiagnostic(errorDiagnostic);
                    //}
                }
                else if (attributeName == typeof(MapFromAttribute).FullName)
                {
                    var parameters = attribute.GetAttributeParameterInfos();
                    if (
                        parameters.TryGetValue(nameof(MapToAttribute.TargetType), out var value)
                        is false
                    )
                        continue;
                    parameters.TryGetValue(nameof(MapToAttribute.TargetName), out var targetData);
                    var targetType = (INamedTypeSymbol)value.Value!;
                    var targetName = targetData?.Value?.ToString() ?? targetType.Name;
                    var methodName = $"MapFrom{targetName}";
                    var propertyAttributeName = $"{classSymbol.Name}{methodName}PropertyAttribute";
                    if (
                        mapMethods.TryGetValue(classSymbol.ContainingNamespace, out var methods)
                        is false
                    )
                        methods = mapMethods[classSymbol.ContainingNamespace] = [];
                    methods.Add(new(methodName!, propertyAttributeName));
                    //if (methods.Add(methodName!) is false)
                    //{
                    //    var errorDiagnostic = Diagnostic.Create(
                    //        Descriptors.SameMethodName,
                    //        classSymbol.Locations[0],
                    //        methodName
                    //    );
                    //    ExecutionContext.ReportDiagnostic(errorDiagnostic);
                    //}
                }
            }
        }
        return mapMethods;
    }

    public StringWriter Generate(
        Dictionary<INamespaceSymbol, HashSet<MapMethodAndAttributeName>> mapInfoByNamespace
    )
    {
        var stringStream = new StringWriter();
        var writer = new IndentedTextWriter(stringStream, "\t");
        writer.WriteLine("// <auto-generated>");
        writer.WriteLine("using System;");
        writer.WriteLine("#nullable enable");
        writer.WriteLine("#pragma warning disable CS1591");
        writer.WriteLine();
        foreach (var classAndMapMethod in mapInfoByNamespace)
        {
            writer.WriteLine($"namespace {classAndMapMethod.Key}");
            writer.WriteLine("{");
            writer.Indent++;
            foreach (var mapMethod in classAndMapMethod.Value)
            {
                writer.WriteLine(
                    @$"
/// <summary>
/// {mapMethod.AttributeName}
/// <para>This attribute is only used in <see cref=""{classAndMapMethod.Key}.MapperExtensions.{mapMethod.MethodName}""/></para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
{CommonData.GeneratedCodeAttribute}
public sealed class {mapMethod.AttributeName} : Attribute
{{
    /// <inheritdoc/>
    public {mapMethod.AttributeName}(string PropertyName)
    {{
        this.PropertyName = PropertyName;
    }}

    /// <inheritdoc/>
    public {mapMethod.AttributeName}(Type ConverterType)
    {{
        this.ConverterType = ConverterType;
    }}

    /// <inheritdoc cref=""HKW.HKWMapper.MapPropertyAttribute.PropertyName""/>
    public string? PropertyName {{ get; set; }}

    /// <inheritdoc cref=""HKW.HKWMapper.MapPropertyAttribute.ConverterType""/>
    public Type? ConverterType {{ get; set; }}

    /// <inheritdoc cref=""HKW.HKWMapper.MapPropertyAttribute.MapWhenRValueNotNullOrDefault""/>
    public bool {nameof(MapPropertyAttribute.MapWhenRValueNotNullOrDefault)} {{ get; set; }}

    /// <inheritdoc cref=""HKW.HKWMapper.MapPropertyAttribute.MapWhenLValueNullOrDefault""/>
    public bool {nameof(MapPropertyAttribute.MapWhenLValueNullOrDefault)} {{ get; set; }}
}}"
                );
            }

            writer.Indent--;
            writer.WriteLine("}");
        }
        return stringStream;
    }
}
