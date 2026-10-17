using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace HKW.Mapper;

internal static class Descriptors
{
    private const string _category = "HKWMapper";

    public static readonly DiagnosticDescriptor DuplicateMapMethodName = new(
        id: "MAP0001",
        title: "Duplicate map method name",
        messageFormat: "Map method name '{0}' is already in use. Set a different target name on the attribute.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor DuplicateMapTargetMember = new(
        id: "MAP0002",
        title: "Multiple source members map to the same target member",
        messageFormat: "Source members '{0}' and '{1}' both map to target member '{2}'. Set a different target member name.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor InvalidMapConfigType = new(
        id: "MAP0003",
        title: "Invalid map config type",
        messageFormat: "Map config '{0}' must inherit from MapperConfig<TSource, TTarget>.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetMemberDoesNotExist = new(
        id: "MAP0004",
        title: "Target member does not exist",
        messageFormat: "Target member '{0}' does not exist. Specify a valid member name or ignore the source member.",
        category: _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor TargetMemberTypeIncompatible = new(
        id: "MAP0005",
        title: "Target member type is incompatible",
        messageFormat: "Target member '{0}' has an incompatible type. Ignore the member or configure a converter.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor TargetMemberIsStatic = new(
        id: "MAP0006",
        title: "Target member is static",
        messageFormat: "Target member '{0}' is static. Ignore it or map to an instance member.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetMemberAccessorInaccessible = new(
        id: "MAP0007",
        title: "Target member accessor is inaccessible",
        messageFormat: "Target member '{0}' has an inaccessible {1}. Ignore the member or change its accessibility.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor SourceMemberIsReferenceType = new(
        id: "MAP0008",
        title: "Source member is a reference type",
        messageFormat: "Source member '{0}' is a reference type. Ignore it or set MapType to MapPropertyType.Reference.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor ConverterDoesNotImplementIMapConverter = new(
        id: "MAP0009",
        title: "Converter does not implement IMapConverter",
        messageFormat: "Converter '{0}' must implement IMapConverter<TSourceValue, TTargetValue>.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor ConverterGenericTypeMismatch = new(
        id: "MAP0010",
        title: "Converter generic arguments are incompatible",
        messageFormat: "Converter types '<{0}, {1}>' are incompatible with mapping types '<{2}, {3}>'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor InvalidMapConfigConverterMemberName = new(
        id: "MAP0011",
        title: "Map config converter member name is invalid",
        messageFormat: "The map config converter source member name must be a non-empty string.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor InvalidMapConfigActionMemberName = new(
        id: "MAP0012",
        title: "Map config action member name is invalid",
        messageFormat: "The map config action member name must be a non-empty string for this action mode.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor DuplicateMapConfigActionForMember = new(
        id: "MAP0013",
        title: "Duplicate map config action for member",
        messageFormat: "Only one map config action can be assigned to a member for each action mode.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigActionCannotHaveParameters = new(
        id: "MAP0014",
        title: "Map config action method cannot have parameters",
        messageFormat: "A map config action method cannot declare parameters.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigActionMethodInaccessible = new(
        id: "MAP0015",
        title: "Map config action method is inaccessible",
        messageFormat: "A map config action method must be internal or public.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MemberMappingTargetNotFound = new(
        id: "MAP0016",
        title: "Member mapping target not found",
        messageFormat: "Member mapping target '{0}' does not match any MapTarget.",
        category: _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MemberMappingTargetAmbiguous = new(
        id: "MAP0017",
        title: "Member mapping target is ambiguous",
        messageFormat: "The mapping attribute on member '{0}' matches more than one MapTarget.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MapConfigGenericTypeMismatch = new(
        id: "MAP0018",
        title: "Map config generic arguments are incompatible",
        messageFormat: "Map config types '<{0}, {1}>' are incompatible with MapTarget types '<{2}, {3}>'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor ConverterDoesNotImplementIMapCompositeConverter = new(
        id: "MAP0019",
        title: "Converter does not implement IMapCompositeConverter",
        messageFormat: "Converter '{0}' must implement IMapCompositeConverter<TSourceValue, TTargetValues>.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor CompositeConverterRequiresTuple = new(
        id: "MAP0020",
        title: "Composite converter requires a tuple",
        messageFormat: "Composite converter '{0}' must use a ValueTuple as its target values type.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor CompositeTargetMemberCountMismatch = new(
        id: "MAP0021",
        title: "Composite target member count mismatch",
        messageFormat: "Composite mapping declares {0} target members, but the converter tuple contains {1} elements.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor CompositeTargetMemberTypeMismatch = new(
        id: "MAP0022",
        title: "Composite target member type mismatch",
        messageFormat: "Target member '{0}' has type '{1}', but tuple element {2} has type '{3}'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor InvalidCompositeTargetMemberNames = new(
        id: "MAP0023",
        title: "Composite target member names are invalid",
        messageFormat: "Composite target members must contain at least two unique, non-empty names.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor CompositeSourceTypeMismatch = new(
        id: "MAP0024",
        title: "Composite converter source type mismatch",
        messageFormat: "Composite converter source type '{0}' cannot be applied to source member type '{1}'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor InvalidMapConfigCompositeMemberNames = new(
        id: "MAP0025",
        title: "Map config composite member names are invalid",
        messageFormat: "A map config composite converter requires one source member and at least two unique target members.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigCompositeInitializerCannotBeAnalyzed = new(
        id: "MAP0026",
        title: "Map config composite initializer cannot be analyzed",
        messageFormat: "A map config composite converter must use an inline object initializer with an inline target member array.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConfigConverterOverridesAttribute = new(
        id: "MAP0027",
        title: "Map config converter overrides mapping attribute",
        messageFormat: "Only the map config converter is used; remove the redundant mapping attribute.",
        category: _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapIgnoreOverridesMappingAttributes = new(
        id: "MAP0028",
        title: "Ignore attribute overrides mapping attributes",
        messageFormat: "Only the ignore attribute is used; remove other mapping attributes from this member.",
        category: _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor CompositeMappingOverridesRegularMapping = new(
        id: "MAP0029",
        title: "Composite mapping attribute overrides regular mapping attribute",
        messageFormat: "Only the composite mapping attribute is used for this mapping target; remove the regular mapping attribute for the same target.",
        category: _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor SourceMemberAccessorInaccessible = new(
        id: "MAP0030",
        title: "Source member accessor is inaccessible",
        messageFormat: "Source member '{0}' has an inaccessible {1}. Ignore the member or change its accessibility.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor FieldIsReadOnly = new(
        id: "MAP0031",
        title: "Field is read-only",
        messageFormat: "Field '{0}' cannot be written when mapping {1}.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TargetPathValueTypeNotWritable = new(
        id: "MAP0032",
        title: "Target path value type is not writable",
        messageFormat: "Target path member '{0}' is a value type property and does not provide a writable variable for nested member '{1}'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor SourcePathMemberDoesNotExist = new(
        id: "MAP0033",
        title: "Source path member does not exist",
        messageFormat: "Source path member '{0}' does not exist. Check SourcePath.",
        category: _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor SourcePathMemberIsStatic = new(
        id: "MAP0034",
        title: "Source path member is static",
        messageFormat: "Source path member '{0}' is static. Check SourcePath.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor SourcePathValueTypeNotWritable = new(
        id: "MAP0035",
        title: "Source path value type is not writable",
        messageFormat: "Source path member '{0}' is a value type property and does not provide a writable variable for nested member '{1}'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MemberDirectionConflictsWithMapTarget = new(
        id: "MAP0036",
        title: "Member mapping direction conflicts with MapTarget",
        messageFormat: "Member mapping direction '{0}' has no overlap with MapTarget direction '{1}'; this member mapping is skipped.",
        category: _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConstructorTargetNotFound = new(
        id: "MAP0037",
        title: "Map constructor target not found",
        messageFormat: "Map constructor target '{0}' does not match any MapTarget on this source type.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConstructorNotFound = new(
        id: "MAP0038",
        title: "Map constructor not found",
        messageFormat: "Target type '{0}' does not contain a constructor with parameter types ({1}).",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor MapConstructorInaccessible = new(
        id: "MAP0039",
        title: "Map constructor is inaccessible",
        messageFormat: "The selected constructor on target type '{0}' is not accessible from generated mapping code.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor DuplicateMapConstructor = new(
        id: "MAP0040",
        title: "Duplicate map constructor",
        messageFormat: "The constructor ({0}) is selected more than once for map target '{1}'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor ReadOnlyStructCannotMapFrom = new(
        id: "MAP0041",
        title: "Readonly struct cannot map from a target",
        messageFormat: "Readonly struct source '{0}' cannot be modified by MapFrom. Use Direction = MapDirections.To.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor RequiredTargetMemberNotInitialized = new(
        id: "MAP0042",
        title: "Required target member is not initialized",
        messageFormat: "Required target member '{0}' is not initialized by this mapping, so the creation overload is not generated.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor InitTargetMemberCannotBeCreated = new(
        id: "MAP0043",
        title: "Init-only target member cannot be initialized",
        messageFormat: "Init-only target member '{0}' cannot be represented in the generated object initializer.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor NullableCollectionRequiresNullableTarget = new(
        id: "MAP0044",
        title: "Nullable collection requires a nullable target",
        messageFormat: "Nullable source collection member '{0}' cannot map to non-nullable collection target member '{1}'.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor InitOnlySourceMemberCannotMapFrom = new(
        id: "MAP0045",
        title: "Init-only source member cannot map from a target",
        messageFormat: "Init-only source member '{0}' cannot be modified by MapFrom; only its MapTo direction is generated.",
        category: _category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor ConditionalTargetMemberCannotBeInitialized = new(
        id: "MAP0046",
        title: "Conditional target member cannot be initialized",
        messageFormat: "Target member '{0}' is init-only or required and cannot use a MapTo condition in a generated object initializer; its MapTo mapping is skipped.",
        category: _category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
