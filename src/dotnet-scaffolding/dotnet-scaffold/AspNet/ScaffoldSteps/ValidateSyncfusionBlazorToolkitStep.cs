// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Validates Syncfusion Blazor Toolkit settings and seeds the project context for the
/// setup scaffolder.
///
/// The setup scaffolder only requires:
///   - A valid project file path.
///   - A Components/_Imports.razor file (used as a safe anchor for inserting using
///     directives via the code-modification step).
///
/// The theme stylesheet host (Components/App.razor for Blazor Web App, or
/// wwwroot/index.html for standalone Blazor WASM) is NOT required for
/// validation to succeed. If neither is present the theme step is skipped
/// and the rest of the setup (package, Program.cs registration,
/// Components/_Imports.razor) still applies.
///
/// It does not require a MainLayout, App.razor, or NavMenu.razor: the
/// code-modification step targets Program.cs, Components/_Imports.razor,
/// and (when present) the resolved theme host file.
/// </summary>
internal class ValidateSyncfusionBlazorToolkitStep : ScaffoldStep
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

    public ValidateSyncfusionBlazorToolkitStep(
        IFileSystem fileSystem,
        ILogger<ValidateSyncfusionBlazorToolkitStep> logger,
        ITelemetryService telemetryService)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var settings = ValidateSettings();
        if (settings is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
                nameof(ValidateSyncfusionBlazorToolkitStep),
                context.Scaffolder.DisplayName,
                result: false));
            return Task.FromResult(false);
        }

        // Require Components/_Imports.razor so the code-mod has a safe anchor for adding
        // the @using directive. Program.cs and Components/App.razor are also modified, but
        // those are common to all ASP.NET Core projects.
        var projectDirectory = Path.GetDirectoryName(settings.Project);
        if (string.IsNullOrEmpty(projectDirectory) || !_fileSystem.DirectoryExists(projectDirectory))
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit scaffolder could not find the project directory '{ProjectDirectory}'.",
                projectDirectory);
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
                nameof(ValidateSyncfusionBlazorToolkitStep),
                context.Scaffolder.DisplayName,
                result: false));
            return Task.FromResult(false);
        }

        var importsPath = Path.Combine(projectDirectory, "Components", "_Imports.razor");
        if (!_fileSystem.FileExists(importsPath))
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit scaffolder requires 'Components/_Imports.razor' to exist in the target Blazor project.");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
                nameof(ValidateSyncfusionBlazorToolkitStep),
                context.Scaffolder.DisplayName,
                result: false));
            return Task.FromResult(false);
        }

        context.Properties.Add(nameof(SyncfusionBlazorToolkitSettings), settings);
        context.Properties.Add(Constants.StepConstants.CodeModifierProperties, new Dictionary<string, string>());
        context.Properties.Add(Constants.StepConstants.BaseProjectPath, projectDirectory);

        _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
            nameof(ValidateSyncfusionBlazorToolkitStep),
            context.Scaffolder.DisplayName,
            result: true));
        return Task.FromResult(true);
    }

    private SyncfusionBlazorToolkitSettings? ValidateSettings()
    {
        if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.ProjectCliOption} option.");
            return null;
        }

        return new SyncfusionBlazorToolkitSettings
        {
            Project = Project,
            Prerelease = Prerelease,
        };
    }
}
