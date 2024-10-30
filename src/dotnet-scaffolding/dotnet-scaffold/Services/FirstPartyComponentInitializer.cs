// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

internal class FirstPartyComponentInitializer
{
    private readonly ILogger _logger;
    private readonly IDotNetToolService _dotnetToolService;
    private readonly List<string> _firstPartyTools = ["Microsoft.dotnet-scaffold-aspnet", "Microsoft.dotnet-scaffold-aspire"];
    public FirstPartyComponentInitializer(ILogger logger, IDotNetToolService dotnetToolService)
    {
        _logger = logger;
        _dotnetToolService = dotnetToolService;
    }

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
