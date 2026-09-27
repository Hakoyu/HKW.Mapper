using System.CodeDom.Compiler;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace HKW.HKWMapper.SourceGenerator;

[Generator]
internal partial class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                TypeFullNames.MapTargetAttribute.Substring(TypeFullNames._global.Length),
                static (node, _) => node is ClassDeclarationSyntax,
                static (attributeContext, _) =>
                {
                    return attributeContext.TargetSymbol is INamedTypeSymbol symbol
                        && attributeContext.TargetNode is ClassDeclarationSyntax syntax
                        ? new Candidate(syntax, symbol)
                        : null;
                }
            )
            .Where(static candidate => candidate is not null)
            .Select(static (candidate, _) => candidate!)
            .Collect();
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(candidates),
            static (spc, input) =>
            {
                var compilation = input.Left;
                GeneratorHelper.Initialize(spc, compilation);
                var classInfos = new List<ClassInfo>();
                foreach (var candidate in input.Right)
                {
                    if (ClassValidator(candidate.Syntax, candidate.Symbol) is { } classInfo)
                        classInfos.Add(classInfo);
                }
                var allMapTargets = classInfos
                    .SelectMany(i => i.MapTargets)
                    .GroupBy(GetMapTargetKey)
                    .ToDictionary(group => group.Key, group => group.First());
                foreach (var classInfo in classInfos)
                {
                    MapperGenerator.Generate(classInfo, allMapTargets);
                    ClassSourceWriter.Execute(classInfo);
                }
            }
        );
    }

    private static string GetMapTargetKey(MapTargetInfo mapTarget)
    {
        return $"{mapTarget.SourceType.GetFullName()}->{mapTarget.TargetType.GetFullName()}";
    }

    private sealed class Candidate
    {
        public Candidate(ClassDeclarationSyntax syntax, INamedTypeSymbol symbol)
        {
            Syntax = syntax;
            Symbol = symbol;
        }

        public ClassDeclarationSyntax Syntax { get; }
        public INamedTypeSymbol Symbol { get; }
    }

    private static ClassInfo? ClassValidator(ClassDeclarationSyntax classSyntax, INamedTypeSymbol classSymbol)
    {
        var atts = classSymbol.GetAttributes();
        var mapTargets = new List<AttributeData>();
        foreach (var att in atts)
        {
            if (att.AttributeClass!.GetFullName() == TypeFullNames.MapTargetAttribute)
                mapTargets.Add(att);
        }
        return mapTargets.Count == 0 ? null : new ClassInfo(classSyntax, classSymbol, mapTargets);
    }
}
