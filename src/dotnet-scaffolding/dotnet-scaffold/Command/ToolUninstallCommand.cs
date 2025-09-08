// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Command;

/// <summary>
/// Command to handle the uninstallation of tools via the scaffold CLI.
/// </summary>
/// <remarks>
/// Uses <see cref="IToolManager"/> to perform the actual tool uninstallation logic.
/// </remarks>
internal class ToolUninstallCommand(IToolManager toolManager) : Command<ToolUninstallSettings>
{
    // The tool manager responsible for uninstalling tools.
    private readonly IToolManager _toolManager = toolManager;

    /// <summary>
    /// Executes the tool uninstall command with the provided settings.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The settings for tool uninstallation.</param>
    /// <returns>0 if successful, otherwise an error code.</returns>
    public override int Execute([NotNull] CommandContext context, [NotNull] ToolUninstallSettings settings)
    {
        // Remove the tool using the provided settings.
        _toolManager.RemoveTool(settings.PackageName, settings.Global);
        return 0;
    }
}
