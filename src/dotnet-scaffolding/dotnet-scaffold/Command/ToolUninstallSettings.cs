// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Command;

internal class ToolUninstallSettings : ToolSettings
{
    [CommandArgument(0, "<PACKAGE_NAME>")]
    public required string PackageName { get; set; }

    [CommandOption("--global")]
    public bool Global { get; set; }
}
