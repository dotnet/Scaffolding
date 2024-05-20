// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands
{
    internal class DatabaseCommand : Command<DatabaseCommand.DatabaseCommandSettings>
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public DatabaseCommand(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }
        public override int Execute([NotNull] CommandContext context, [NotNull] DatabaseCommandSettings settings)
        {
            if (!ValidateDatabaseCommandSettings(settings))
            {
                return -1;
            }

            InstallPackages(settings);
            return 0;
        }

        public class DatabaseCommandSettings : CommandSettings
        {
            [CommandOption("--type <TYPE>")]
            public required string Type { get; set; }

            [CommandOption("--apphost-project <APPHOSTPROJECT>")]
            public required string AppHostProject { get; set; }

            [CommandOption("--project <PROJECT>")]
            public required string Project { get; set; }

            [CommandOption("--prerelease")]
            public required bool Prerelease { get; set; }
        }

        internal bool ValidateDatabaseCommandSettings(DatabaseCommandSettings commandSettings)
        {
            if (string.IsNullOrEmpty(commandSettings.Type) || !GetCmdsHelper.DatabaseTypeCustomValues.Contains(commandSettings.Type, StringComparer.OrdinalIgnoreCase))
            {
                string dbTypeDisplayList = string.Join(", ", GetCmdsHelper.DatabaseTypeCustomValues.GetRange(0, GetCmdsHelper.DatabaseTypeCustomValues.Count - 1)) +
                    (GetCmdsHelper.DatabaseTypeCustomValues.Count > 1 ? " and " : "") + GetCmdsHelper.DatabaseTypeCustomValues[GetCmdsHelper.DatabaseTypeCustomValues.Count - 1];
                _logger.LogMessage("Missing/Invalid --type option.", LogMessageType.Error);
                _logger.LogMessage($"Valid options : {dbTypeDisplayList}", LogMessageType.Error);
                return false;
            }

            if (string.IsNullOrEmpty(commandSettings.AppHostProject))
            {
                _logger.LogMessage("Missing/Invalid --apphost-project option.", LogMessageType.Error);
                return false;
            }

            if (string.IsNullOrEmpty(commandSettings.Project))
            {
                _logger.LogMessage("Missing/Invalid --project option.", LogMessageType.Error);
                return false;
            }

            return true;
        }

        internal void InstallPackages(DatabaseCommandSettings commandSettings)
        {
            PackageConstants.DatabasePackages.DatabasePackagesAppHostDict.TryGetValue(commandSettings.Type, out string? appHostPackageName);
            if (_fileSystem.FileExists(commandSettings.AppHostProject) && !string.IsNullOrEmpty(appHostPackageName))
            {
                DotnetCommands.AddPackage(
                    packageName: appHostPackageName,
                    logger: _logger,
                    projectFile: commandSettings.AppHostProject,
                    includePrerelease: commandSettings.Prerelease);
            }

            PackageConstants.DatabasePackages.DatabasePackagesApiServiceDict.TryGetValue(commandSettings.Type, out string? projectPackageName);
            if (_fileSystem.FileExists(commandSettings.Project) && !string.IsNullOrEmpty(projectPackageName))
            {
                DotnetCommands.AddPackage(
                    packageName: projectPackageName,
                    logger: _logger,
                    projectFile: commandSettings.Project,
                    includePrerelease: commandSettings.Prerelease);
            }
        }
    }
}
