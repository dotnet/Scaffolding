// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class DotnetNewScaffolderStep : ScaffoldStep
{
    public string? ProjectPath { get; set; }
    public required string CommandName { get; set; }
    public string? FileName { get; set; }
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public DotnetNewScaffolderStep(ILogger<DotnetNewScaffolderStep> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

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

        return Task.FromResult(result);
    }

    private bool InvokeDotnetNew(DotnetNewStepSettings stepSettings)
    {
        var projectBasePath = Path.GetDirectoryName(stepSettings.Project);
        //using the project name from the csproj path as namespace currently.
        //to evaluate the correct namespace is a lot of overhead for a simple 'dotnet new' operation
        //TODO maybe change this?
        var projectName = Path.GetFileNameWithoutExtension(stepSettings.Project);
        if (Directory.Exists(projectBasePath) &&
            !string.IsNullOrEmpty(projectBasePath) &&
            !string.IsNullOrEmpty(projectName))
        {
            //arguments for 'dotnet new {settings.CommandName}'
            var args = new List<string>()
            {
                stepSettings.CommandName,
                "--name",
                stepSettings.Name,
                "--namespace",
                projectName,
                "--output",
                projectBasePath
            };

            var runner = DotnetCliRunner.CreateDotNet("new", args);
            var exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
            return exitCode == 0;
        }

        return false;
    }

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
            CommandName = CommandName
        };
    }
}
