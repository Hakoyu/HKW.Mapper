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
        GeneratorExecution.Load(context).Execute();
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
