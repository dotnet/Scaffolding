// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Ensures that required first-party scaffold tools are installed and available for use.
/// </summary>
internal class FirstPartyComponentInitializer
{
    // Logger for recording informational and error messages.
    private readonly ILogger _logger;
    // Service for managing .NET CLI tools.
    private readonly IDotNetToolService _dotnetToolService;
    // List of required first-party tool package names.
    private readonly List<string> _firstPartyTools = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FirstPartyComponentInitializer"/> class.
    /// </summary>
    /// <param name="logger">Logger for informational and error messages.</param>
    /// <param name="dotnetToolService">Service for managing .NET CLI tools.</param>
    public FirstPartyComponentInitializer(ILogger logger, IDotNetToolService dotnetToolService)
    {
        _logger = logger;
        _dotnetToolService = dotnetToolService;
    }

    /// <summary>
    /// Installs any missing first-party scaffold tools required by the CLI.
    /// </summary>
    /// <param name="envVars">Environment variables to use during tool installation.</param>
    public void Initialize(IDictionary<string, string> envVars)
    {
        List<string> toolsToInstall = [];
        var installedTools = _dotnetToolService.GetDotNetTools(refresh: true, envVars);
        foreach (var tool in _firstPartyTools)
        {
            if (installedTools.FirstOrDefault(x => x.PackageName.Equals(tool, System.StringComparison.OrdinalIgnoreCase)) is null)
            {
                toolsToInstall.Add(tool);
            }
        }

        if (toolsToInstall.Count != 0)
        {
            var isDotnetScaffoldPrerelease = ToolHelper.IsToolPrerelease();
            foreach (var tool in toolsToInstall)
            {
                _logger.LogInformation("Installing {tool}.", tool);
                var successfullyInstalled = _dotnetToolService.InstallDotNetTool(tool, prerelease : isDotnetScaffoldPrerelease);
                if (!successfullyInstalled)
                {
                    _logger.LogInformation("Failed to install {tool}.", tool);
                }
            }
        }
    }
}
