// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands
{
    public class CachingCommand : AsyncCommand<CachingCommand.CachingCommandSettings>
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentService _environmentService;
        public CachingCommand(IFileSystem fileSystem, ILogger logger, IEnvironmentService environmentService)
        {
            _environmentService = environmentService;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] CachingCommandSettings settings)
        {
            if (!ValidateCachingCommandSettings(settings))
            {
                return -1;
            }

            InstallPackages(settings);
            return await UpdateAppHostAsync(settings) && await UpdateWebAppAsync(settings) ? 0 : 1;
        }

        public class CachingCommandSettings : CommandSettings
        {
            [CommandOption("--type <TYPE>")]
            public required string Type { get; set; }

            [CommandOption("--host-project <PROJECT>")]
            public required string Project { get; set; }

            [CommandOption("--web-project <WEBPROJECT>")]
            public required string WebProject { get; set; }
        }

        internal async Task<bool> UpdateAppHostAsync(CachingCommandSettings commandSettings)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("redis-apphost.json", System.Reflection.Assembly.GetExecutingAssembly());
            var workspaceSettings = new WorkspaceSettings
            {
                InputPath = commandSettings.Project
            };

            var hostAppSettings = new AppSettings();
            hostAppSettings.AddSettings("workspace", workspaceSettings);
            var codeService = new CodeService(hostAppSettings, _logger);
            CodeChangeOptions options = new()
            {
                IsMinimalApp = await ProjectModifierHelper.IsMinimalApp(codeService),
                UsingTopLevelsStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(codeService)
            };

            var projectModifier = new ProjectModifier(
                _environmentService,
                hostAppSettings,
                codeService,
                _logger,
                options,
                config);
            return await projectModifier.RunAsync();
        }

        internal async Task<bool> UpdateWebAppAsync(CachingCommandSettings commandSettings)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            CodeModifierConfig? config = ProjectModifierHelper.GetCodeModifierConfig("redis-webapp.json", System.Reflection.Assembly.GetExecutingAssembly());
            var workspaceSettings = new WorkspaceSettings
            {
                InputPath = commandSettings.WebProject
            };

            var hostAppSettings = new AppSettings();
            hostAppSettings.AddSettings("workspace", workspaceSettings);
            var codeService = new CodeService(hostAppSettings, _logger);
            CodeChangeOptions options = new()
            {
                IsMinimalApp = await ProjectModifierHelper.IsMinimalApp(codeService),
                UsingTopLevelsStatements = await ProjectModifierHelper.IsUsingTopLevelStatements(codeService)
            };

            var projectModifier = new ProjectModifier(
            _environmentService,
            hostAppSettings,
            codeService,
            _logger,
            options,
            config);
            return await projectModifier.RunAsync();
        }

        internal bool ValidateCachingCommandSettings(CachingCommandSettings commandSettings)
        {
            if (string.IsNullOrEmpty(commandSettings.Type) || !GetCmdsHelper.CachingTypeCustomValues.Contains(commandSettings.Type, StringComparer.OrdinalIgnoreCase))
            {
                string cachingTypeDisplayList = string.Join(", ", GetCmdsHelper.CachingTypeCustomValues.GetRange(0, GetCmdsHelper.CachingTypeCustomValues.Count - 1)) +
                    (GetCmdsHelper.CachingTypeCustomValues.Count > 1 ? " and " : "") + GetCmdsHelper.CachingTypeCustomValues[GetCmdsHelper.CachingTypeCustomValues.Count - 1];
                _logger.LogMessage("Missing/Invalid --type option.", LogMessageType.Error);
                _logger.LogMessage($"Valid options : {cachingTypeDisplayList}", LogMessageType.Error);
                return false;
            }

            if (string.IsNullOrEmpty(commandSettings.Project))
            {
                _logger.LogMessage("Missing/Invalid --apphost-project option.", LogMessageType.Error);
                return false;
            }

            if (string.IsNullOrEmpty(commandSettings.WebProject))
            {
                _logger.LogMessage("Missing/Invalid --web-project option.", LogMessageType.Error);
                return false;
            }

            return true;
        }

        internal void InstallPackages(CachingCommandSettings commandSettings)
        {
            if (_fileSystem.FileExists(commandSettings.Project))
            {
                DotnetCommands.AddPackage(
                    packageName: PackageConstants.CachingPackages.AppHostRedisPackageName,
                    logger: _logger,
                    projectFile: commandSettings.Project);
            }

            PackageConstants.CachingPackages.CachingPackagesDict.TryGetValue(commandSettings.Type, out string? webAppPackageName);
            if (_fileSystem.FileExists(commandSettings.WebProject) && !string.IsNullOrEmpty(webAppPackageName))
            {
                DotnetCommands.AddPackage(
                    packageName: webAppPackageName,
                    logger: _logger,
                    projectFile: commandSettings.WebProject);
            }
        }
    }
}
