using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.HKWMapper.SourceGenerator;

[Generator]
internal partial class Generator : ISourceGenerator
{
    public Generator() { }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = GenerateMapPropertyAttributes.Execute(context);
        if (compilation is null)
            return;

        var info = GenerateMappers.Execute(context, compilation);

        //GenerateConfigs.Execute(context, info.Configs);
        //GenerateConverters.Execute(context, info.Converters);
    }
}

class SyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = [];

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (
            syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
            && classDeclarationSyntax.BaseList?.Types.Any(type =>
                type.ToString().Contains(nameof(MapperConfig<int, int>) + "<")
            )
                is true
        )
        {
            CandidateClasses.Add(classDeclarationSyntax);
        }
    }
}
