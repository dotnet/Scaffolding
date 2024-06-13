// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Scaffolders;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;

internal class CachingCommand(IFileSystem fileSystem, ILogger logger, IEnvironmentService environmentService) : AsyncCommand<CachingCommand.CachingCommandSettings>
{
    private readonly ILogger _logger = logger;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IEnvironmentService _environmentService = environmentService;

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] CachingCommandSettings settings)
    {
        if (!ValidateCachingCommandSettings(settings))
        {
            return -1;
        }

        CachingScaffolder scaffolder = new CachingScaffolder(
            settings.Type,
            settings.AppHostProject,
            settings.Project,
            settings.Prerelease,
            _logger,
            _fileSystem,
            _environmentService);

        await scaffolder.ExecuteAsync();

        return 0;
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

    public class CachingCommandSettings : CommandSettings
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
}
