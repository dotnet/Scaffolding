// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.ObjectModel;

namespace Microsoft.DotNet.Scaffolding.Core.ComponentModel;

/// <summary>
/// Represents a parameter for a command, including its name, type, and additional metadata.
/// </summary>
internal class Parameter
{
    /// <summary>
    /// Gets the unique name of the parameter.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the display name of the parameter, suitable for UI or help output.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is required.
    /// </summary>
    public required bool Required { get; set; } = false;

    /// <summary>
    /// Gets or sets the description of the parameter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the CLI type of the parameter.
    /// </summary>
    public required CliTypes Type { get; set; } = CliTypes.String;

    /// <summary>
    /// Gets or sets the picker type for interactive selection.
    /// </summary>
    public InteractivePickerType PickerType { get; set; } = InteractivePickerType.None;

    /// <summary>
    /// Gets or sets custom picker values for the parameter, if applicable.
    /// </summary>
    public IEnumerable<string>? CustomPickerValues { get; set; }

    /// <summary>
    /// Gets or sets the message displayed to the user during an interactive prompt.
    /// </summary>
    public string? InteractivePromptMessage { get; set; }

    /// <summary>
    /// Mapping from <see cref="CliTypes"/> to corresponding .NET <see cref="Type"/>.
    /// </summary>
    private static readonly ReadOnlyDictionary<CliTypes, Type> s_baseTypeToTypeMapping = new(
        new Dictionary<CliTypes, Type>
        {
            { CliTypes.Bool, typeof(bool) },
            { CliTypes.Int, typeof(int) },
            { CliTypes.Long, typeof(long) },
            { CliTypes.Double, typeof(double) },
            { CliTypes.Decimal, typeof(decimal) },
            { CliTypes.Char, typeof(char) },
            { CliTypes.String, typeof(string) }
        });

    /// <summary>
    /// Mapping from .NET <see cref="Type"/> to <see cref="CliTypes"/>.
    /// </summary>
    private static readonly ReadOnlyDictionary<Type, CliTypes> s_typeToBaseTypeMapping = new(s_baseTypeToTypeMapping.ToDictionary(x => x.Value, x => x.Key));

    /// <summary>
    /// Gets the .NET <see cref="Type"/> corresponding to the specified <see cref="CliTypes"/>.
    /// </summary>
    /// <param name="baseType">The CLI type.</param>
    /// <returns>The corresponding .NET type.</returns>
    public static Type GetType(CliTypes baseType)
    {
        return s_baseTypeToTypeMapping[baseType];
    }

    /// <summary>
    /// Gets the <see cref="CliTypes"/> corresponding to the specified .NET <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The .NET type.</param>
    /// <returns>The corresponding CLI type.</returns>
    public static CliTypes GetCliType(Type type)
    {
        return s_typeToBaseTypeMapping[type];
    }

    /// <summary>
    /// Gets the <see cref="CliTypes"/> corresponding to the generic type parameter <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The .NET type.</typeparam>
    /// <returns>The corresponding CLI type.</returns>
    public static CliTypes GetCliType<T>()
        => GetCliType(typeof(T));
}
