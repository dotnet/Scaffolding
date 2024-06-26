// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MVC;

internal class MvcControllerEmptyCommand : Command<MvcControllerEmptyCommand.MvcControllerEmptySettings>
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    public MvcControllerEmptyCommand(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] MvcControllerEmptySettings settings)
    {
        if (!ValidateMvcControllerEmptyCommandSettings(settings))
        {
            return -1;
        }

        _logger.LogMessage("Adding MVC Controller...");
        var addingComponentResult = AddEmptyMvcController(settings);

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

    private bool AddEmptyMvcController(MvcControllerEmptySettings settings)
    {
        var projectBasePath = Path.GetDirectoryName(settings.Project);
        var projectName = Path.GetFileNameWithoutExtension(settings.Project);
        if (!string.IsNullOrEmpty(projectName) && Directory.Exists(projectBasePath))
        {
            var mvcControllerName = settings.Name;
            var actionsParameter = settings.Actions ? "--actions" : string.Empty;
            //arguments for 'dotnet new mvccontroller'
            var args = new List<string>()
            {
                "mvccontroller",
                "--name",
                mvcControllerName,
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

    private bool ValidateMvcControllerEmptyCommandSettings(MvcControllerEmptySettings commandSettings)
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

    public class MvcControllerEmptySettings : CommandSettings
    {
        [CommandOption("--project <PROJECT>")]
        public string? Project { get; init; }

        [CommandOption("--name <NAME>")]
        public required string Name { get; init; }

        [CommandOption("--actions <ACTIONS>")]
        public required bool Actions { get; init; }
    }
}
