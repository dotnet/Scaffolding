// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Helpers.Services;

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

    public void Initialize()
    {
        List<string> toolsToInstall = [];
        var installedTools = _dotnetToolService.GetDotNetTools(refresh: true);
        foreach (var tool in _firstPartyTools)
        {
            if (installedTools.FirstOrDefault(x => x.PackageName.Equals(tool)) is null)
            {
                toolsToInstall.Add(tool);
            }
        }

        foreach (var tool in toolsToInstall)
        {
            _logger.LogMessage($"Installing {tool}!");
            var successfullyInstalled = _dotnetToolService.InstallDotNetTool(tool, prerelease: true);
            if (!successfullyInstalled)
            {
                _logger.LogMessage($"Failed to install {tool}!");
            }
        }
    }
}
