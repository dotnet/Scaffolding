// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Defines operations for managing .NET CLI tools, such as installing, uninstalling, and retrieving tool information and commands.
/// </summary>
internal interface IDotNetToolService
{
    /// <summary>
    /// Gets all commands for the specified tools in parallel.
    /// </summary>
    /// <param name="components">The list of tool components to query. If null, all tools are used.</param>
    /// <param name="envVars">Optional environment variables for the command execution.</param>
    /// <returns>A list of key-value pairs mapping tool command names to their command info.</returns>
    IList<KeyValuePair<string, CommandInfo>> GetAllCommandsParallel(IList<DotNetToolInfo>? components = null, IDictionary<string, string>? envVars = null);

    /// <summary>
    /// Gets information about a specific .NET tool by name and optional version.
    /// </summary>
    /// <param name="componentName">The name of the tool package or command.</param>
    /// <param name="version">Optional version to match.</param>
    /// <returns>The matching <see cref="DotNetToolInfo"/>, or null if not found.</returns>
    DotNetToolInfo? GetDotNetTool(string? componentName, string? version = null);

    /// <summary>
    /// Gets all installed .NET tools.
    /// </summary>
    /// <param name="refresh">Whether to refresh the tool list from the system.</param>
    /// <param name="envVars">Optional environment variables for the command execution.</param>
    /// <returns>A list of installed <see cref="DotNetToolInfo"/> objects.</returns>
    IList<DotNetToolInfo> GetDotNetTools(bool refresh = false, IDictionary<string, string>? envVars = null);

    /// <summary>
    /// Installs a .NET tool with the specified options.
    /// </summary>
    /// <param name="toolName">The name of the tool to install.</param>
    /// <param name="version">Optional version to install.</param>
    /// <param name="global">Whether to install the tool globally.</param>
    /// <param name="prerelease">Whether to allow prerelease versions.</param>
    /// <param name="addSources">Optional additional sources for installation.</param>
    /// <param name="configFile">Optional NuGet config file path.</param>
    /// <returns>True if the tool was installed successfully; otherwise, false.</returns>
    bool InstallDotNetTool(string toolName, string? version = null, bool global = false, bool prerelease = false, string[]? addSources = null, string? configFile = null);

    /// <summary>
    /// Uninstalls a .NET tool by name.
    /// </summary>
    /// <param name="toolName">The name of the tool to uninstall.</param>
    /// <param name="global">Whether the tool is installed globally.</param>
    /// <returns>True if the tool was uninstalled successfully; otherwise, false.</returns>
    bool UninstallDotNetTool(string toolName, bool global = false);

    /// <summary>
    /// Gets the list of commands provided by a specific .NET tool.
    /// </summary>
    /// <param name="dotnetTool">The tool to query for commands.</param>
    /// <param name="envVars">Optional environment variables for the command execution.</param>
    /// <returns>A list of <see cref="CommandInfo"/> objects for the tool.</returns>
    List<CommandInfo> GetCommands(DotNetToolInfo dotnetTool, IDictionary<string, string>? envVars = null);
}
