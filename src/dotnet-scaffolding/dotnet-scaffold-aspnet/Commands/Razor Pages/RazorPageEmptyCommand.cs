// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.RazorPage;

internal class RazorPageEmptyCommand : Command<RazorPageEmptyCommand.RazorPageEmptySettings>
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    public RazorPageEmptyCommand(IFileSystem fileSystem, ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] RazorPageEmptySettings settings)
    {
        if (!ValidateRazorPageEmptyCommandSettings(settings))
        {
            return -1;
        }

        _logger.LogMessage("Adding Razor Page...");
        var addingPageResult = AddEmptyRazorPage(settings);

        if (addingPageResult)
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

    private bool AddEmptyRazorPage(RazorPageEmptySettings settings)
    {
        var projectBasePath = Path.GetDirectoryName(settings.Project);
        if (Directory.Exists(projectBasePath))
        {
            var razorPageName = settings.Name;
            //arguments for 'dotnet new razorpage'
            var args = new List<string>()
            {
                "razorpage",
                "--name",
                razorPageName,
                "--output",
                projectBasePath
            };

            var runner = DotnetCliRunner.CreateDotNet("new", args);
            var exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
            return exitCode == 0;
        }

        return false;
    }

    private bool ValidateRazorPageEmptyCommandSettings(RazorPageEmptySettings commandSettings)
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

    public class RazorPageEmptySettings : CommandSettings
    {
        [CommandOption("--project <PROJECT>")]
        public string? Project { get; set; }

        [CommandOption("--name <NAME>")]
        public required string Name { get; set; }
    }
}
