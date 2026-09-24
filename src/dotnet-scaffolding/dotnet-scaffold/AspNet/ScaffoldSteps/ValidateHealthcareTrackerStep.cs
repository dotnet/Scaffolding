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
///
/// Healthcare Tracker requires a default Blazor Web App template layout:
///   - Components/_Imports.razor
///   - Components/App.razor
///   - Components/Layout/NavMenu.razor (preferred; non-preferred locations are accepted with a warning)
///
/// Validation fails fast (LogError + return false) when any of these are missing so
/// the user gets a clear, actionable error instead of a partially-scaffolded project.
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

        // Fail fast: require the default Blazor Web App template layout under the project directory.
        if (!ValidateBlazorWebAppLayout(settings.Project, out string? layoutFailure))
        {
            _logger.LogError(layoutFailure);
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

    /// <summary>
    /// Verifies that the project contains the default Blazor Web App template layout
    /// required by the Healthcare Tracker scaffolder.
    /// </summary>
    /// <param name="projectPath">Absolute path to the project file.</param>
    /// <param name="failureMessage">Populated with a clear error when validation fails; otherwise null.</param>
    /// <returns>True when all required files are present; otherwise false.</returns>
    private bool ValidateBlazorWebAppLayout(string? projectPath, out string? failureMessage)
    {
        failureMessage = null;

        if (string.IsNullOrEmpty(projectPath))
        {
            failureMessage = "Healthcare Tracker requires a default Blazor Web App template layout, but no project path was provided.";
            return false;
        }

        var projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDirectory) || !_fileSystem.DirectoryExists(projectDirectory))
        {
            failureMessage = $"Healthcare Tracker requires a default Blazor Web App template layout, but the project directory '{projectDirectory}' was not found.";
            return false;
        }

        // Required: Components/_Imports.razor
        var importsPath = Path.Combine(projectDirectory, "Components", "_Imports.razor");
        if (!_fileSystem.FileExists(importsPath))
        {
            failureMessage = "Healthcare Tracker requires a default Blazor Web App template layout. Missing required file: 'Components/_Imports.razor'.";
            return false;
        }

        // Required: Components/App.razor
        var appPath = Path.Combine(projectDirectory, "Components", "App.razor");
        if (!_fileSystem.FileExists(appPath))
        {
            failureMessage = "Healthcare Tracker requires a default Blazor Web App template layout. Missing required file: 'Components/App.razor'.";
            return false;
        }

        // Required: a NavMenu.razor (preferred path is Components/Layout/NavMenu.razor).
        var preferredNavMenuPath = Path.Combine(projectDirectory, "Components", "Layout", "NavMenu.razor");
        if (_fileSystem.FileExists(preferredNavMenuPath))
        {
            return true;
        }

        // Fallback: search the project tree for any NavMenu.razor so the code-mod can still locate it.
        var anyNavMenu = _fileSystem
            .EnumerateFiles(projectDirectory, "NavMenu.razor", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (anyNavMenu is null)
        {
            failureMessage = "Healthcare Tracker requires a default Blazor Web App template layout. Missing required file: 'Components/Layout/NavMenu.razor' (no NavMenu.razor found anywhere under the project).";
            return false;
        }

        // Non-preferred path found: warn so the user knows the code-mod's expected path
        // (Components\Layout\NavMenu.razor) may not match and a manual fix-up may be required.
        _logger.LogWarning(
            "Found 'NavMenu.razor' at a non-preferred location: '{ActualPath}'. The Healthcare Tracker code-mod targets 'Components/Layout/NavMenu.razor' and may not apply automatically; you may need to add the Healthcare Tracker nav entry manually.",
            anyNavMenu);

        return true;
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
