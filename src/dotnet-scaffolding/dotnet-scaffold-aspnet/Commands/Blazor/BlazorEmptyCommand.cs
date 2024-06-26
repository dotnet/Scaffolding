// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Spectre.Console.Cli;

using Microsoft.DotNet.Scaffolding.Helpers.General;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor;

internal class BlazorEmptyCommand : Command<BlazorEmptyCommand.BlazorEmptySettings>
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    public BlazorEmptyCommand(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] BlazorEmptySettings settings)
    {
        if (!ValidateBlazorEmptyCommandSettings(settings))
        {
            return -1;
        }

        _logger.LogMessage("Adding Razor Component...");
        var addingComponentResult = AddEmptyRazorComponent(settings);

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

    private bool AddEmptyRazorComponent(BlazorEmptySettings settings)
    {
        var projectBasePath = Path.GetDirectoryName(settings.Project);
        if (Directory.Exists(projectBasePath))
        {
            var razorComponentName = settings.Name;
            //arguments for 'dotnet new razorcomponent'
            var args = new List<string>()
            {
                "razorcomponent",
                "--name",
                razorComponentName,
                "--output",
                projectBasePath
            };

            var runner = DotnetCliRunner.CreateDotNet("new", args);
            var exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
            return exitCode == 0;
        }

        return false;
    }

    private bool ValidateBlazorEmptyCommandSettings(BlazorEmptySettings commandSettings)
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

    public class BlazorEmptySettings : CommandSettings
    {
        [CommandOption("--project <PROJECT>")]
        public string? Project { get; set; }

        [CommandOption("--name <NAME>")]
        public required string Name { get; set; }
    }
}
