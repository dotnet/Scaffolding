// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Validates Healthcare Tracker settings and initializes the scaffolding model.
/// </summary>
internal class ValidateHealthcareTrackerStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Path to the project file.
    /// </summary>
    public string? Project { get; set; }

    /// <summary>
    /// Indicates if prerelease packages should be used.
    /// </summary>
    public bool Prerelease { get; set; }

    public ValidateHealthcareTrackerStep(
        IFileSystem fileSystem,
        ILogger<ValidateHealthcareTrackerStep> logger,
        ITelemetryService telemetryService)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var settings = ValidateSettings();
        if (settings is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
                nameof(ValidateHealthcareTrackerStep),
                context.Scaffolder.DisplayName,
                result: false));
            return false;
        }

        context.Properties.Add(nameof(HealthcareTrackerSettings), settings);

        _logger.LogInformation("Initializing scaffolding model...");
        var model = await GetHealthcareTrackerModelAsync(context, settings);
        if (model is null)
        {
            _logger.LogError("An error occurred.");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
                nameof(ValidateHealthcareTrackerStep),
                context.Scaffolder.DisplayName,
                result: false));
            return false;
        }

        context.Properties.Add(nameof(HealthcareTrackerModel), model);
        context.Properties.Add(Constants.StepConstants.CodeModifierProperties, new Dictionary<string, string>());

        var projectBasePath = Path.GetDirectoryName(settings.Project);
        if (!string.IsNullOrEmpty(projectBasePath))
        {
            context.Properties.Add(Constants.StepConstants.BaseProjectPath, projectBasePath);
        }

        _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
            nameof(ValidateHealthcareTrackerStep),
            context.Scaffolder.DisplayName,
            result: true));
        return true;
    }

    private HealthcareTrackerSettings? ValidateSettings()
    {
        if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.ProjectCliOption} option.");
            return null;
        }

        return new HealthcareTrackerSettings
        {
            Project = Project,
            Prerelease = Prerelease,
        };
    }

    private Task<HealthcareTrackerModel?> GetHealthcareTrackerModelAsync(
        ScaffolderContext context,
        HealthcareTrackerSettings settings)
    {
        ProjectInfo projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
        context.SetSpecifiedTargetFramework(projectInfo.LowestSupportedTargetFramework);
        if (projectInfo is null || projectInfo.CodeService is null)
        {
            return Task.FromResult<HealthcareTrackerModel?>(null);
        }

        bool hasMainLayout = false;
        var projectDirectory = Path.GetDirectoryName(settings.Project);
        if (!string.IsNullOrEmpty(projectDirectory) && _fileSystem.DirectoryExists(projectDirectory))
        {
            hasMainLayout = _fileSystem
                .EnumerateFiles(projectDirectory, "MainLayout.razor", SearchOption.AllDirectories)
                .Any();
        }

        var scaffoldingModel = new HealthcareTrackerModel
        {
            ProjectInfo = projectInfo,
            HasMainLayout = hasMainLayout,
        };

        if (scaffoldingModel.ProjectInfo.CodeService is not null)
        {
            scaffoldingModel.ProjectInfo.CodeChangeOptions = [];
        }

        return Task.FromResult<HealthcareTrackerModel?>(scaffoldingModel);
    }
}
