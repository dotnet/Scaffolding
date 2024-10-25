// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class EmptyControllerScaffolderStep : ScaffoldStep
{
    public string? ProjectPath { get; set; }
    public required string CommandName { get; set; }
    public string? FileName { get; set; }
    public bool Actions { get; set; }
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ITelemetryService _telemetryService;
    public EmptyControllerScaffolderStep(
        ILogger<EmptyControllerScaffolderStep> logger,
        IFileSystem fileSystem,
        ITelemetryService telemetryService)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _telemetryService = telemetryService;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Adding '{CommandName}' using 'dotnet new'...");
        var stepSettings = ValidateEmptyControllerCommandSettings();
        var result = false;
        if (stepSettings is not null)
        {
            result = InvokeDotnetNew(stepSettings);
        }

        if (result)
        {
            _logger.LogInformation("Done");
        }
        else
        {
            _logger.LogError("Failed");
        }

        _telemetryService.TrackEvent(new EmptyControllerScaffolderTelemetryEvent(context.Scaffolder.DisplayName, Actions, stepSettings is not null, result));
        return Task.FromResult(result);
    }

    private bool InvokeDotnetNew(EmptyControllerStepSettings settings)
    {
        var projectBasePath = Path.GetDirectoryName(settings.Project);
        var projectName = Path.GetFileNameWithoutExtension(settings.Project);
        var actionsParameter = settings.Actions ? "--actions" : string.Empty;
        if (Directory.Exists(projectBasePath) && !string.IsNullOrEmpty(projectName))
        {
            //arguments for 'dotnet new {settings.CommandName}'
            var args = new List<string>()
            {
                settings.CommandName,
                "--name",
                settings.Name,
                "--output",
                projectBasePath,
                "--namespace",
                projectName,
            };

            if (!string.IsNullOrEmpty(actionsParameter))
            {
                args.Add(actionsParameter);
            }

            var runner = DotnetCliRunner.CreateDotNet("new", args);
            var exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
            return exitCode == 0;
        }

        return false;
    }

    private EmptyControllerStepSettings? ValidateEmptyControllerCommandSettings()
    {
        if (string.IsNullOrEmpty(ProjectPath) || !_fileSystem.FileExists(ProjectPath))
        {
            _logger.LogInformation("Missing/Invalid --project option.");
            return null;
        }

        if (string.IsNullOrEmpty(FileName))
        {
            _logger.LogInformation("Missing/Invalid --name option.");
            return null;
        }
        else
        {
            //Component names cannot start with a lowercase character, using CurrentCulture to capitalize the first letter
            FileName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(FileName);
        }

        return new EmptyControllerStepSettings
        {
            Project = ProjectPath,
            Name = FileName,
            Actions = Actions,
            CommandName = CommandName
        };
    }
}
