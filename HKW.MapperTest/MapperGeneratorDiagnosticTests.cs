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
                [MapProperty(typeof(Target))]
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
        Assert.AreEqual(1, RunGenerator(source).Count(x => x.Id == "MAP0029"));
    }

    [TestMethod]
    public void ReportsMissingInheritedTargetPropertyAsWarningOnMapTarget()
    {
        const string source = """
            using HKW.Mapper;

            public class BaseSource
            {
                public int Missing { get; set; }
            }

            [MapTarget(typeof(Target), MapParentProperties = true)]
            public sealed class Source : BaseSource;

            public sealed class Target;
            """;

        var diagnostic = RunGenerator(source).Single(x => x.Id == "MAP0004");

        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.AreEqual(
            "MapTarget(typeof(Target), MapParentProperties = true)",
            diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan)
        );
    }

    [TestMethod]
    public void ReportsTypeMismatchForInheritedProperty()
    {
        const string source = """
            using HKW.Mapper;

            public class BaseSource
            {
                public int Value { get; set; }
            }

            [MapTarget(typeof(Target), MapParentProperties = true)]
            public sealed class Source : BaseSource;

            public sealed class Target
            {
                public string Value { get; set; } = string.Empty;
            }
            """;

        var diagnostic = RunGenerator(source).Single(x => x.Id == "MAP0005");

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void ReportsMapIgnorePropertyOverrideOnceWhenAllMappingAttributesArePresent()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target))]
            [MapTarget(typeof(OtherTarget))]
            public sealed class Source
            {
                [MapIgnoreProperty]
                [MapProperty(typeof(Target))]
                [MapCompositeProperty(typeof(Target), typeof(object), "First", "Second")]
                public int Value { get; set; }
            }

            public sealed class Target;
            public sealed class OtherTarget;
            """;

        var diagnostics = RunGenerator(source).Where(x => x.Id == "MAP0028").ToArray();

        Assert.AreEqual(1, diagnostics.Length);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostics[0].Severity);
        Assert.AreEqual(
            "MapIgnoreProperty",
            diagnostics[0]
                .Location.SourceTree!
                .GetText()
                .ToString(diagnostics[0].Location.SourceSpan)
        );
        Assert.IsFalse(RunGenerator(source).Any(x => x.Id == "MAP0029"));
    }

    [TestMethod]
    public void ReportsMapIgnorePropertyOverrideForEachOtherMappingAttributeKind()
    {
        const string propertySource = """
            using HKW.Mapper;

            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapIgnoreProperty, MapProperty(typeof(Target))]
                public int Value { get; set; }
            }

            public sealed class Target;
            """;
        const string compositeSource = """
            using HKW.Mapper;

            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapIgnoreProperty, MapCompositeProperty(typeof(Target), typeof(object), "First", "Second")]
                public int Value { get; set; }
            }

            public sealed class Target;
            """;

        Assert.AreEqual(1, RunGenerator(propertySource).Count(x => x.Id == "MAP0028"));
        Assert.AreEqual(1, RunGenerator(compositeSource).Count(x => x.Id == "MAP0028"));
    }

    [TestMethod]
    public void ReportsMapCompositePropertyOverrideForSameTargetOnly()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target))]
                [MapCompositeProperty(typeof(Target), typeof(Converter), nameof(Target.First), nameof(Target.Second))]
                public int Value { get; set; }
            }

            public sealed class Target
            {
                public int First { get; set; }
                public int Second { get; set; }
            }

            public sealed class Converter : ICompositeMapConverter<int, (int First, int Second)>
            {
                public (int First, int Second) Convert(object source, int value) => (value, value);
                public int ConvertBack(object target, (int First, int Second) values) => values.First;
            }
            """;

        var diagnostics = RunGenerator(source);
        var overrideDiagnostic = diagnostics.Single(x => x.Id == "MAP0029");

        Assert.AreEqual(DiagnosticSeverity.Warning, overrideDiagnostic.Severity);
        Assert.IsFalse(diagnostics.Any(x => x.Id == "MAP0004"));
    }

    [TestMethod]
    public void DoesNotReportMapCompositePropertyOverrideForDifferentTargets()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(CompositeTarget))]
            [MapTarget(typeof(PropertyTarget))]
            public sealed class Source
            {
                [MapProperty(typeof(PropertyTarget))]
                [MapCompositeProperty(typeof(CompositeTarget), typeof(Converter), nameof(CompositeTarget.First), nameof(CompositeTarget.Second))]
                public int Value { get; set; }
            }

            public sealed class CompositeTarget
            {
                public int First { get; set; }
                public int Second { get; set; }
            }

            public sealed class PropertyTarget
            {
                public int Value { get; set; }
            }

            public sealed class Converter : ICompositeMapConverter<int, (int First, int Second)>
            {
                public (int First, int Second) Convert(object source, int value) => (value, value);
                public int ConvertBack(object target, (int First, int Second) values) => values.First;
            }
            """;

        Assert.IsFalse(RunGenerator(source).Any(x => x.Id == "MAP0029"));
    }

    [TestMethod]
    public void ReportsDuplicateTargetPropertyAcrossPropertyAndCompositeMappings()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target), nameof(Target.Shared))]
                public int Value { get; set; }

                [MapCompositeProperty(typeof(Target), typeof(Converter), nameof(Target.Shared), nameof(Target.Other))]
                public int CompositeValue { get; set; }
            }

            public sealed class Target
            {
                public int Shared { get; set; }
                public int Other { get; set; }
            }

            public sealed class Converter : ICompositeMapConverter<int, (int Shared, int Other)>
            {
                public (int Shared, int Other) Convert(object source, int value) => (value, value);
                public int ConvertBack(object target, (int Shared, int Other) values) => values.Shared;
            }
            """;

        var diagnostic = RunGenerator(source).Single(x => x.Id == "MAP0002");

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [TestMethod]
    public void ReportsInaccessibleSourceGetterForToMapping()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                public int Value { private get; set; }
            }

            public sealed class Target
            {
                public int Value { get; set; }
            }
            """;

        var diagnostic = RunGenerator(source).Single(x => x.Id == "MAP0030");

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        StringAssert.Contains(diagnostic.GetMessage(), "Value");
        StringAssert.Contains(diagnostic.GetMessage(), "getter");
    }

    [TestMethod]
    public void ReportsMissingSourceGetterForToMapping()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                public int Value { set { } }
            }

            public sealed class Target
            {
                public int Value { get; set; }
            }
            """;

        StringAssert.Contains(
            RunGenerator(source).Single(x => x.Id == "MAP0030").GetMessage(),
            "getter"
        );
    }

    [TestMethod]
    public void ReportsInaccessibleAndMissingSourceSetterForFromMapping()
    {
        const string inaccessibleSource = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), Direction = MapDirections.From)]
            public sealed class Source
            {
                public int Value { get; private set; }
            }

            public sealed class Target
            {
                public int Value { get; set; }
            }
            """;
        const string missingSource = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), Direction = MapDirections.From)]
            public sealed class Source
            {
                public int Value { get; }
            }

            public sealed class Target
            {
                public int Value { get; set; }
            }
            """;

        StringAssert.Contains(
            RunGenerator(inaccessibleSource).Single(x => x.Id == "MAP0030").GetMessage(),
            "setter"
        );
        StringAssert.Contains(
            RunGenerator(missingSource).Single(x => x.Id == "MAP0030").GetMessage(),
            "setter"
        );
    }

    [TestMethod]
    public void DoesNotCheckUnusedSourceAccessorForSingleDirectionMapping()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(ToTarget), Direction = MapDirections.To)]
            [MapTarget(typeof(FromTarget), Direction = MapDirections.From)]
            public sealed class Source
            {
                [MapProperty(typeof(ToTarget))]
                public int ToValue { get; private set; }

                [MapProperty(typeof(FromTarget))]
                public int FromValue { private get; set; }
            }

            public sealed class ToTarget
            {
                public int ToValue { get; set; }
            }

            public sealed class FromTarget
            {
                public int FromValue { get; set; }
            }
            """;

        Assert.IsFalse(RunGenerator(source).Any(x => x.Id == "MAP0030"));
    }

    [TestMethod]
    public void ReportsOnlySourceGetterFirstForBothMapping()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                public int Value { private get; private set; }
            }

            public sealed class Target
            {
                public int Value { get; set; }
            }
            """;

        var diagnostics = RunGenerator(source).Where(x => x.Id == "MAP0030").ToArray();

        Assert.AreEqual(1, diagnostics.Length);
        StringAssert.Contains(diagnostics[0].GetMessage(), "getter");
    }

    [TestMethod]
    public void ReportsSourceAccessibilityBeforeTargetAccessibility()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                public int Value { private get; set; }
            }

            public sealed class Target
            {
                public int Value { get; private set; }
            }
            """;

        var diagnostics = RunGenerator(source);

        Assert.AreEqual(1, diagnostics.Count(x => x.Id == "MAP0030"));
        Assert.IsFalse(diagnostics.Any(x => x.Id == "MAP0007"));
    }

    [TestMethod]
    public void AcceptsAccessibleSourceAccessors()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                internal int Value { get; set; }
            }

            public sealed class Target
            {
                internal int Value { get; set; }
            }
            """;

        Assert.IsFalse(RunGenerator(source).Any(x => x.Id == "MAP0030"));
    }

    private static void AssertOverrideDiagnostic(string source, string expectedAttribute)
    {
        var diagnostics = RunGenerator(source).Where(x => x.Id == "MAP0027").ToArray();

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
