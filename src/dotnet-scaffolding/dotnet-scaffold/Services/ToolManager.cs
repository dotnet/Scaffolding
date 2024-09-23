// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

internal class ToolManager(ILogger<ToolManager> logger, IToolManifestService toolManifestService, IDotNetToolService dotnetToolService) : IToolManager
{
    private readonly ILogger _logger = logger;
    private readonly IToolManifestService _toolManifestService = toolManifestService;
    private readonly IDotNetToolService _dotnetToolService = dotnetToolService;

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
