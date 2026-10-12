using HKW.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapperGeneratorDiagnosticTests
{
    [TestMethod]
    public void ReportsConfigConverterOverrideOnPropertyConverterAttribute()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), typeof(Config))]
            public sealed class Source
            {
                [MapProperty(typeof(Target), typeof(AttributeConverter))]
                public int Value { get; set; }
            }

            public sealed class Target
            {
                public int Value { get; set; }
            }

            public sealed class AttributeConverter : IMapConverter<int, int>
            {
                public int Convert(object source, int value) => value;
                public int ConvertBack(object target, int value) => value;
            }

            public sealed class Config : MapperConfig<Source, Target>
            {
                public Config(Source source, Target target) : base(source, target) { }

                public MapConfigPropertyConverter<int, int> ValueConverter { get; } =
                    new(nameof(Source.Value), (_, value) => value, (_, value) => value);
            }
            """;

        AssertOverrideDiagnostic(source, "MapProperty(typeof(Target), typeof(AttributeConverter))");
    }

    [TestMethod]
    public void ReportsConfigConverterOverrideOnCompositeConverterAttribute()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), typeof(Config))]
            public sealed class Source
            {
                [MapCompositeProperty(typeof(Target), typeof(AttributeConverter), nameof(Target.First), nameof(Target.Second))]
                public int Value { get; set; }
            }

            public sealed class Target
            {
                public int First { get; set; }
                public int Second { get; set; }
            }

            public sealed class AttributeConverter : ICompositeMapConverter<int, (int First, int Second)>
            {
                public (int First, int Second) Convert(object source, int value) => (value, value);
                public int ConvertBack(object target, (int First, int Second) values) => values.First;
            }

            public sealed class Config : MapperConfig<Source, Target>
            {
                public Config(Source source, Target target) : base(source, target) { }

                public MapConfigCompositePropertyConverter<int, (int First, int Second)> ValueConverter { get; } =
                    new(
                        nameof(Source.Value),
                        [nameof(Target.First), nameof(Target.Second)],
                        (_, value) => (value, value),
                        (_, values) => values.First
                    );
            }
            """;

        AssertOverrideDiagnostic(
            source,
            "MapCompositeProperty(typeof(Target), typeof(AttributeConverter), nameof(Target.First), nameof(Target.Second))"
        );
    }

    private static void AssertOverrideDiagnostic(string source, string expectedAttribute)
    {
        var diagnostics = RunGenerator(source).Where(x => x.Id == "MAP0028").ToArray();

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostics[0].Severity);
        Assert.AreEqual(
            expectedAttribute,
            diagnostics[0]
                .Location.SourceTree!
                .GetText()
                .ToString(diagnostics[0].Location.SourceSpan)
        );
    }

    private static IReadOnlyList<Diagnostic> RunGenerator(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(MapTargetAttribute).Assembly.Location));
        var compilation = CSharpCompilation.Create(
            "DiagnosticTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        var generatorType = typeof(MapTargetAttribute).Assembly.GetType(
            "HKW.Mapper.SourceGenerator.Generator",
            throwOnError: true
        )!;
        var generator = (IIncrementalGenerator)Activator.CreateInstance(generatorType, nonPublic: true)!;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: parseOptions
        );

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult().Diagnostics;
    }
}
