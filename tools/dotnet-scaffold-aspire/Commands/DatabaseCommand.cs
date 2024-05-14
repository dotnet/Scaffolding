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

            [CommandOption("--host-project <PROJECT>")]
            public required string Project { get; set; }

            [CommandOption("--api-project <APIPROJECT>")]
            public required string ApiProject { get; set; }
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

            if (string.IsNullOrEmpty(commandSettings.Project))
            {
                _logger.LogMessage("Missing/Invalid --project option.", LogMessageType.Error);
                return false;
            }

            if (string.IsNullOrEmpty(commandSettings.ApiProject))
            {
                _logger.LogMessage("Missing/Invalid --api-project option.", LogMessageType.Error);
                return false;
            }

            return true;
        }

        internal void InstallPackages(DatabaseCommandSettings commandSettings)
        {
            PackageConstants.DatabasePackages.DatabasePackagesAppHostDict.TryGetValue(commandSettings.Type, out string? appHostPackageName);
            if (_fileSystem.FileExists(commandSettings.Project) && !string.IsNullOrEmpty(appHostPackageName))
            {
                DotnetCommands.AddPackage(
                    packageName: appHostPackageName,
                    logger: _logger,
                    projectFile: commandSettings.Project);
            }

            PackageConstants.DatabasePackages.DatabasePackagesAppHostDict.TryGetValue(commandSettings.Type, out string? apiServicePackageName);
            if (_fileSystem.FileExists(commandSettings.ApiProject) && !string.IsNullOrEmpty(apiServicePackageName))
            {
                DotnetCommands.AddPackage(
                    packageName: apiServicePackageName,
                    logger: _logger,
                    projectFile: commandSettings.ApiProject);
            }
        }
    }
}
