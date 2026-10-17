using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HKW.Mapper.SourceGenerator;

internal sealed class ConverterMappingProcessor
{
    private readonly MapperGenerationContext _context;

    internal ConverterMappingProcessor(MapperGenerationContext context)
    {
        _context = context;
    }

    internal static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (
            type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        )
            return named.TypeArguments[0];
        return type;
    }

    internal static bool IsNullable(ITypeSymbol type) =>
        type.NullableAnnotation == NullableAnnotation.Annotated
        || type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    internal bool AreConverterTypesCompatible(ITypeSymbol converterType, ITypeSymbol propertyType)
    {
        return HasImplicitCompatibleConversion(converterType, propertyType)
            || HasImplicitCompatibleConversion(propertyType, converterType);
    }

    internal bool HasImplicitCompatibleConversion(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        var conversion = ClassifyConversion(sourceType, targetType);
        return conversion.Exists
            && conversion.IsImplicit
            && (conversion.IsIdentity || conversion.IsReference || conversion.IsBoxing);
    }

    internal string ConvertExpression(
        string expression,
        ITypeSymbol sourceType,
        ITypeSymbol targetType
    )
    {
        var conversion = ClassifyConversion(sourceType, targetType);
        if (conversion.Exists && conversion.IsImplicit)
            return expression;
        return $"({targetType.GetGlobalFullName()})({expression})";
    }

    internal Conversion ClassifyConversion(ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        return ((CSharpCompilation)_context.ClassInfo.Compilation).ClassifyConversion(
            sourceType,
            targetType
        );
    }

    internal bool TryUseConfigPropertyConverter(
        MapTargetInfo mapTarget,
        MapMemberInfo propertySymbol,
        MapMemberInfo targetProperty
    )
    {
        if (
            mapTarget.ConfigInfo?.Converters.TryGetValue(propertySymbol.Name, out var data)
                is not true
            || data.IsFieldConverter != propertySymbol.IsField
        )
            return false;

        var converterType = (INamedTypeSymbol)data.Property.Type;
        var converterSourceType = converterType.TypeArguments[0];
        var converterTargetType = converterType.TypeArguments[1];
        if (
            AreConverterTypesCompatible(converterSourceType, propertySymbol.Type) is false
            || AreConverterTypesCompatible(converterTargetType, targetProperty.Type) is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterGenericTypeMismatch,
                data.Property.Locations[0],
                converterType.TypeArguments[0].GetName(),
                converterType.TypeArguments[1].GetName(),
                propertySymbol.Type.GetName(),
                targetProperty.Type.GetName()
            );
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return false;
        }
        var convertName = $"{MapConfigInfo.ConfigName}.{data.Property.Name}";
        var sourceValue = ConvertExpression(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath}",
            propertySymbol.Type,
            converterSourceType
        );
        var convertArgs =
            $"new global::HKW.Mapper.MapConvertArgs({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName}, \"{propertySymbol.AccessPath}\", \"{targetProperty.AccessPath}\")";
        var convertedTargetValue =
            $"{convertName}.{nameof(IMapConverter<,>.Convert)}({sourceValue}, {convertArgs})";
        var targetValue = ConvertExpression(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}",
            targetProperty.Type,
            converterTargetType
        );
        var convertedSourceValue =
            $"{convertName}.{nameof(IMapConverter<,>.ConvertBack)}({targetValue}, {convertArgs})";
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath} = {ConvertExpression(convertedTargetValue, converterTargetType, targetProperty.Type)};"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath} = {ConvertExpression(convertedSourceValue, converterSourceType, propertySymbol.Type)};"
        );
        return true;
    }

    internal bool TryUsePropertyConverter(
        MapTargetInfo mapTarget,
        MapMemberInfo propertySymbol,
        AttributeInfo? attributeInfo,
        MapMemberInfo targetProperty
    )
    {
        if (
            attributeInfo?.TryGetParam<INamedTypeSymbol>(
                nameof(MapPropertyAttribute.ConverterType),
                out var converterType
            )
                is not true
            || converterType is null
        )
            return false;

        var converterInterface = converterType.GetInterface(TypeFullNames.MapConverterInterface);
        // 判断转换器是否实现转换器接口
        if (converterInterface is null)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterDoesNotImplementIMapConverter,
                attributeInfo.Data.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                    attributeInfo.Data.ApplicationSyntaxReference.Span
                ),
                targetProperty.ToString()
            );
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return false;
        }
        var converterSourceType = converterInterface.TypeArguments[0];
        var converterTargetType = converterInterface.TypeArguments[1];
        if (
            AreConverterTypesCompatible(converterSourceType, propertySymbol.Type) is false
            || AreConverterTypesCompatible(converterTargetType, targetProperty.Type) is false
        )
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.ConverterGenericTypeMismatch,
                attributeInfo.Data.ApplicationSyntaxReference!.SyntaxTree.GetLocation(
                    attributeInfo.Data.ApplicationSyntaxReference.Span
                ),
                converterInterface.TypeArguments[0].GetName(),
                converterInterface.TypeArguments[1].GetName(),
                propertySymbol.Type.GetName(),
                targetProperty.Type.GetName()
            );
            _context.ClassInfo.ProductionContext.ReportDiagnostic(diagnostic);
            return false;
        }
        _context.ClassInfo.AddConverters(converterType, out var fieldName);
        var sourceValue = ConvertExpression(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath}",
            propertySymbol.Type,
            converterSourceType
        );
        var convertArgs =
            $"new global::HKW.Mapper.MapConvertArgs({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName}, \"{propertySymbol.AccessPath}\", \"{targetProperty.AccessPath}\")";
        var convertedTargetValue =
            $"{fieldName}.{nameof(IMapConverter<,>.Convert)}({sourceValue}, {convertArgs})";
        var targetValue = ConvertExpression(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath}",
            targetProperty.Type,
            converterTargetType
        );
        var convertedSourceValue =
            $"{fieldName}.{nameof(IMapConverter<,>.ConvertBack)}({targetValue}, {convertArgs})";
        mapTarget.MapToMethod.Contents.Add(
            $"{MapTargetInfo.TargetParamName}.{targetProperty.AccessPath} = {ConvertExpression(convertedTargetValue, converterTargetType, targetProperty.Type)};"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"{MapTargetInfo.SourceParamName}.{propertySymbol.AccessPath} = {ConvertExpression(convertedSourceValue, converterSourceType, propertySymbol.Type)};"
        );
        return true;
    }
}
