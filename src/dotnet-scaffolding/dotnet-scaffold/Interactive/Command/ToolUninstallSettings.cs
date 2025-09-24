// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Command;

/// <summary>
/// Represents the settings used for uninstalling a tool via the scaffold CLI.
/// </summary>
internal class ToolUninstallSettings : ToolSettings
{
    /// <summary>
    /// Gets or sets the package name of the tool to uninstall.
    /// </summary>
    [CommandArgument(0, "<PACKAGE_NAME>")]
    public required string PackageName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to uninstall the tool globally.
    /// </summary>
    [CommandOption("--global")]
    public bool Global { get; set; }
}
