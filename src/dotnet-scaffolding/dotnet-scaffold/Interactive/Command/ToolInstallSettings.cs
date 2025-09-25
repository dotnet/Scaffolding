// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Command;

/// <summary>
/// Represents the settings used for installing a tool via the scaffold CLI.
/// </summary>
internal class ToolInstallSettings : ToolSettings
{
    /// <summary>
    /// Gets or sets the package name of the tool to install.
    /// </summary>
    //[Description("Available packages are dotnet-scaffold-aspnet and dotnet-scaffold-aspire.")]
    [CommandArgument(0, "<PACKAGE_NAME>")]
    public required string PackageName { get; set; }

    /// <summary>
    /// Gets or sets additional sources to use when searching for the tool package.
    /// </summary>
    [CommandOption("--add-source <SOURCE>")]
    public string[] AddSources { get; set; } = [];

    /// <summary>
    /// Gets or sets the NuGet configuration file to use.
    /// </summary>
    [CommandOption("--configfile <FILE>")]
    public string? ConfigFile { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow prerelease versions of the tool.
    /// </summary>
    [CommandOption("--prerelease")]
    public bool Prerelease { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to install the tool globally.
    /// </summary>
    [CommandOption("--global")]
    public bool Global { get; set; }

    /// <summary>
    /// Gets or sets the version of the tool to install.
    /// </summary>
    [CommandOption("--version <VERSION_NUMBER>")]
    public string? Version { get; set; }
}
