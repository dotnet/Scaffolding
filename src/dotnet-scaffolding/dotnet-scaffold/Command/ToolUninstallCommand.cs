// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Command;

internal class ToolUninstallCommand(IToolManager toolManager) : Command<ToolUninstallSettings>
{
    private readonly IToolManager _toolManager = toolManager;

    public override int Execute([NotNull] CommandContext context, [NotNull] ToolUninstallSettings settings)
    {
        _toolManager.RemoveTool(settings.PackageName, settings.Global);
        return 0;
    }
}
