// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

internal class FirstPartyComponentInitializer
{
    private readonly ILogger _logger;
    private readonly IDotNetToolService _dotnetToolService;
    private readonly List<string> _firstPartyTools = ["dotnet-scaffold-aspnet", "dotnet-scaffold-aspire"];
    public FirstPartyComponentInitializer(ILogger logger, IDotNetToolService dotnetToolService)
    {
        _logger = logger;
        _dotnetToolService = dotnetToolService;
    }

    public void Initialize()
    {
        List<string> toolsToInstall = [];
        foreach (var tool in _firstPartyTools)
        {
            if (_dotnetToolService.GetDotNetTool(tool) is null)
            {
                toolsToInstall.Add(tool);
            }
        }

        foreach (var tool in toolsToInstall)
        {
            _logger.LogMessage($"Installing {tool}!");
            var successfullyInstalled = _dotnetToolService.InstallDotNetTool(tool);
            if (!successfullyInstalled)
            {
                _logger.LogMessage($"Failed to install {tool}!");
            }
        }
    }
}
