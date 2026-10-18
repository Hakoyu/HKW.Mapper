using System.Collections.Immutable;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HKW.Mapper.SourceGenerator;

[Generator]
internal partial class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GeneratorHelper.Initialize();
        var mapTargetCandidates = GetCandidates(context, typeof(MapTargetAttribute).GetFullName());
        var mapSourceCandidates = GetCandidates(context, typeof(MapSourceAttribute).GetFullName());

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(mapTargetCandidates.Combine(mapSourceCandidates)),
            static (spc, input) =>
            {
                var compilation = input.Left;
                var classInfos = new List<ClassInfo>();
                var declaredNames = new Dictionary<INamedTypeSymbol, HashSet<string>>(
                    SymbolEqualityComparer.Default
                );

                foreach (var candidate in input.Right.Left)
                {
                    var attributes = candidate.Symbol.GetAttributes()
                        .Where(x => x.AttributeClass?.GetGlobalFullName() == TypeFullNames.MapTargetAttribute)
                        .ToList();
                    if (attributes.Count == 0)
                        continue;
                    classInfos.Add(new(spc, compilation, candidate.Syntax, candidate.Symbol, attributes));
                    declaredNames[candidate.Symbol] = new HashSet<string>(
                        attributes.Select(GetTargetName),
                        StringComparer.Ordinal
                    );
                }

                foreach (var candidate in input.Right.Right)
                {
                    var attributes = candidate.Symbol.GetAttributes()
                        .Where(x => x.AttributeClass?.GetGlobalFullName() == TypeFullNames.MapSourceAttribute)
                        .ToList();
                    if (attributes.Count == 0)
                        continue;
                    if (IsValidMapSourceContainer(candidate) is false)
                    {
                        foreach (var attribute in attributes)
                            spc.ReportDiagnostic(
                                Diagnostic.Create(
                                    Descriptors.InvalidMapSourceContainer,
                                    GetLocation(attribute)
                                )
                            );
                        continue;
                    }

                    foreach (var sourceGroup in attributes.GroupBy(GetSourceType, SymbolEqualityComparer.Default))
                    {
                        if (sourceGroup.Key is not INamedTypeSymbol sourceType)
                            continue;
                        if (declaredNames.TryGetValue(sourceType, out var names) is false)
                        {
                            names = new(StringComparer.Ordinal);
                            declaredNames.Add(sourceType, names);
                        }
                        var validAttributes = new List<AttributeData>();
                        foreach (var attribute in sourceGroup)
                        {
                            var targetName = GetTargetName(attribute);
                            if (names.Add(targetName) is false)
                            {
                                spc.ReportDiagnostic(
                                    Diagnostic.Create(
                                        Descriptors.DuplicateMapSourceTargetName,
                                        GetLocation(attribute),
                                        targetName,
                                        sourceType.GetName()
                                    )
                                );
                                continue;
                            }
                            validAttributes.Add(attribute);
                        }
                        if (validAttributes.Count == 0)
                            continue;
                        var sourceSyntax = sourceType.DeclaringSyntaxReferences
                            .Select(x => x.GetSyntax())
                            .OfType<TypeDeclarationSyntax>()
                            .FirstOrDefault();
                        classInfos.Add(
                            new(
                                spc,
                                compilation,
                                sourceSyntax,
                                sourceType,
                                validAttributes,
                                candidate.Symbol,
                                candidate.Syntax
                            )
                        );
                    }
                }

                var mapTargetDic = new Dictionary<INamedTypeSymbol, HashSet<MapTargetInfo>>(
                    SymbolEqualityComparer.Default
                );
                foreach (var sourceGroup in classInfos.GroupBy(x => x.ClassSymbol, SymbolEqualityComparer.Default))
                {
                    if (sourceGroup.Key is INamedTypeSymbol sourceType)
                        mapTargetDic[sourceType] = new(sourceGroup.SelectMany(x => x.MapTargets));
                }

                foreach (var classInfo in classInfos)
                {
                    MapperGenerator.Generate(classInfo, mapTargetDic);
                    ClassSourceWriter.Execute(classInfo);
                }
            }
        );
    }

    private static IncrementalValueProvider<ImmutableArray<Candidate>> GetCandidates(
        IncrementalGeneratorInitializationContext context,
        string attributeName
    ) =>
        context.SyntaxProvider.ForAttributeWithMetadataName(
                attributeName,
                static (node, _) => node is TypeDeclarationSyntax,
                static (attributeContext, _) =>
                    attributeContext.TargetSymbol is INamedTypeSymbol symbol
                    && attributeContext.TargetNode is TypeDeclarationSyntax syntax
                        ? new Candidate(syntax, symbol)
                        : null
            )
            .Where(static candidate => candidate is not null)
            .Select(static (candidate, _) => candidate!)
            .Collect();

    private static bool IsValidMapSourceContainer(Candidate candidate) =>
        candidate.Symbol.TypeKind == TypeKind.Class
        && candidate.Symbol.ContainingType is null
        && candidate.Symbol.TypeParameters.Length == 0
        && candidate.Symbol.IsStatic
        && candidate.Syntax.Modifiers.Any(SyntaxKind.PartialKeyword);

    private static INamedTypeSymbol? GetSourceType(AttributeData attribute) =>
        attribute.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;

    private static string GetTargetName(AttributeData attribute)
    {
        var explicitName = attribute.NamedArguments.FirstOrDefault(x => x.Key == nameof(MapTargetAttribute.TargetName)).Value.Value as string;
        var targetIndex = attribute.AttributeClass?.GetGlobalFullName() == TypeFullNames.MapSourceAttribute ? 1 : 0;
        var targetType = attribute.ConstructorArguments.Length > targetIndex
            ? attribute.ConstructorArguments[targetIndex].Value as INamedTypeSymbol
            : null;
        return string.IsNullOrWhiteSpace(explicitName) ? targetType?.Name ?? string.Empty : explicitName!;
    }

    private static Location GetLocation(AttributeData attribute) =>
        attribute.ApplicationSyntaxReference?.SyntaxTree.GetLocation(
            attribute.ApplicationSyntaxReference.Span
        ) ?? Location.None;

    private sealed class Candidate
    {
        public Candidate(TypeDeclarationSyntax syntax, INamedTypeSymbol symbol)
        {
            Syntax = syntax;
            Symbol = symbol;
        }

        public TypeDeclarationSyntax Syntax { get; }
        public INamedTypeSymbol Symbol { get; }
    }
}
