// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Represents a scaffolder option for CLI and interactive UI.
/// </summary>
public abstract class ScaffolderOption
{
    /// <summary>
    /// Display name for the option.
    /// </summary>
    public required string DisplayName { get; init; }
    /// <summary>
    /// CLI option name (e.g., --option).
    /// </summary>
    public string? CliOption { get; init; }
    /// <summary>
    /// Indicates if the option is required.
    /// </summary>
    public bool Required { get; init; }
    /// <summary>
    /// Description of the option.
    /// </summary>
    public string? Description { get; init; }
    /// <summary>
    /// Picker type for interactive UI.
    /// </summary>
    public InteractivePickerType PickerType { get; init; } = InteractivePickerType.None;
    /// <summary>
    /// Custom picker values for interactive UI.
    /// </summary>
    public IEnumerable<string>? CustomPickerValues { get; init; } = null;

    /// <summary>
    /// Converts the option to a CLI option.
    /// </summary>
    internal abstract Option ToCliOption();
    /// <summary>
    /// Converts the option to a parameter for interactive UI.
    /// </summary>
    internal abstract Parameter ToParameter();
    /// <summary>
    /// Gets the parsed value from a ParseResult.
    /// </summary>
    internal abstract object? GetValue(ParseResult parseResult);
}
