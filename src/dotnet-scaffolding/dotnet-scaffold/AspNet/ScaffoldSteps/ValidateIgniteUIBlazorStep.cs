// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
/// Scaffold step that validates the Ignite UI for Blazor options and initializes the
/// <see cref="IgniteUIBlazorModel"/> (resolved host page, _Imports.razor, packages and theme stylesheet).
/// </summary>
internal class ValidateIgniteUIBlazorStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Path to the project file.
    /// </summary>
    public string? Project { get; set; }
    /// <summary>
    /// Which Ignite UI package set to add: 'Lite', 'GridLite' or 'All'.
    /// </summary>
    public string? Package { get; set; }
    /// <summary>
    /// The theme to link ('bootstrap', 'material', 'fluent' or 'indigo'). Defaults to 'bootstrap'.
    /// </summary>
    public string? Theme { get; set; }
    /// <summary>
    /// The theme variant to link ('light' or 'dark'). Defaults to 'light'.
    /// </summary>
    public string? ThemeVariant { get; set; }
    /// <summary>
    /// Indicates if prerelease package versions should be installed.
    /// </summary>
    public bool Prerelease { get; set; }

    /// <summary>
    /// Constructor for ValidateIgniteUIBlazorStep.
    /// </summary>
    /// <param name="fileSystem">File system service.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="telemetryService">Telemetry service.</param>
    public ValidateIgniteUIBlazorStep(
        IFileSystem fileSystem,
        ILogger<ValidateIgniteUIBlazorStep> logger,
        ITelemetryService telemetryService)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Executes the step to validate the Ignite UI settings and initialize the <see cref="IgniteUIBlazorModel"/>.
    /// </summary>
    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var settings = ValidateSettings();
        if (settings is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIgniteUIBlazorStep), context.Scaffolder.DisplayName, false));
            return Task.FromResult(false);
        }

        context.Properties.Add(nameof(IgniteUIBlazorSettings), settings);

        _logger.LogInformation("Initializing Ignite UI for Blazor scaffolding model...");
        var model = GetModel(context, settings);
        if (model is null)
        {
            _logger.LogError("An error occurred while initializing the Ignite UI for Blazor scaffolding model.");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIgniteUIBlazorStep), context.Scaffolder.DisplayName, false));
            return Task.FromResult(false);
        }

        context.Properties.Add(nameof(IgniteUIBlazorModel), model);
        // No template variables are needed by the Ignite UI code modification configs, but the
        // code change steps expect the dictionary to be present (shared pattern across scaffolders).
        context.Properties.Add(Constants.StepConstants.CodeModifierProperties, new Dictionary<string, string>());

        LogRenderModeGuidance(model);
        _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIgniteUIBlazorStep), context.Scaffolder.DisplayName, true));
        return Task.FromResult(true);
    }

    /// <summary>
    /// Validates the options provided by the user.
    /// </summary>
    /// <returns>The validated settings, or null when validation failed.</returns>
    private IgniteUIBlazorSettings? ValidateSettings()
    {
        if (string.IsNullOrEmpty(Project) || !_fileSystem.FileExists(Project))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.ProjectCliOption} option.");
            return null;
        }

        if (!IgniteUIBlazorHelper.IsValidPackageOption(Package))
        {
            _logger.LogError($"Missing/Invalid {AspNetConstants.CliOptions.IgniteUIPackageOption} option. Expected one of: {string.Join(", ", IgniteUIBlazorHelper.PackageOptions)}.");
            return null;
        }

        var theme = IgniteUIBlazorHelper.NormalizeTheme(Theme);
        if (!string.IsNullOrEmpty(Theme) && !theme.Equals(Theme, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation($"Invalid {AspNetConstants.CliOptions.IgniteUIThemeOption} option '{Theme}'. Using default '{theme}'.");
        }

        var themeVariant = IgniteUIBlazorHelper.NormalizeThemeVariant(ThemeVariant);
        if (!string.IsNullOrEmpty(ThemeVariant) && !themeVariant.Equals(ThemeVariant, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation($"Invalid {AspNetConstants.CliOptions.IgniteUIThemeVariantOption} option '{ThemeVariant}'. Using default '{themeVariant}'.");
        }

        // '--project' may be relative to the current directory; every later step (host page, _Imports.razor,
        // code modification, package installation, WebAssembly client detection) expects a fully qualified path.
        return new IgniteUIBlazorSettings
        {
            Project = Path.GetFullPath(Project),
            Package = Package!,
            Theme = theme,
            ThemeVariant = themeVariant,
            Prerelease = Prerelease
        };
    }

    /// <summary>
    /// Initializes and returns the <see cref="IgniteUIBlazorModel"/> for scaffolding.
    /// </summary>
    private IgniteUIBlazorModel? GetModel(ScaffolderContext context, IgniteUIBlazorSettings settings)
    {
        ProjectInfo projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
        context.SetSpecifiedTargetFramework(projectInfo.LowestSupportedTargetFramework);
        var projectDirectory = Path.GetDirectoryName(projectInfo.ProjectPath);
        if (projectInfo.CodeService is null || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        var projectFileContent = _fileSystem.ReadAllText(settings.Project);
        var programFilePath = Path.Combine(projectDirectory, "Program.cs");
        var programFileContent = _fileSystem.FileExists(programFilePath) ? _fileSystem.ReadAllText(programFilePath) : null;

        bool includeLite = IgniteUIBlazorHelper.IncludesLite(settings.Package);
        bool includeGridLite = IgniteUIBlazorHelper.IncludesGridLite(settings.Package);
        // The GridLite stylesheet is only appropriate when GridLite is the only Ignite UI package in use.
        bool useLiteStylesheet = includeLite || IgniteUIBlazorHelper.ProjectReferencesLitePackage(projectFileContent);
        if (includeGridLite && !includeLite && useLiteStylesheet)
        {
            _logger.LogInformation("The project already references IgniteUI.Blazor.Lite; linking the IgniteUI.Blazor theme stylesheet instead of the GridLite-only stylesheet.");
        }

        var hostPagePath = IgniteUIBlazorHelper.FindHostPage(_fileSystem, projectDirectory);
        var stylesheetPath = IgniteUIBlazorHelper.GetThemeStylesheetPath(useLiteStylesheet, settings.Theme, settings.ThemeVariant);
        if (hostPagePath is null)
        {
            _logger.LogWarning($"Could not find a host page ({string.Join(", ", IgniteUIBlazorHelper.HostPageCandidates)}) in '{projectDirectory}'.");
            _logger.LogWarning("Add the following line to the <head> of your host page manually:");
            _logger.LogWarning($"    {IgniteUIBlazorHelper.BuildStylesheetLink(stylesheetPath, useAssetsCollection: false)}");
        }

        // No option-filtered blocks exist in the Ignite UI code modification configs.
        projectInfo.CodeChangeOptions = [];

        return new IgniteUIBlazorModel
        {
            ProjectInfo = projectInfo,
            ProjectPath = settings.Project,
            BaseOutputPath = projectDirectory,
            IncludeLite = includeLite,
            IncludeGridLite = includeGridLite,
            Theme = settings.Theme,
            ThemeVariant = settings.ThemeVariant,
            StylesheetPath = stylesheetPath,
            IsWebAssemblyProject = IgniteUIBlazorHelper.IsWebAssemblyProject(projectFileContent, programFileContent),
            HostPagePath = hostPagePath,
            ImportsFilePath = IgniteUIBlazorHelper.GetImportsFilePath(_fileSystem, projectDirectory)
        };
    }

    /// <summary>
    /// Ignite UI components need an interactive render mode; static server-side rendering renders nothing usable.
    /// Logs guidance for Blazor Web Apps that do not register or declare an interactive render mode.
    /// Blazor WebAssembly and Blazor Server applications are always interactive, so nothing is logged for them.
    /// </summary>
    private void LogRenderModeGuidance(IgniteUIBlazorModel model)
    {
        if (model.IsWebAssemblyProject ||
            model.HostPagePath is null ||
            !model.HostPagePath.EndsWith("App.razor", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var programFilePath = Path.Combine(model.BaseOutputPath, "Program.cs");
        var programFileContent = _fileSystem.FileExists(programFilePath) ? _fileSystem.ReadAllText(programFilePath) : null;
        if (!IgniteUIBlazorHelper.HasInteractiveRenderModeServices(programFileContent))
        {
            _logger.LogWarning("Ignite UI components require an interactive render mode, but this Blazor Web App does not register interactive components.");
            _logger.LogWarning("Chain '.AddInteractiveServerComponents()' and/or '.AddInteractiveWebAssemblyComponents()' after 'builder.Services.AddRazorComponents()' in Program.cs, then set '@rendermode InteractiveServer' (or InteractiveWebAssembly / InteractiveAuto) on the components that use Ignite UI.");
            return;
        }

        var appRazorContent = _fileSystem.ReadAllText(model.HostPagePath);
        var routesRazorPath = Path.Combine(Path.GetDirectoryName(model.HostPagePath) ?? model.BaseOutputPath, "Routes.razor");
        var routesRazorContent = _fileSystem.FileExists(routesRazorPath) ? _fileSystem.ReadAllText(routesRazorPath) : null;
        if (!IgniteUIBlazorHelper.DeclaresRenderMode(appRazorContent) && !IgniteUIBlazorHelper.DeclaresRenderMode(routesRazorContent))
        {
            _logger.LogInformation("No global render mode is set on <Routes /> in App.razor. Ignite UI components need an interactive render mode: add '@rendermode InteractiveServer' (or InteractiveWebAssembly / InteractiveAuto) to the pages that use them, or set '<Routes @rendermode=\"InteractiveAuto\" />' globally.");
        }
    }
}
