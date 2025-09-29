// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Scaffold step for invoking 'dotnet new' to add new files (e.g., Razor pages, components, views) to a project.
/// </summary>
internal class DotnetNewScaffolderStep : ScaffoldStep
{
    /// <summary>
    /// Gets or sets the project file path.
    /// </summary>
    public string? ProjectPath { get; set; }
    /// <summary>
    /// Gets or sets the dotnet new command name (e.g., 'page', 'view').
    /// </summary>
    public required string CommandName { get; set; }
    /// <summary>
    /// Gets or sets the namespace for the new file.
    /// </summary>
    public string? NamespaceName { get; set; }
    /// <summary>
    /// Gets or sets the file name for the new file.
    /// </summary>
    public string? FileName { get; set; }
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetNewScaffolderStep"/> class.
    /// </summary>
    public DotnetNewScaffolderStep(ILogger<DotnetNewScaffolderStep> logger, IFileSystem fileSystem, ITelemetryService telemetryService)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _telemetryService = telemetryService;
    }

    /// <inheritdoc />
    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Adding '{CommandName}' using 'dotnet new'...");
        var result = false;
        var stepSettings = ValidateDotnetNewCommandSettings();
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

        _telemetryService.TrackEvent(new DotnetNewScaffolderTelemetryEvent(context.Scaffolder.DisplayName, stepSettings is not null, result));
        return Task.FromResult(result);
    }

    /// <summary>
    /// Invokes the 'dotnet new' command with the specified settings.
    /// </summary>
    private bool InvokeDotnetNew(DotnetNewStepSettings stepSettings)
    {
        var outputDirectory = Path.GetDirectoryName(stepSettings.Project);
        //invoking a command with a specific output folder, if not, use the default output folder (project root)
        if (!string.IsNullOrEmpty(outputDirectory) &&
            OutputFolders.TryGetValue(stepSettings.CommandName, out var folderName))
        {
            outputDirectory = Path.Combine(outputDirectory, folderName);
            _fileSystem.CreateDirectoryIfNotExists(outputDirectory);
        }

        var projectName = Path.GetFileNameWithoutExtension(stepSettings.Project);
        if (!string.IsNullOrEmpty(outputDirectory) &&
            _fileSystem.DirectoryExists(outputDirectory))
        {
            //arguments for 'dotnet new {settings.CommandName}'
            var args = new List<string>()
            {
                stepSettings.CommandName,
                "--name",
                stepSettings.Name,
                "--output",
                outputDirectory
            };

            if (!string.IsNullOrEmpty(stepSettings.NamespaceName))
            {
                args.Add("--namespace");
                args.Add(stepSettings.NamespaceName);
            }

            var runner = DotnetCliRunner.CreateDotNet("new", args);
            var exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
            return exitCode == 0;
        }

        return false;
    }

    /// <summary>
    /// Validates the dotnet new command settings and returns the settings object if valid.
    /// </summary>
    private DotnetNewStepSettings? ValidateDotnetNewCommandSettings()
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

        return new DotnetNewStepSettings
        {
            Project = ProjectPath,
            Name = FileName,
            NamespaceName = NamespaceName,
            CommandName = CommandName
        };
    }

    private readonly static Dictionary<string, string> OutputFolders = new()
    {
        { Constants.DotnetCommands.RazorPageCommandName, Constants.DotnetCommands.RazorPageCommandOutput },
        { Constants.DotnetCommands.RazorComponentCommandName, Constants.DotnetCommands.RazorComponentCommandOutput },
        { Constants.DotnetCommands.ViewCommandName, Constants.DotnetCommands.ViewCommandOutput }
    };
}
