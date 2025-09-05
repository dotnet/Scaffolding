// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    IDotNetToolService dotnetToolService) : IToolManager
{
    // Logger for recording informational and error messages.
    private readonly ILogger _logger = logger;
    // Service for managing the tool manifest file.
    private readonly IToolManifestService _toolManifestService = toolManifestService;
    // Service for installing and uninstalling dotnet tools.
    private readonly IDotNetToolService _dotnetToolService = dotnetToolService;

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
        _logger.LogInformation("Installing {packageName}...", packageName);

        if (_dotnetToolService.GetDotNetTool(packageName) is not null || _dotnetToolService.InstallDotNetTool(packageName, version, global: global, prerelease, addSources, configFile))
        {
            if (_toolManifestService.AddTool(packageName))
            {
                _logger.LogInformation("Tool {packageName} installed successfully", packageName);
                return true;
            }
            else
            {
                _logger.LogError("Failed to add tool {packageName} to manifest", packageName);
            }
        }
        else
        {
            _logger.LogError("Failed to install tool {packageName}", packageName);
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
        _logger.LogInformation("Uninstalling {packageName}...", packageName);

        if (_toolManifestService.RemoveTool(packageName))
        {
            if (_dotnetToolService.UninstallDotNetTool(packageName, global))
            {
                _logger.LogInformation("Tool {packageName} removed successfully", packageName);
                return true;
            }
            else
            {
                _logger.LogError("Failed to uninstall tool {packageName}", packageName);
            }
        }
        else
        {
            _logger.LogError("Failed to remove tool {packageName} from manifest", packageName);
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
