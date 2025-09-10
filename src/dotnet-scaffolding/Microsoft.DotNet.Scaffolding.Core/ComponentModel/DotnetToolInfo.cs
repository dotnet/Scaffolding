// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Core.ComponentModel;

/// <summary>
/// Represents information about a .NET tool as reported by 'dotnet tool list' and 'dotnet tool list -g'.
/// </summary>
internal class DotNetToolInfo
{
    /// <summary>
    /// Gets or sets the package name of the .NET tool.
    /// </summary>
    public string PackageName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the version of the .NET tool.
    /// </summary>
    public string Version { get; set; } = default!;

    /// <summary>
    /// Gets or sets the command provided by the .NET tool.
    /// </summary>
    public string Command { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the tool is installed globally.
    /// </summary>
    public bool IsGlobalTool { get; set; } = false;

    /// <summary>
    /// Returns a display string for the tool, including command, package name, and version.
    /// </summary>
    /// <returns>A formatted string representing the tool.</returns>
    public string ToDisplayString()
    {
        return $"{Command} ({PackageName} v{Version})";
    }
}
