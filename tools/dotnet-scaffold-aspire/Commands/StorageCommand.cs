// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;

internal class StorageCommand : Command<StorageCommand.StorageCommandSettings>
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public StorageCommand(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] StorageCommandSettings settings)
    {
        if (!ValidateStorageCommandSettings(settings))
        {
            return -1;
        }

        InstallPackages(settings);
        return 0;
    }

    public class StorageCommandSettings : CommandSettings
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

    internal bool ValidateStorageCommandSettings(StorageCommandSettings commandSettings)
    {
        if (string.IsNullOrEmpty(commandSettings.Type) || !GetCmdsHelper.StorageTypeCustomValues.Contains(commandSettings.Type, StringComparer.OrdinalIgnoreCase))
        {
            string storageTypeDisplayList = string.Join(", ", GetCmdsHelper.StorageTypeCustomValues.GetRange(0, GetCmdsHelper.StorageTypeCustomValues.Count - 1)) +
                (GetCmdsHelper.StorageTypeCustomValues.Count > 1 ? " and " : "") + GetCmdsHelper.StorageTypeCustomValues[GetCmdsHelper.StorageTypeCustomValues.Count - 1];
            _logger.LogMessage("Missing/Invalid --type option.", LogMessageType.Error);
            _logger.LogMessage($"Valid options : {storageTypeDisplayList}", LogMessageType.Error);
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

    internal void InstallPackages(StorageCommandSettings commandSettings)
    {
        if (_fileSystem.FileExists(commandSettings.AppHostProject))
        {
            DotnetCommands.AddPackage(
                packageName: PackageConstants.StoragePackages.AppHostStoragePackageName,
                logger: _logger,
                projectFile: commandSettings.AppHostProject,
                includePrerelease: commandSettings.Prerelease);
        }

        PackageConstants.StoragePackages.StoragePackagesDict.TryGetValue(commandSettings.Type, out string? projectPackageName);
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
