// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands
{
    public class CachingCommand : Command<CachingCommand.CachingCommandSettings>
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IAppSettings _appSettings;
        public CachingCommand(IFileSystem fileSystem, ILogger logger, IAppSettings appSettings)
        {
            _appSettings = appSettings;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] CachingCommandSettings settings)
        {
            if (!ValidateCachingCommandSettings(settings))
            {
                return -1;
            }

            InstallPackages(settings);
            return 0;
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

        internal bool ValidateCachingCommandSettings(CachingCommandSettings commandSettings)
        {
            InitializeCommand();
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
                _logger.LogMessage("Missing/Invalid --project option.", LogMessageType.Error);
                return false;
            }

            if (string.IsNullOrEmpty(commandSettings.WebProject))
            {
                _logger.LogMessage("Missing/Invalid --web-project option.", LogMessageType.Error);
                return false;
            }

            return true;
        }

        internal void InitializeCommand()
        {
            AnsiConsole.Status()
            .WithSpinner()
            .Start("Initializing dotnet-scaffold", statusContext =>
            {
                statusContext.Refresh();
                //add 'workspace' settings
                var workspaceSettings = new WorkspaceSettings();
                _appSettings.AddSettings("workspace", workspaceSettings);
            });
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
