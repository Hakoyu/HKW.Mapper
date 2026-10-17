using System;
using HKW.SourceGeneratorUtils;
using Microsoft.CodeAnalysis;

namespace HKW.Mapper.SourceGenerator;

internal sealed class CreationMethodFinalizer
{
    private readonly MapperGenerationContext _context;

    internal CreationMethodFinalizer(MapperGenerationContext context)
    {
        _context = context;
    }

    internal void FinalizeCreationMethods(MapTargetInfo mapTarget)
    {
        if (mapTarget.Direction.HasFlag(MapDirections.To) is false)
            return;

        if (mapTarget.Constructors.Count == 0 && mapTarget.CreationInitializers.Count > 0)
        {
            foreach (var memberName in mapTarget.CreationInitializers.Keys)
            {
                _context.ClassInfo.ProductionContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.InitTargetMemberCannotBeCreated,
                        mapTarget.MapTargetLocation,
                        memberName
                    )
                );
            }
        }

        var requiredMembers = mapTarget.MemberByName.Values
            .Where(x => x.IsRequired)
            .Distinct()
            .ToArray();
        foreach (var constructorInfo in mapTarget.Constructors)
        {
            var constructor = constructorInfo.Constructor;
            var setsRequiredMembers = constructor.GetAttributes().Any(x =>
                x.AttributeClass?.GetGlobalFullName()
                    == "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute"
            );
            var missingRequired = setsRequiredMembers
                ? []
                : requiredMembers
                    .Where(x => mapTarget.CreationInitializers.ContainsKey(x.Name) is false)
                    .ToArray();
            if (missingRequired.Length > 0)
            {
                foreach (var member in missingRequired)
                {
                    _context.ClassInfo.ProductionContext.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.RequiredTargetMemberNotInitialized,
                            constructorInfo.Location,
                            member.Name
                        )
                    );
                }
                continue;
            }

            var initializer = BuildObjectInitializer(mapTarget);
            var arguments = constructor.Parameters.Select(GetConstructorArgument).ToArray();
            var creation =
                $"new {mapTarget.TargetType.GetGlobalFullName()}({string.Join(", ", arguments)}){initializer}";
            if (constructor.Parameters.Length == 0)
            {
                if (mapTarget.TargetType.IsReferenceType)
                {
                    mapTarget.MapToMethod.Params![1] = new(
                        $"{mapTarget.TargetType.GetGlobalFullName()}?",
                        MapTargetInfo.TargetParamName
                    );
                    mapTarget.MapToMethod.Contents.Insert(
                        0,
                        $"{MapTargetInfo.TargetParamName} ??= {creation};"
                    );
                }
                else if (mapTarget.CreationInitializers.Count > 0)
                {
                    mapTarget.MapToMethod.Contents.Insert(
                        0,
                        $"{MapTargetInfo.TargetParamName} = {creation};"
                    );
                }
                continue;
            }

            var method = new MethodGenerateInfo(
                mapTarget.MapToMethod.TypeName,
                mapTarget.MapToName,
                string.Empty
            )
            {
                IsStatic = true,
                Accessibility = mapTarget.MapToMethod.Accessibility,
                Params = [],
            };
            method.Params.Add(
                new(mapTarget.SourceType, MapTargetInfo.SourceParamName)
                {
                    GenerateType = ParameterGenerateType.This,
                }
            );
            method.Params.Add(
                new(
                    mapTarget.TargetType.IsReferenceType
                        ? $"{mapTarget.TargetType.GetGlobalFullName()}?"
                        : $"global::System.Nullable<{mapTarget.TargetType.GetGlobalFullName()}>",
                    MapTargetInfo.TargetParamName
                )
            );
            foreach (var parameter in constructor.Parameters)
            {
                var generatedParameter = new ParameterGenerateInfo(
                    parameter.Type,
                    GetConstructorParameterName(parameter)
                );
                generatedParameter.GenerateType = parameter.IsParams
                    ? ParameterGenerateType.Params
                    : parameter.RefKind switch
                    {
                        RefKind.Ref => ParameterGenerateType.Ref,
                        RefKind.Out => ParameterGenerateType.Out,
                        RefKind.In => ParameterGenerateType.In,
                        _ => ParameterGenerateType.None,
                    };
                method.Params.Add(generatedParameter);
            }
            method.Contents.Add(
                $"if ({MapTargetInfo.TargetParamName} is not null) throw new global::System.ArgumentException(\"A target instance cannot be supplied when constructor arguments are used.\", nameof({MapTargetInfo.TargetParamName}));"
            );
            method.Contents.Add($"var mappedTarget = {creation};");
            method.Contents.Add(
                mapTarget.ConfigInfo?.MapTo.IsAsync is true
                    ? $"return await {mapTarget.MapToName}({MapTargetInfo.SourceParamName}, mappedTarget);"
                    : $"return {mapTarget.MapToName}({MapTargetInfo.SourceParamName}, mappedTarget);"
            );
            mapTarget.MapToCreationMethods.Add(method);
        }
    }

    private static string BuildObjectInitializer(MapTargetInfo mapTarget) =>
        mapTarget.CreationInitializers.Count == 0
            ? string.Empty
            : " { "
                + string.Join(
                    ", ",
                    mapTarget.CreationInitializers.Select(x => $"{x.Key} = {x.Value}")
                )
                + " }";

    private static string GetConstructorParameterName(IParameterSymbol parameter) =>
        parameter.Name is MapTargetInfo.SourceParamName or MapTargetInfo.TargetParamName
            ? $"constructor_{parameter.Name}"
            : parameter.Name;

    private static string GetConstructorArgument(IParameterSymbol parameter)
    {
        var modifier = parameter.RefKind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => string.Empty,
        };
        return modifier + GetConstructorParameterName(parameter);
    }

    internal void TryAddConfig(MapTargetInfo mapTarget)
    {
        if (mapTarget.ConfigInfo is null)
            return;
        mapTarget.MapToMethod.Contents.Add(
            $"var {MapConfigInfo.ConfigName} = new {mapTarget.ConfigInfo.Type.GetGlobalFullName()}({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName});"
        );
        mapTarget.MapFromMethod.Contents.Add(
            $"var {MapConfigInfo.ConfigName} = new {mapTarget.ConfigInfo.Type.GetGlobalFullName()}({MapTargetInfo.SourceParamName}, {MapTargetInfo.TargetParamName});"
        );
    }
}
