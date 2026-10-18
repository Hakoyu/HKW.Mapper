using HKW.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HKW.MapperTest;

[TestClass]
public sealed class DescriptorsTests
{
    [TestMethod]
    public void MAP0001_ReportsDuplicateTargetName()
    {
        AssertDiagnostic(
            "MAP0001",
            DiagnosticSeverity.Error,
            "MapTarget(typeof(TargetB)",
            ["Same", "already in use", "different target name"],
            """
            [MapTarget(typeof(TargetA), TargetName = "Same")]
            [MapTarget(typeof(TargetB), TargetName = "Same")]
            public sealed class Source;
            public sealed class TargetA;
            public sealed class TargetB;
            """
        );
    }

    [TestMethod]
    public void MAP0002_ReportsMultipleSourcePropertiesForSameTargetProperty()
    {
        AssertDiagnostic(
            "MAP0002",
            DiagnosticSeverity.Error,
            "Second",
            ["First", "Second", "Value"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target), nameof(Target.Value))]
                public int First { get; set; }
                [MapProperty(typeof(Target), nameof(Target.Value))]
                public int Second { get; set; }
            }
            public sealed class Target { public int Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0002_DoesNotReportDuplicateTargetsForAttributeConverters()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), MapFields = true)]
            public sealed class Source
            {
                [MapProperty(typeof(Target), nameof(Target.Property), typeof(Converter))]
                public int FirstProperty { get; set; }

                [MapProperty(typeof(Target), nameof(Target.Property), typeof(Converter))]
                public int SecondProperty { get; set; }

                [MapField(typeof(Target), nameof(Target.Field), typeof(Converter))]
                public int FirstField;

                [MapField(typeof(Target), nameof(Target.Field), typeof(Converter))]
                public int SecondField;
            }

            public sealed class Target
            {
                public int Property { get; set; }
                public int Field;
            }

            public sealed class Converter : IMapConverter<int, int>
            {
                public int Convert(int value, MapConvertArgs args) => value;
                public int ConvertBack(int value, MapConvertArgs args) => value;
            }
            """;

        Assert.IsFalse(RunGenerator(source).Any(d => d.Id == "MAP0002"));
    }

    [TestMethod]
    public void MAP0002_DoesNotReportDuplicateTargetsForConfigConverters()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), typeof(Config), MapFields = true)]
            public sealed class Source
            {
                public int FirstProperty { get; set; }
                public int SecondProperty { get; set; }
                public int FirstField;
                public int SecondField;
            }

            public sealed class Target
            {
                public int Property { get; set; }
                public int Field;
            }

            public sealed class Config : MapperConfig<Source, Target>
            {
                public Config(Source source, Target target) : base(source, target) { }

                public MapConfigPropertyConverter<int, int> FirstPropertyConverter { get; } =
                    new(nameof(Source.FirstProperty), nameof(Target.Property), (value, _) => value, (value, _) => value);

                public MapConfigPropertyConverter<int, int> SecondPropertyConverter { get; } =
                    new(nameof(Source.SecondProperty), nameof(Target.Property), (value, _) => value, (value, _) => value);

                public MapConfigFieldConverter<int, int> FirstFieldConverter { get; } =
                    new(nameof(Source.FirstField), nameof(Target.Field), (value, _) => value, (value, _) => value);

                public MapConfigFieldConverter<int, int> SecondFieldConverter { get; } =
                    new(nameof(Source.SecondField), nameof(Target.Field), (value, _) => value, (value, _) => value);
            }
            """;

        Assert.IsFalse(RunGenerator(source).Any(d => d.Id == "MAP0002"));
    }

    [TestMethod]
    public void MAP0003_ReportsInvalidMapperConfigType()
    {
        AssertDiagnostic(
            "MAP0003",
            DiagnosticSeverity.Error,
            "MapTarget",
            ["BadConfig", "MapperConfig"],
            """
            [MapTarget(typeof(Target), typeof(BadConfig))]
            public sealed class Source;
            public sealed class Target;
            public sealed class BadConfig;
            """
        );
    }

    [TestMethod]
    public void MAP0004_ReportsMissingTargetProperty()
    {
        AssertDiagnostic(
            "MAP0004",
            DiagnosticSeverity.Warning,
            "Value",
            ["Target.Value", "does not exist"],
            Basic("public int Value { get; set; }", string.Empty)
        );
    }

    [TestMethod]
    public void MAP0005_ReportsPropertyTypeMismatch()
    {
        AssertDiagnostic(
            "MAP0005",
            DiagnosticSeverity.Error,
            "Value",
            ["Target.Value", "incompatible type"],
            Basic(
                "public int Value { get; set; }",
                "public string Value { get; set; } = string.Empty;"
            )
        );
    }

    [TestMethod]
    public void MAP0006_ReportsStaticTargetProperty()
    {
        AssertDiagnostic(
            "MAP0006",
            DiagnosticSeverity.Error,
            "Value",
            ["Value", "static"],
            Basic("public int Value { get; set; }", "public static int Value { get; set; }")
        );
    }

    [TestMethod]
    public void MAP0007_ReportsInaccessibleTargetSetter()
    {
        AssertDiagnostic(
            "MAP0007",
            DiagnosticSeverity.Error,
            "Value",
            ["Value", "setter", "accessibility"],
            Basic(
                "public int Value { get; set; }",
                "public int Value { get; private set; }",
                "Direction = MapDirections.To"
            )
        );
    }

    [TestMethod]
    public void MAP0008_ReportsReferenceTypeWithoutConverter()
    {
        AssertDiagnostic(
            "MAP0008",
            DiagnosticSeverity.Error,
            "Value",
            ["Source.Value", "reference type"],
            Basic(
                "public Item Value { get; set; } = new();",
                "public Item Value { get; set; } = new();"
            ) + "\npublic sealed class Item;"
        );
    }

    [TestMethod]
    public void MAP0009_ReportsInvalidPropertyConverterType()
    {
        AssertDiagnostic(
            "MAP0009",
            DiagnosticSeverity.Error,
            "MapProperty",
            ["Target.Value", "IMapConverter"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target), typeof(BadConverter))]
                public int Value { get; set; }
            }
            public sealed class Target { public int Value { get; set; } }
            public sealed class BadConverter;
            """
        );
    }

    [TestMethod]
    public void MAP0010_ReportsIncompatiblePropertyConverterTypes()
    {
        AssertDiagnostic(
            "MAP0010",
            DiagnosticSeverity.Error,
            "MapProperty",
            ["string", "int", "incompatible"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target), typeof(BadConverter))]
                public int Value { get; set; }
            }
            public sealed class Target { public int Value { get; set; } }
            public sealed class BadConverter : IMapConverter<string, string>
            {
                public string Convert(string value, MapConvertArgs args) => value;
                public string ConvertBack(string value, MapConvertArgs args) => value;
            }
            """
        );
    }

    [TestMethod]
    public void MAP0011_ReportsInvalidConfigConverterPropertyName()
    {
        AssertDiagnostic(
            "MAP0011",
            DiagnosticSeverity.Error,
            "ValueConverter",
            ["source member name", "non-empty string"],
            Config(
                "public MapConfigPropertyConverter<int, int> ValueConverter { get; } = new(\"\", (value, _) => value, (value, _) => value);"
            )
        );
    }

    [TestMethod]
    public void MAP0012_ReportsMissingConfigActionPropertyName()
    {
        AssertDiagnostic(
            "MAP0012",
            DiagnosticSeverity.Error,
            "Run",
            ["action member name", "non-empty string", "action mode"],
            Config("[MapToConfigAction(MapConfigActionMode.BeforeProperty)] public void Run() { }")
        );
    }

    [TestMethod]
    public void MAP0013_ReportsDuplicateConfigActionsForProperty()
    {
        AssertDiagnostic(
            "MAP0013",
            DiagnosticSeverity.Error,
            "Second",
            ["one map config action", "member", "action mode"],
            Config(
                "[MapToConfigAction(MapConfigActionMode.BeforeProperty, nameof(Source.Value))] public void First() { }\n"
                    + "[MapToConfigAction(MapConfigActionMode.BeforeProperty, nameof(Source.Value))] public void Second() { }"
            )
        );
    }

    [TestMethod]
    public void MAP0014_ReportsConfigActionWithParameters()
    {
        AssertDiagnostic(
            "MAP0014",
            DiagnosticSeverity.Error,
            "Run",
            ["cannot declare parameters"],
            Config("[MapToConfigAction(MapConfigActionMode.Start)] public void Run(int value) { }")
        );
    }

    [TestMethod]
    public void MAP0015_ReportsInaccessibleConfigAction()
    {
        AssertDiagnostic(
            "MAP0015",
            DiagnosticSeverity.Error,
            "Run",
            ["internal or public"],
            Config("[MapToConfigAction(MapConfigActionMode.Start)] private void Run() { }")
        );
    }

    [TestMethod]
    public void MAP0016_ReportsPropertyMappingForUnconfiguredTarget()
    {
        AssertDiagnostic(
            "MAP0016",
            DiagnosticSeverity.Warning,
            "MapProperty",
            ["Other", "does not match any MapTarget"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Other))]
                public int Value { get; set; }
            }
            public sealed class Target;
            public sealed class Other;
            """
        );
    }

    [TestMethod]
    public void MAP0017_ReportsMultiplePropertyMappingsForSameTarget()
    {
        AssertDiagnostic(
            "MAP0017",
            DiagnosticSeverity.Error,
            "Value",
            ["Value", "more than one"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target))]
                [MapProperty(typeof(Target), nameof(Target.Value))]
                public int Value { get; set; }
            }
            public sealed class Target { public int Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0018_ReportsMapperConfigTypeMismatch()
    {
        AssertDiagnostic(
            "MAP0018",
            DiagnosticSeverity.Error,
            "MapTarget",
            ["OtherSource", "Source", "Target"],
            """
            [MapTarget(typeof(Target), typeof(Config))]
            public sealed class Source;
            public sealed class OtherSource;
            public sealed class Target;
            public sealed class Config : MapperConfig<OtherSource, Target>
            {
                public Config(OtherSource source, Target target) : base(source, target) { }
            }
            """
        );
    }

    [TestMethod]
    public void MAP0019_ReportsInvalidCompositeConverterType()
    {
        AssertDiagnostic(
            "MAP0019",
            DiagnosticSeverity.Error,
            "MapCompositeProperty",
            ["BadConverter", "IMapCompositeConverter"],
            Composite(
                "BadConverter",
                "nameof(Target.First), nameof(Target.Second)",
                "public sealed class BadConverter;"
            )
        );
    }

    [TestMethod]
    public void MAP0020_ReportsNonTupleCompositeConverterTarget()
    {
        AssertDiagnostic(
            "MAP0020",
            DiagnosticSeverity.Error,
            "MapCompositeProperty",
            ["ScalarConverter", "ValueTuple"],
            Composite(
                "ScalarConverter",
                "nameof(Target.First), nameof(Target.Second)",
                """
                public sealed class ScalarConverter : IMapCompositeConverter<int, int>
                {
                    public int Convert(int value, MapCompositeConvertArgs args) => value;
                    public int ConvertBack(int values, MapCompositeConvertArgs args) => values;
                }
                """
            )
        );
    }

    [TestMethod]
    public void MAP0021_ReportsCompositeTargetCountMismatch()
    {
        AssertDiagnostic(
            "MAP0021",
            DiagnosticSeverity.Error,
            "MapCompositeProperty",
            ["3 target members", "2 elements"],
            Composite(
                "TupleConverter",
                "nameof(Target.First), nameof(Target.Second), nameof(Target.Third)",
                TupleConverter("int", "int"),
                "public int Third { get; set; }"
            )
        );
    }

    [TestMethod]
    public void MAP0022_ReportsCompositeTargetTypeMismatch()
    {
        AssertDiagnostic(
            "MAP0022",
            DiagnosticSeverity.Error,
            "MapCompositeProperty",
            ["Second", "int", "element 2", "string"],
            Composite(
                "MixedConverter",
                "nameof(Target.First), nameof(Target.Second)",
                TupleConverter("int", "string", "MixedConverter")
            )
        );
    }

    [TestMethod]
    public void MAP0023_ReportsDuplicateCompositeTargetProperties()
    {
        AssertDiagnostic(
            "MAP0023",
            DiagnosticSeverity.Error,
            "MapCompositeProperty",
            ["two unique", "non-empty"],
            Composite(
                "TupleConverter",
                "nameof(Target.First), nameof(Target.First)",
                TupleConverter("int", "int")
            )
        );
    }

    [TestMethod]
    public void MAP0024_ReportsCompositeSourceTypeMismatch()
    {
        AssertDiagnostic(
            "MAP0024",
            DiagnosticSeverity.Error,
            "MapCompositeProperty",
            ["string", "int", "source type"],
            Composite(
                "StringSourceConverter",
                "nameof(Target.First), nameof(Target.Second)",
                """
                public sealed class StringSourceConverter : IMapCompositeConverter<string, (int, int)>
                {
                    public (int, int) Convert(string value, MapCompositeConvertArgs args) => default;
                    public string ConvertBack((int, int) values, MapCompositeConvertArgs args) => string.Empty;
                }
                """
            )
        );
    }

    [TestMethod]
    public void MAP0025_ReportsDuplicateConfigCompositeTargetProperties()
    {
        AssertDiagnostic(
            "MAP0025",
            DiagnosticSeverity.Error,
            "ValueConverter",
            ["source member", "two unique target members"],
            Config(
                "public MapConfigCompositePropertyConverter<int, (int, int)> ValueConverter { get; } = "
                    + "new(nameof(Source.Value), [nameof(Target.First), nameof(Target.First)], (value, _) => (value, value), (values, _) => values.Item1);",
                "public int First { get; set; } public int Second { get; set; }"
            )
        );
    }

    [TestMethod]
    public void MAP0026_ReportsNonInlineConfigCompositeConverter()
    {
        AssertDiagnostic(
            "MAP0026",
            DiagnosticSeverity.Error,
            "ValueConverter",
            ["inline object initializer", "inline target member array"],
            Config(
                "public MapConfigCompositePropertyConverter<int, (int, int)> ValueConverter => "
                    + "new(nameof(Source.Value), [nameof(Target.First), nameof(Target.Second)], (value, _) => (value, value), (values, _) => values.Item1);",
                "public int First { get; set; } public int Second { get; set; }"
            )
        );
    }

    [TestMethod]
    public void MAP0027_ReportsConfigConverterOverridingAttributeConverter()
    {
        AssertDiagnostic(
            "MAP0027",
            DiagnosticSeverity.Warning,
            "MapProperty",
            ["config converter", "redundant mapping attribute"],
            Config(
                "public MapConfigPropertyConverter<int, int> ValueConverter { get; } = new(nameof(Source.Value), (value, _) => value, (value, _) => value);",
                "public int Value { get; set; }",
                "[MapProperty(typeof(Target), typeof(AttributeConverter))] public int Value { get; set; }",
                """
                public sealed class AttributeConverter : IMapConverter<int, int>
                {
                    public int Convert(int value, MapConvertArgs args) => value;
                    public int ConvertBack(int value, MapConvertArgs args) => value;
                }
                """
            )
        );
    }

    [TestMethod]
    public void MAP0028_ReportsIgnoreAttributeOverridingMappingAttribute()
    {
        AssertDiagnostic(
            "MAP0028",
            DiagnosticSeverity.Warning,
            "MapIgnoreProperty",
            ["ignore attribute", "remove other mapping attributes", "member"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapIgnoreProperty, MapProperty(typeof(Target))]
                public int Value { get; set; }
            }
            public sealed class Target;
            """
        );
    }

    [TestMethod]
    public void MAP0029_ReportsCompositeAttributeOverridingPropertyAttribute()
    {
        AssertDiagnostic(
            "MAP0029",
            DiagnosticSeverity.Warning,
            "MapCompositeProperty",
            ["composite mapping attribute", "regular mapping attribute"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target))]
                [MapCompositeProperty(typeof(Target), typeof(TupleConverter), nameof(Target.First), nameof(Target.Second))]
                public int Value { get; set; }
            }
            public sealed class Target { public int First { get; set; } public int Second { get; set; } }
            """ + TupleConverter("int", "int")
        );
    }

    [TestMethod]
    public void MAP0030_ReportsInaccessibleSourceGetter()
    {
        AssertDiagnostic(
            "MAP0030",
            DiagnosticSeverity.Error,
            "Value",
            ["Value", "getter", "accessibility"],
            Basic(
                "public int Value { private get; set; }",
                "public int Value { get; set; }",
                "Direction = MapDirections.To"
            )
        );
    }

    [TestMethod]
    public void MAP0030_UsesMemberDirectionForAccessorDiagnostics()
    {
        AssertDiagnostic(
            "MAP0030",
            DiagnosticSeverity.Error,
            "Value",
            ["Value", "getter", "accessibility"],
            Basic(
                "[MapProperty(typeof(Target), Direction = MapDirections.To)] public int Value { private get; set; }",
                "public int Value { get; set; }"
            )
        );
    }

    [TestMethod]
    public void MAP0031_ReportsReadonlyFieldForBothDirections()
    {
        AssertDiagnostic(
            "MAP0031",
            DiagnosticSeverity.Error,
            "Value",
            ["Value", "From", "written"],
            """
            [MapTarget(typeof(Target), MapFields = true)]
            public sealed class Source
            {
                public readonly int Value;
            }
            public sealed class Target { public int Value; }
            """
        );
    }

    [TestMethod]
    public void MAP0031_ReportsReadonlyTargetFieldForMapTo()
    {
        AssertDiagnostic(
            "MAP0031",
            DiagnosticSeverity.Error,
            "Value",
            ["Value", "To", "written"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.To, MapFields = true)]
            public sealed class Source { public int Value; }
            public sealed class Target { public readonly int Value; }
            """
        );
    }

    [TestMethod]
    public void CorrectedDescriptorsUseAccurateMemberTerminology()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                public int Value { get; set; }
                public ReferenceValue Reference { get; set; } = new();
            }

            public sealed class Target
            {
                public int Value { get; private set; }
                public ReferenceValue Reference { get; set; } = new();
            }

            public sealed class ReferenceValue;
            """;

        var diagnostics = RunGenerator(source);
        var targetAccessor = diagnostics.Single(d => d.Id == "MAP0007");
        var referenceType = diagnostics.Single(d => d.Id == "MAP0008");

        Assert.AreEqual(
            "Target member accessor is inaccessible",
            targetAccessor.Descriptor.Title.ToString()
        );
        StringAssert.Contains(targetAccessor.GetMessage(), "Target member 'Value'");
        Assert.AreEqual(
            "Source member is a reference type",
            referenceType.Descriptor.Title.ToString()
        );
        StringAssert.Contains(referenceType.GetMessage(), "MapPropertyType.Reference");
    }

    private static void AssertDiagnostic(
        string id,
        DiagnosticSeverity severity,
        string locationText,
        string[] messageParts,
        string source
    )
    {
        var diagnostics = RunGenerator(WithUsings(source));
        var diagnostic = diagnostics.Single(d => d.Id == id);

        Assert.AreEqual(severity, diagnostic.Severity, diagnostic.ToString());
        Assert.AreNotEqual(Location.None, diagnostic.Location, diagnostic.ToString());
        StringAssert.Contains(GetLocationText(diagnostic), locationText, diagnostic.ToString());
        foreach (var messagePart in messageParts)
            StringAssert.Contains(diagnostic.GetMessage(), messagePart, diagnostic.ToString());
    }

    [TestMethod]
    public void MAP0033_ReportsMissingNestedSourcePathSegment()
    {
        AssertDiagnostic(
            "MAP0033",
            DiagnosticSeverity.Warning,
            "Node",
            ["Source.Node.Missing.Value", "does not exist"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target), nameof(Target.Value), SourcePath = "Node.Missing.Value")]
                public Node Node { get; set; } = new();
            }
            public sealed class Node { public int Value { get; set; } }
            public sealed class Target { public int Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0030_ReportsUnreadableIntermediateSourcePathSegment()
    {
        AssertDiagnostic(
            "MAP0030",
            DiagnosticSeverity.Error,
            "Anchor",
            ["Anchor", "getter", "accessibility"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                [MapProperty(typeof(Target), nameof(Target.Value), SourcePath = "Node.Value")]
                public int Anchor { get; set; }

                [MapIgnoreProperty]
                public Node Node { private get; set; } = new();
            }
            public sealed class Node { public int Value { get; set; } }
            public sealed class Target { public int Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0035_ReportsValueTypePropertyInWritableSourcePath()
    {
        AssertDiagnostic(
            "MAP0035",
            DiagnosticSeverity.Error,
            "Anchor",
            ["Point", "value type", "writable"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.From)]
            public sealed class Source
            {
                [MapProperty(typeof(Target), nameof(Target.Value), SourcePath = "Point.Value")]
                public int Anchor { get; set; }

                [MapIgnoreProperty]
                public Point Point { get; set; }
            }
            public struct Point { public int Value; }
            public sealed class Target { public int Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0031_ReportsReadonlySourcePathFieldForMapFrom()
    {
        AssertDiagnostic(
            "MAP0031",
            DiagnosticSeverity.Error,
            "Node",
            ["Node.Value", "From", "written"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.From, MapFields = true)]
            public sealed class Source
            {
                [MapField(typeof(Target), nameof(Target.Value), SourcePath = "Node.Value")]
                public Node Node = new();
            }
            public sealed class Node { public readonly int Value; }
            public sealed class Target { public int Value; }
            """
        );
    }

    [TestMethod]
    public void MAP0004_ReportsMissingNestedTargetPathSegment()
    {
        AssertDiagnostic(
            "MAP0004",
            DiagnosticSeverity.Warning,
            "Value",
            ["Target.Node.Missing.Value", "does not exist"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target), "Node.Missing.Value")]
                public int Value { get; set; }
            }
            public sealed class Target { public Node Node { get; set; } = new(); }
            public sealed class Node;
            """
        );
    }

    [TestMethod]
    public void MAP0004_ReportsNestedTargetFieldWhenMapFieldsIsDisabled()
    {
        AssertDiagnostic(
            "MAP0004",
            DiagnosticSeverity.Warning,
            "Value",
            ["Target.Node.Value", "does not exist"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target), "Node.Value")]
                public int Value { get; set; }
            }
            public sealed class Target { public Node Node = new(); }
            public sealed class Node { public int Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0007_ReportsUnreadableIntermediateTargetPathSegment()
    {
        AssertDiagnostic(
            "MAP0007",
            DiagnosticSeverity.Error,
            "Value",
            ["Node", "getter", "accessibility"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                [MapProperty(typeof(Target), "Node.Value")]
                public int Value { get; set; }
            }
            public sealed class Target { public Node Node { private get; set; } = new(); }
            public sealed class Node { public int Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0032_ReportsValueTypePropertyInWritableTargetPath()
    {
        AssertDiagnostic(
            "MAP0032",
            DiagnosticSeverity.Error,
            "Value",
            ["Point", "value type", "writable"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                [MapProperty(typeof(Target), "Point.Value")]
                public int Value { get; set; }
            }
            public sealed class Target { public Point Point { get; set; } }
            public struct Point { public int Value; }
            """
        );
    }

    [TestMethod]
    public void DifferentTargetPathsWithSameLeafNameDoNotConflict()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapProperty(typeof(Target), "First.Value")]
                public int First { get; set; }

                [MapProperty(typeof(Target), "Second.Value")]
                public int Second { get; set; }
            }
            public sealed class Target
            {
                public Node First { get; set; } = new();
                public Node Second { get; set; } = new();
            }
            public sealed class Node { public int Value { get; set; } }
            """;

        Assert.IsFalse(RunGenerator(source).Any(d => d.Id == "MAP0002"));
    }

    [TestMethod]
    public void DoesNotReportSourceAccessorDiagnosticForUnusedDirectionalAccessors()
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

            public sealed class ToTarget { public int ToValue { get; set; } }
            public sealed class FromTarget { public int FromValue { get; set; } }
            """;

        Assert.IsFalse(RunGenerator(source).Any(d => d.Id == "MAP0030"));
    }

    [TestMethod]
    public void MemberDirectionsOnlyDiagnoseAccessorsUsedByTheEffectiveDirection()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), MapFields = true)]
            public sealed class Source
            {
                [MapProperty(typeof(Target), Direction = MapDirections.To)]
                public int ToProperty { get; private set; }

                [MapProperty(typeof(Target), Direction = MapDirections.From)]
                public int FromProperty { private get; set; }

                [MapField(typeof(Target), Direction = MapDirections.To)]
                public readonly int ToField;

                [MapField(typeof(Target), Direction = MapDirections.From)]
                public int FromField;
            }

            public sealed class Target
            {
                public int ToProperty { private get; set; }
                public int FromProperty { get; private set; }
                public int ToField;
                public readonly int FromField;
            }
            """;

        var diagnostics = RunGenerator(source);

        Assert.IsFalse(diagnostics.Any(d => d.Id == "MAP0030"));
        Assert.IsFalse(diagnostics.Any(d => d.Id == "MAP0031"));
    }

    [TestMethod]
    public void MemberDirectionIntersectionReportsConflictAndSkipsAccessorDiagnostics()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                [MapProperty(typeof(Target), Direction = MapDirections.From)]
                public int Value { private get; set; }
            }

            public sealed class Target
            {
                public int Value { get; private set; }
            }
            """;

        var diagnostics = RunGenerator(source);
        var diagnostic = diagnostics.Single();

        Assert.AreEqual("MAP0036", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
        StringAssert.Contains(GetLocationText(diagnostic), "MapProperty");
        StringAssert.Contains(diagnostic.GetMessage(), "From");
        StringAssert.Contains(diagnostic.GetMessage(), "To");
        StringAssert.Contains(diagnostic.GetMessage(), "skipped");
    }

    [TestMethod]
    public void MAP0036_ReportsAllMemberAttributeDirectionConflicts()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(PropertyTarget), Direction = MapDirections.To)]
            [MapTarget(typeof(CompositePropertyTarget), Direction = MapDirections.To)]
            [MapTarget(typeof(FieldTarget), Direction = MapDirections.To, MapFields = true)]
            [MapTarget(typeof(CompositeFieldTarget), Direction = MapDirections.To, MapFields = true)]
            public sealed class Source
            {
                [MapProperty(typeof(PropertyTarget), Direction = MapDirections.From)]
                [MapCompositeProperty(typeof(CompositePropertyTarget), typeof(Converter), nameof(CompositePropertyTarget.First), nameof(CompositePropertyTarget.Second), Direction = MapDirections.From)]
                public int PropertyValue { get; set; }

                [MapField(typeof(FieldTarget), Direction = MapDirections.From)]
                [MapCompositeField(typeof(CompositeFieldTarget), typeof(Converter), nameof(CompositeFieldTarget.First), nameof(CompositeFieldTarget.Second), Direction = MapDirections.From)]
                public int FieldValue;
            }

            public sealed class PropertyTarget { public int PropertyValue { get; set; } }
            public sealed class CompositePropertyTarget
            {
                public int First { get; set; }
                public int Second { get; set; }
            }
            public sealed class FieldTarget { public int FieldValue; }
            public sealed class CompositeFieldTarget
            {
                public int First;
                public int Second;
            }
            public sealed class Converter : IMapCompositeConverter<int, (int, int)>
            {
                public (int, int) Convert(int value, MapCompositeConvertArgs args) => (value, value);
                public int ConvertBack((int, int) values, MapCompositeConvertArgs args) => values.Item1;
            }
            """;

        var diagnostics = RunGenerator(source).Where(d => d.Id == "MAP0036").ToArray();

        Assert.AreEqual(4, diagnostics.Length);
        Assert.IsTrue(diagnostics.All(d => d.Severity == DiagnosticSeverity.Warning));
        Assert.IsTrue(diagnostics.All(d => d.GetMessage().Contains("From")));
        Assert.IsTrue(diagnostics.All(d => d.GetMessage().Contains("To")));
        var locations = diagnostics.Select(GetLocationText).ToArray();
        Assert.IsTrue(locations.Any(x => x.StartsWith("MapProperty(")));
        Assert.IsTrue(locations.Any(x => x.StartsWith("MapCompositeProperty(")));
        Assert.IsTrue(locations.Any(x => x.StartsWith("MapField(")));
        Assert.IsTrue(locations.Any(x => x.StartsWith("MapCompositeField(")));
    }

    [TestMethod]
    public void MAP0036_DoesNotReportWhenDirectionsOverlap()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                [MapProperty(typeof(Target), Direction = MapDirections.Both)]
                public int Value { get; set; }
            }

            public sealed class Target { public int Value { get; set; } }
            """;

        Assert.IsFalse(RunGenerator(source).Any(d => d.Id == "MAP0036"));
    }

    [TestMethod]
    public void DoesNotReportSourceAccessorDiagnosticForAccessibleAccessors()
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

        Assert.IsFalse(RunGenerator(source).Any(d => d.Id == "MAP0030"));
    }

    [TestMethod]
    public void DoesNotReportCompositeOverrideForDifferentMappingTargets()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(PropertyTarget))]
            [MapTarget(typeof(CompositeTarget))]
            public sealed class Source
            {
                [MapProperty(typeof(PropertyTarget))]
                [MapCompositeProperty(typeof(CompositeTarget), typeof(Converter), nameof(CompositeTarget.First), nameof(CompositeTarget.Second))]
                public int Value { get; set; }
            }

            public sealed class PropertyTarget { public int Value { get; set; } }
            public sealed class CompositeTarget
            {
                public int First { get; set; }
                public int Second { get; set; }
            }

            public sealed class Converter : IMapCompositeConverter<int, (int, int)>
            {
                public (int, int) Convert(int value, MapCompositeConvertArgs args) => (value, value);
                public int ConvertBack((int, int) values, MapCompositeConvertArgs args) => values.Item1;
            }
            """;

        Assert.IsFalse(RunGenerator(source).Any(d => d.Id == "MAP0029"));
    }

    [TestMethod]
    public void MAP0037_ReportsUnknownConstructorTarget()
    {
        AssertDiagnostic(
            "MAP0037",
            DiagnosticSeverity.Error,
            "MapConstructor",
            ["Missing", "does not match"],
            """
            [MapTarget(typeof(Target))]
            [MapConstructor("Missing", typeof(int))]
            public sealed class Source { public int Value { get; set; } }
            public sealed class Target { public int Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0038_ReportsMissingConstructorSignature()
    {
        AssertDiagnostic(
            "MAP0038",
            DiagnosticSeverity.Error,
            "MapConstructor",
            ["Target", "string"],
            """
            [MapTarget(typeof(Target))]
            [MapConstructor(typeof(Target), typeof(string))]
            public sealed class Source { public int Value { get; set; } }
            public sealed class Target { public Target(int value) {} public int Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0039_ReportsInaccessibleConstructor()
    {
        AssertDiagnostic(
            "MAP0039",
            DiagnosticSeverity.Error,
            "MapConstructor",
            ["Target", "not accessible"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            [MapConstructor(typeof(Target), typeof(int))]
            public sealed class Source { public int Value { get; set; } }
            public sealed class Target
            {
                private Target(int value) { }
                public int Value { get; set; }
            }
            """
        );
    }

    [TestMethod]
    public void MAP0040_ReportsDuplicateConstructorSelection()
    {
        AssertDiagnostic(
            "MAP0040",
            DiagnosticSeverity.Error,
            "MapConstructor(typeof(Target), typeof(int))",
            ["int", "Target"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            [MapConstructor(typeof(Target), typeof(int))]
            [MapConstructor(typeof(Target), typeof(int))]
            public sealed class Source { public int Value { get; set; } }
            public sealed class Target
            {
                public Target(int value) { }
                public int Value { get; set; }
            }
            """
        );
    }

    [TestMethod]
    public void MAP0041_ReportsReadonlyStructReverseMapping()
    {
        AssertDiagnostic(
            "MAP0041",
            DiagnosticSeverity.Error,
            "MapTarget",
            ["Source", "cannot be modified", "Direction"],
            """
            [MapTarget(typeof(Target))]
            public readonly struct Source { public int Value { get; init; } }
            public sealed class Target { public int Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0042_ReportsUnmappedRequiredTargetMember()
    {
        AssertDiagnostic(
            "MAP0042",
            DiagnosticSeverity.Error,
            "MapTarget",
            ["Name", "not initialized"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source { public int Value { get; set; } }
            public sealed class Target
            {
                public int Value { get; set; }
                public required string Name { get; init; }
            }
            """
        );
    }

    [TestMethod]
    public void MAP0044_ReportsNullableCollectionToNonNullableImmutableTarget()
    {
        AssertDiagnostic(
            "MAP0044",
            DiagnosticSeverity.Error,
            "Items",
            ["Items", "non-nullable collection"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                public System.Collections.Generic.List<int>? Items { get; set; }
            }
            public sealed class Target
            {
                public System.Collections.Immutable.ImmutableList<int> Items { get; set; } = [];
            }
            """
        );
    }

    [TestMethod]
    public void MAP0045_WarnsAndKeepsMapToForInitOnlySourceMember()
    {
        const string source = """
            using HKW.Mapper;

            [MapTarget(typeof(Target))]
            public sealed record Source(int Value);
            public sealed class Target { public int Value { get; set; } }
            """;

        var diagnostics = RunGenerator(source);
        var diagnostic = diagnostics.Single(x => x.Id == "MAP0045");

        Assert.AreEqual(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [TestMethod]
    public void MAP0046_ReportsConditionalInitOnlyTargetMember()
    {
        AssertDiagnostic(
            "MAP0046",
            DiagnosticSeverity.Error,
            "MapProperty",
            ["Value", "init-only or required", "MapTo condition"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                [MapProperty(typeof(Target), MapToCondition = MapMemberCondition.SourceIsNotNullOrDefault)]
                public int Value { get; set; }
            }
            public sealed class Target { public int Value { get; init; } }
            """
        );
    }

    [TestMethod]
    public void MAP0046_ReportsConditionalRequiredTargetMember()
    {
        AssertDiagnostic(
            "MAP0046",
            DiagnosticSeverity.Error,
            "MapProperty",
            ["Value", "init-only or required", "MapTo condition"],
            """
            [MapTarget(typeof(Target), Direction = MapDirections.To)]
            public sealed class Source
            {
                [MapProperty(typeof(Target), MapToCondition = MapMemberCondition.SourceIsNullOrDefault)]
                public string Value { get; set; } = string.Empty;
            }
            public sealed class Target { public required string Value { get; set; } }
            """
        );
    }

    [TestMethod]
    public void MAP0047_ReportsInvalidMapSourceContainers()
    {
        const string source = """
            using HKW.Mapper;

            public sealed class Source { public int Value { get; set; } }
            public sealed class Target { public int Value { get; set; } }

            [MapSource(typeof(Source), typeof(Target))]
            public partial class NotStatic;

            [MapSource(typeof(Source), typeof(Target))]
            public static class NotPartial;

            [MapSource(typeof(Source), typeof(Target))]
            public static partial class Generic<T>;

            public sealed class Outer
            {
                [MapSource(typeof(Source), typeof(Target))]
                public static partial class Nested;
            }
            """;

        var diagnostics = RunGenerator(source).Where(x => x.Id == "MAP0047").ToArray();

        Assert.AreEqual(4, diagnostics.Length);
        Assert.IsTrue(diagnostics.All(x => x.Severity == DiagnosticSeverity.Error));
    }

    [TestMethod]
    public void MAP0048_ReportsMapSourceConflictWithMapTarget()
    {
        AssertDiagnostic(
            "MAP0048",
            DiagnosticSeverity.Error,
            "MapSource",
            ["Target", "Source", "already declared"],
            """
            [MapTarget(typeof(Target))]
            public sealed class Source { public int Value { get; set; } }
            public sealed class Target { public int Value { get; set; } }

            [MapSource(typeof(Source), typeof(Target))]
            public static partial class Mappings;
            """
        );
    }

    [TestMethod]
    public void MAP0048_ReportsDuplicateMapSourceTargetName()
    {
        AssertDiagnostic(
            "MAP0048",
            DiagnosticSeverity.Error,
            "MapSource",
            ["Same", "Source", "already declared"],
            """
            public sealed class Source { public int Value { get; set; } }
            public sealed class TargetA { public int Value { get; set; } }
            public sealed class TargetB { public int Value { get; set; } }

            [MapSource(typeof(Source), typeof(TargetA), TargetName = "Same")]
            [MapSource(typeof(Source), typeof(TargetB), TargetName = "Same")]
            public static partial class Mappings;
            """
        );
    }

    private static string Basic(
        string sourceProperty,
        string targetProperty,
        string? options = null
    ) =>
        $$"""
            [MapTarget(typeof(Target){{(options is null ? string.Empty : ", " + options)}})]
            public sealed class Source { {{sourceProperty}} }
            public sealed class Target { {{targetProperty}} }
            """;

    private static string Config(
        string member,
        string targetMembers = "public int Value { get; set; }",
        string sourceMember = "public int Value { get; set; }",
        string additionalType = ""
    ) =>
        $$"""
            [MapTarget(typeof(Target), typeof(Config))]
            public sealed class Source { {{sourceMember}} }
            public sealed class Target { {{targetMembers}} }
            public sealed class Config : MapperConfig<Source, Target>
            {
                public Config(Source source, Target target) : base(source, target) { }
                {{member}}
            }
            {{additionalType}}
            """;

    private static string Composite(
        string converterName,
        string targetNames,
        string converter,
        string additionalTargetMember = ""
    ) =>
        $$"""
            [MapTarget(typeof(Target))]
            public sealed class Source
            {
                [MapCompositeProperty(typeof(Target), typeof({{converterName}}), {{targetNames}})]
                public int Value { get; set; }
            }
            public sealed class Target
            {
                public int First { get; set; }
                public int Second { get; set; }
                {{additionalTargetMember}}
            }
            {{converter}}
            """;

    private static string TupleConverter(
        string firstType,
        string secondType,
        string name = "TupleConverter"
    ) =>
        $$"""
            public sealed class {{name}} : IMapCompositeConverter<int, ({{firstType}}, {{secondType}})>
            {
                public ({{firstType}}, {{secondType}}) Convert(int value, MapCompositeConvertArgs args) => default;
                public int ConvertBack(({{firstType}}, {{secondType}}) values, MapCompositeConvertArgs args) => default;
            }
            """;

    private static string WithUsings(string source) => "using HKW.Mapper;\n" + source;

    private static string GetLocationText(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);

    private static IReadOnlyList<Diagnostic> RunGenerator(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(MapTargetAttribute).Assembly.Location));
        var compilation = CSharpCompilation.Create(
            "DescriptorTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        var inputErrors = compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.AreEqual(
            0,
            inputErrors.Length,
            string.Join(Environment.NewLine, inputErrors.Select(d => d.ToString()))
        );

        var generatorType = typeof(MapTargetAttribute).Assembly.GetType(
            "HKW.Mapper.SourceGenerator.Generator",
            throwOnError: true
        )!;
        var generator = (IIncrementalGenerator)
            Activator.CreateInstance(generatorType, nonPublic: true)!;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: parseOptions
        );

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult().Diagnostics;
    }
}
