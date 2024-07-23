// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Steps;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;

internal interface ICommandWithSettings
{
    Task<int> ExecuteAsync(CommandSettings settings);
}

internal class CachingCommand : ICommandWithSettings
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public CachingCommand(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(CommandSettings settings)
    {
        new MsBuildInitializer(_logger).Initialize();
        if (!ValidateCachingCommandSettings(settings))
        {
            return -1;
        }

        _logger.LogInformation("Installing packages...");
        await InstallPackagesAsync(settings);

        _logger.LogInformation("Updating App host project...");
        var appHostResult = await UpdateAppHostAsync(settings);

        _logger.LogInformation("Updating web/worker project...");
        var workerResult = await UpdateWebAppAsync(settings);

        if (appHostResult && workerResult)
        {
            _logger.LogInformation("Finished");
            return 0;
        }
        else
        {
            _logger.LogInformation("An error occurred.");
            return -1;
        }
    }

    internal async Task<bool> UpdateAppHostAsync(CommandSettings commandSettings)
    {
        CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("redis-apphost.json", System.Reflection.Assembly.GetExecutingAssembly());
        if (config is null)
        {
            _logger.LogInformation("Unable to parse 'redis-apphost.json' CodeModifierConfig.");
            return false;
        }

        var workspaceSettings = new WorkspaceSettings
        {
            InputPath = commandSettings.AppHostProject
        };

        var hostAppSettings = new AppSettings();
        hostAppSettings.AddSettings("workspace", workspaceSettings);
        var codeService = new CodeService(hostAppSettings, _logger);
        var codeModifierProperties = await GetCodeModifierPropertiesAsync(commandSettings, codeService);
        CodeChangeStep codeChangeStep = new()
        {
            CodeModifierConfig = config,
            CodeModifierProperties = codeModifierProperties,
            CodeService = codeService,
            Logger = _logger,
            ProjectPath = commandSettings.AppHostProject,
        };

        return await codeChangeStep.ExecuteAsync();
    }

    internal async Task<bool> UpdateWebAppAsync(CommandSettings commandSettings)
    {
        var configName = commandSettings.Type.Equals("redis-with-output-caching", StringComparison.OrdinalIgnoreCase) ? "redis-webapp-oc.json" : "redis-webapp.json";
        CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig(configName, System.Reflection.Assembly.GetExecutingAssembly());
        if (config is null)
        {
            _logger.LogInformation($"Unable to parse '{configName}' CodeModifierConfig.");
            return false; 
        }

        CodeChangeStep codeChangeStep = new()
        {
            CodeModifierConfig = config,
            CodeModifierProperties = new Dictionary<string, string>(),
            Logger = _logger,
            ProjectPath = commandSettings.Project,
        };

        return await codeChangeStep.ExecuteAsync();
    }

    internal bool ValidateCachingCommandSettings(CommandSettings commandSettings)
    {
        if (string.IsNullOrEmpty(commandSettings.Type) || !GetCmdsHelper.CachingTypeCustomValues.Contains(commandSettings.Type, StringComparer.OrdinalIgnoreCase))
        {
            string cachingTypeDisplayList = string.Join(", ", GetCmdsHelper.CachingTypeCustomValues.GetRange(0, GetCmdsHelper.CachingTypeCustomValues.Count - 1)) +
                (GetCmdsHelper.CachingTypeCustomValues.Count > 1 ? " and " : "") + GetCmdsHelper.CachingTypeCustomValues[GetCmdsHelper.CachingTypeCustomValues.Count - 1];
            _logger.LogError("Missing/Invalid --type option.");
            _logger.LogError($"Valid options : {cachingTypeDisplayList}");
            return false;
        }

        if (string.IsNullOrEmpty(commandSettings.AppHostProject))
        {
            _logger.LogError("Missing/Invalid --apphost-project option.");
            return false;
        }

        if (string.IsNullOrEmpty(commandSettings.Project))
        {
            _logger.LogError("Missing/Invalid --project option.");
            return false;
        }

        return true;
    }

    internal async Task InstallPackagesAsync(CommandSettings commandSettings)
    {
        List<AddPackagesStep> packageSteps = [];
        var appHostPackageStep = new AddPackagesStep
        {
            PackageNames = [PackageConstants.CachingPackages.AppHostRedisPackageName],
            ProjectPath = commandSettings.AppHostProject,
            Prerelease = commandSettings.Prerelease,
            Logger = _logger
        };

        packageSteps.Add(appHostPackageStep);
        if (PackageConstants.CachingPackages.CachingPackagesDict.TryGetValue(commandSettings.Type, out string? projectPackageName))
        {
            var workerProjPackageStep = new AddPackagesStep
            {
                PackageNames = [projectPackageName],
                ProjectPath = commandSettings.Project,
                Prerelease = commandSettings.Prerelease,
                Logger = _logger
            };

            packageSteps.Add(workerProjPackageStep);
        }

        foreach (var packageStep in packageSteps)
        {
            await packageStep.ExecuteAsync();
        }
    }

    internal async Task<Dictionary<string, string>> GetCodeModifierPropertiesAsync(CommandSettings commandSettings, ICodeService codeService)
    {
        var codeModifierProperties = new Dictionary<string, string>();
        var autoGenProjectNames = await AspireHelpers.GetAutoGeneratedProjectNamesAsync(commandSettings.AppHostProject, codeService);
        //add the web worker project name
        if (autoGenProjectNames.TryGetValue(commandSettings.Project, out var autoGenProjectName))
        {
            codeModifierProperties.Add("$(AutoGenProjectName)", autoGenProjectName);
        }

        return codeModifierProperties;
    }
}
