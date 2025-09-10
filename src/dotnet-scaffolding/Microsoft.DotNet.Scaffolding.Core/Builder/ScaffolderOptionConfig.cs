// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Configuration for a scaffolder option, used for CLI and interactive UI.
/// </summary>
public class ScaffolderOptionConfig
{
    /// <summary>
    /// Display name for the option.
    /// </summary>
    public required string DisplayName { get; init; }
    /// <summary>
    /// CLI option name (e.g., --option).
    /// </summary>
    public string? CliOption { get; set; }
    /// <summary>
    /// Indicates if the option is required.
    /// </summary>
    public bool Required { get; set; }
    /// <summary>
    /// Description of the option.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Picker type for interactive UI.
    /// </summary>
    public InteractivePickerType PickerType { get; set; } = InteractivePickerType.None;
    /// <summary>
    /// Custom picker values for interactive UI.
    /// </summary>
    public IEnumerable<string>? CustomPickerValues { get; init; }
}
