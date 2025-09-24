// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Command;

/// <summary>
/// Command to handle the installation of tools via the scaffold CLI.
/// </summary>
/// <remarks>
/// Uses <see cref="IToolManager"/> to perform the actual tool installation logic.
/// </remarks>
internal class ToolInstallCommand(IToolManager toolManager) : Command<ToolInstallSettings>
{
    // The tool manager responsible for installing tools.
    private readonly IToolManager _toolManager = toolManager;

    /// <summary>
    /// Executes the tool install command with the provided settings.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The settings for tool installation.</param>
    /// <returns>0 if successful, otherwise an error code.</returns>
    public override int Execute([NotNull] CommandContext context, [NotNull] ToolInstallSettings settings)
    {
        // Add the tool using the provided settings.
        _toolManager.AddToolAsync(settings.PackageName, settings.AddSources, settings.ConfigFile, settings.Prerelease, settings.Version, settings.Global).Wait();
        return 0;
    }
}
