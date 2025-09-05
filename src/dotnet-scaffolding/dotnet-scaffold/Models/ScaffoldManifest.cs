// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Represents the manifest for scaffolding tools, including version and tool list.
namespace Microsoft.DotNet.Tools.Scaffold.Models;

internal class ScaffoldManifest
{
    /// <summary>
    /// Gets the version of the scaffold manifest.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the list of tools defined in the manifest.
    /// </summary>
    public required IList<ScaffoldTool> Tools { get; init; }

    /// <summary>
    /// Determines whether the manifest contains a tool with the specified name.
    /// </summary>
    /// <param name="name">The name of the tool to check for.</param>
    /// <returns>True if the tool exists; otherwise, false.</returns>
    public bool HasTool(string name)
        => Tools.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
}
