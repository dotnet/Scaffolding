// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Command;

internal class ToolInstallSettings : ToolSettings
{
    [CommandArgument(0, "<PACKAGE_NAME>")]
    public required string PackageName { get; set; }

    [CommandOption("--add-source <SOURCE>")]
    public string[] AddSources { get; set; } = [];

    [CommandOption("--configfile <FILE>")]
    public string? ConfigFile { get; set; }

    [CommandOption("--prerelease")]
    public bool Prerelease { get; set; }

    [CommandOption("--global")]
    public bool Global { get; set; }

    [CommandOption("--version <VERSION_NUMBER>")]
    public string? Version { get; set; }
}
