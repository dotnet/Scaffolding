// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Core.ComponentModel;

/// <summary>
/// Represents the information about a command that components should initialize and return
/// (serialized as JSON to stdout) when the 'get-commands' operation is invoked.
/// </summary>
internal class CommandInfo
{
    /// <summary>
    /// Gets the unique name of the command.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the display name of the command, suitable for UI or help output.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the list of categories this command belongs to, for grouping in UI.
    /// </summary>
    public required List<string> DisplayCategories { get; init; }

    /// <summary>
    /// Gets or sets the description of the command.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the parameters accepted by this command.
    /// </summary>
    public required Parameter[] Parameters { get; init; }
}
