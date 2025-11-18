// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Manages scaffold tools by coordinating installation, removal, and listing operations.
/// Interacts with the tool manifest and dotnet tool services, and provides logging.
/// </summary>
internal class ToolManager(
    ILogger<ToolManager> logger,
    IToolManifestService toolManifestService,
    IDotNetToolService dotnetToolService,
    IScaffolderLogger scaffolderLogger) : IToolManager
{
    // Logger for recording informational and error messages.
    private readonly ILogger _logger = logger;
    // Service for managing the tool manifest file.
    private readonly IToolManifestService _toolManifestService = toolManifestService;
    // Service for installing and uninstalling dotnet tools.
    private readonly IDotNetToolService _dotnetToolService = dotnetToolService;
    // Logger for displaying formatted console output.
    private readonly IScaffolderLogger _scaffolderLogger = scaffolderLogger;

    /// <summary>
    /// Installs a scaffold tool and adds it to the manifest.
    /// </summary>
    /// <param name="packageName">The name of the tool package to install.</param>
    /// <param name="addSources">Additional sources for tool installation.</param>
    /// <param name="configFile">Optional NuGet config file path.</param>
    /// <param name="prerelease">Whether to allow prerelease versions.</param>
    /// <param name="version">Optional version to install.</param>
    /// <param name="global">Whether to install the tool globally.</param>
    /// <returns>True if the tool was installed and added to the manifest; otherwise, false.</returns>
    public bool AddTool(string packageName, string[] addSources, string? configFile, bool prerelease, string? version, bool global)
    {
        _scaffolderLogger.LogInformation($"Installing {packageName}...\n");

        if (_dotnetToolService.GetDotNetTool(packageName) is not null || _dotnetToolService.InstallDotNetTool(packageName, version, global: global, prerelease, addSources, configFile))
        {
            if (_toolManifestService.AddTool(packageName))
            {
                _scaffolderLogger.LogInformation($"Tool {packageName} installed successfully.\n");
                return true;
            }
            else
            {
                _scaffolderLogger.LogError($"Failed to add tool {packageName} to manifest.\n");
            }
        }
        else
        {
            _scaffolderLogger.LogError($"Failed to install tool {packageName}.\n");
        }

        return false;
    }

    /// <summary>
    /// Removes a scaffold tool from the manifest and uninstalls it.
    /// </summary>
    /// <param name="packageName">The name of the tool package to remove.</param>
    /// <param name="global">Whether the tool is installed globally.</param>
    /// <returns>True if the tool was removed and uninstalled; otherwise, false.</returns>
    public bool RemoveTool(string packageName, bool global)
    {
        _scaffolderLogger.LogInformation($"Uninstalling {packageName}...\n");

        if (_toolManifestService.RemoveTool(packageName))
        {
            if (_dotnetToolService.UninstallDotNetTool(packageName, global))
            {
                _scaffolderLogger.LogInformation($"Tool {packageName} removed successfully.\n");
                return true;
            }
            else
            {
                _scaffolderLogger.LogError($"Failed to uninstall tool {packageName}.\n");
            }
        }
        else
        {
            _scaffolderLogger.LogError($"Failed to remove tool {packageName} from manifest.\n");
        }

        return false;
    }

    /// <summary>
    /// Lists all scaffold tools currently in the manifest and displays them in a table.
    /// </summary>
    public void ListTools()
    {
        var manifest = _toolManifestService.GetManifest();

        var table = new Table();
        table.AddColumn("Name");

        foreach (var tool in manifest.Tools)
        {
            table.AddRow(tool.Name);
        }

        AnsiConsole.Write(table);
    }
}
