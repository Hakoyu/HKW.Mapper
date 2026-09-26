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
        context.RegisterSourceOutput(
            context.CompilationProvider,
            static (spc, compilation) =>
            {
                GeneratorHelper.Initialize(spc, compilation);
                var classInfos = new Dictionary<ClassDeclarationSyntax, ClassInfo>();
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    ParseSyntaxTree(compilation, syntaxTree, classInfos);
                }
                var compilation1 = MapAttributeGenerator.Generate(classInfos.Values);
                if (compilation1 == null)
                    return;

                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    ParseSyntaxTree(compilation1, syntaxTree, classInfos);
                }
            }
        );
    }

    private static void ParseSyntaxTree(
        Compilation compilation,
        SyntaxTree syntaxTree,
        Dictionary<ClassDeclarationSyntax, ClassInfo> classInfos
    )
    {
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var syntaxTreeInfo = new SyntaxTreeInfo(syntaxTree, semanticModel);
        var classSyntaxs = syntaxTree
            .GetRoot()
            .DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>();
        foreach (var classSyntax in classSyntaxs)
        {
            var classSymbol = (INamedTypeSymbol)
                ModelExtensions.GetDeclaredSymbol(syntaxTreeInfo.SemanticModel, classSyntax)!;
            if (classInfos.TryGetValue(classSyntax, out var classInfo))
            {
                classInfo.Update(classSymbol);

                MapperGenerator.Generate(classInfo);

                ClassSourceWriter.Execute(classInfo);
            }
            else
            {
                var info = ClassValidator(classSyntax, classSymbol);
                if (info is not null)
                    classInfos.Add(classSyntax, info);
            }
        }

        static ClassInfo? ClassValidator(
            ClassDeclarationSyntax classSyntax,
            INamedTypeSymbol classSymbol
        )
        {
            var atts = classSymbol.GetAttributes();
            if (atts.Length == 0)
                return null;

            // 获取映射目标
            var mapTargets = new List<AttributeData>();
            foreach (var att in atts)
            {
                if (att.AttributeClass!.GetFullName() == TypeFullNames.MapTargetAttribute)
                    mapTargets.Add(att);
            }
            if (mapTargets.Count == 0)
                return null;

            var classInfo = new ClassInfo(classSyntax, classSymbol, mapTargets);

            return classInfo;
        }
    }
}
