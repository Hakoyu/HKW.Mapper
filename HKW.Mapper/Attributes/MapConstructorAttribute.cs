namespace HKW.Mapper;

/// <summary>
/// Selects a target constructor used when <see cref="MapTargetAttribute"/> creates a target instance.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class MapConstructorAttribute : Attribute
{
    /// <summary>
    /// Selects a constructor for the target type.
    /// </summary>
    /// <param name="targetType">The target type declared by <see cref="MapTargetAttribute"/>.</param>
    /// <param name="parameterTypes">The exact constructor parameter types.</param>
    public MapConstructorAttribute(Type targetType, params Type[] parameterTypes)
    {
        TargetType = targetType;
        ParameterTypes = parameterTypes;
    }

    /// <summary>
    /// Selects a constructor for the target name declared by <see cref="MapTargetAttribute.TargetName"/>.
    /// </summary>
    /// <param name="targetName">The target name.</param>
    /// <param name="parameterTypes">The exact constructor parameter types.</param>
    public MapConstructorAttribute(string targetName, params Type[] parameterTypes)
    {
        TargetName = targetName;
        ParameterTypes = parameterTypes;
    }

    /// <summary>Gets the selected target type.</summary>
    public Type? TargetType { get; }

    /// <summary>Gets the selected target name.</summary>
    public string? TargetName { get; }

    /// <summary>Gets the exact constructor parameter types.</summary>
    public Type[] ParameterTypes { get; }
}
