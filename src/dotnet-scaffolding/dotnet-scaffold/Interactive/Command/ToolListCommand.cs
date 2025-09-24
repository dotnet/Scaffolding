// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Interactive.Command;

/// <summary>
/// Command to handle listing installed tools via the scaffold CLI.
/// </summary>
/// <remarks>
/// Uses <see cref="IToolManager"/> to perform the actual tool listing logic.
/// </remarks>
internal class ToolListCommand(IToolManager toolManager) : Command<ToolListSettings>
{
    // The tool manager responsible for listing tools.
    private readonly IToolManager _toolManager = toolManager;

    /// <summary>
    /// Executes the tool list command with the provided settings.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The settings for tool listing.</param>
    /// <returns>0 if successful, otherwise an error code.</returns>
    public override int Execute([NotNull] CommandContext context, [NotNull] ToolListSettings settings)
    {
        // List the installed tools using the tool manager.
        _toolManager.ListTools();

        return 0;
    }
}
