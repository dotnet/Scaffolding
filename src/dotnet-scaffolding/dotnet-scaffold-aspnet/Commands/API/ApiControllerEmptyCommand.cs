// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.API;

internal class ApiControllerEmptyCommand : Command<ApiControllerEmptyCommand.ApiControllerEmptySettings>
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    public ApiControllerEmptyCommand(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] ApiControllerEmptySettings settings)
    {
        if (!ValidateApiControllerEmptyCommandSettings(settings))
        {
            return -1;
        }

        _logger.LogMessage("Adding API Controller...");
        var addingComponentResult = AddEmptyApiController(settings);

        if (addingComponentResult)
        {
            _logger.LogMessage("Finished");
            return 0;
        }
        else
        {
            _logger.LogMessage("An error occurred.");
            return -1;
        }
    }

    private bool AddEmptyApiController(ApiControllerEmptySettings settings)
    {
        var projectBasePath = Path.GetDirectoryName(settings.Project);
        var projectName = Path.GetFileNameWithoutExtension(settings.Project);
        if (!string.IsNullOrEmpty(projectName) && Directory.Exists(projectBasePath))
        {
            var apiControllerName = settings.Name;
            var actionsParameter = settings.Actions ? "--actions" : string.Empty;
            //arguments for 'dotnet new apicontroller'
            var args = new List<string>()
            {
                "apicontroller",
                "--name",
                apiControllerName,
                "--output",
                projectBasePath,
                "--namespace",
                projectName,
                actionsParameter
            };

            var runner = DotnetCliRunner.CreateDotNet("new", args);
            var exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
            return exitCode == 0;
        }

        return false;
    }

    private bool ValidateApiControllerEmptyCommandSettings(ApiControllerEmptySettings commandSettings)
    {
        if (string.IsNullOrEmpty(commandSettings.Project) || !_fileSystem.FileExists(commandSettings.Project))
        {
            _logger.LogMessage("Missing/Invalid --project option.", LogMessageType.Error);
            return false;
        }

        if (string.IsNullOrEmpty(commandSettings.Name))
        {
            _logger.LogMessage("Missing/Invalid --name option.", LogMessageType.Error);
            return false;
        }

        return true;
    }

    public class ApiControllerEmptySettings : CommandSettings
    {
        [CommandOption("--project <PROJECT>")]
        public string? Project { get; init; }

        [CommandOption("--name <NAME>")]
        public required string Name { get; init; }

        [CommandOption("--actions <ACTIONS>")]
        public required bool Actions { get; init; }
    }
}
