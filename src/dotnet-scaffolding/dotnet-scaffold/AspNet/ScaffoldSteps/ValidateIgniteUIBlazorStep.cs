// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal;
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
/// <see cref="IgniteUIBlazorModel"/> (resolved host page, _Imports.razor, theme stylesheet and,
/// for a Blazor Web App, the referenced Blazor WebAssembly client project).
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
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var settings = ValidateSettings();
        if (settings is null)
        {
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIgniteUIBlazorStep), context.Scaffolder.DisplayName, false));
            return false;
        }

        context.Properties.Add(nameof(IgniteUIBlazorSettings), settings);

        _logger.LogInformation("Initializing Ignite UI for Blazor scaffolding model...");
        var model = await GetModelAsync(context, settings);
        if (model is null)
        {
            _logger.LogError("An error occurred while initializing the Ignite UI for Blazor scaffolding model.");
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIgniteUIBlazorStep), context.Scaffolder.DisplayName, false));
            return false;
        }

        context.Properties.Add(nameof(IgniteUIBlazorModel), model);
        // No template variables are needed by the Ignite UI code modification configs, but the
        // code change steps expect the dictionary to be present (shared pattern across scaffolders).
        context.Properties.Add(Constants.StepConstants.CodeModifierProperties, new Dictionary<string, string>());

        _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(nameof(ValidateIgniteUIBlazorStep), context.Scaffolder.DisplayName, true));
        return true;
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

        // Omitted options fall back to the defaults; a value that is given must be one of the supported values.
        if (!IgniteUIBlazorHelper.TryNormalizeTheme(Theme, out var theme))
        {
            _logger.LogError($"Invalid {AspNetConstants.CliOptions.IgniteUIThemeOption} option '{Theme}'. Supported values: {string.Join(", ", IgniteUIBlazorHelper.Themes)} (default: {IgniteUIBlazorHelper.DefaultTheme}).");
            return null;
        }

        if (!IgniteUIBlazorHelper.TryNormalizeThemeVariant(ThemeVariant, out var themeVariant))
        {
            _logger.LogError($"Invalid {AspNetConstants.CliOptions.IgniteUIThemeVariantOption} option '{ThemeVariant}'. Supported values: {string.Join(", ", IgniteUIBlazorHelper.ThemeVariants)} (default: {IgniteUIBlazorHelper.DefaultThemeVariant}).");
            return null;
        }

        // '--project' may be relative to the current directory; every later step (host page, _Imports.razor,
        // code modification, package installation, WebAssembly client detection) expects a fully qualified path.
        return new IgniteUIBlazorSettings
        {
            Project = Path.GetFullPath(Project),
            Theme = theme,
            ThemeVariant = themeVariant,
            Prerelease = Prerelease
        };
    }

    /// <summary>
    /// Initializes and returns the <see cref="IgniteUIBlazorModel"/> for scaffolding.
    /// </summary>
    private async Task<IgniteUIBlazorModel?> GetModelAsync(ScaffolderContext context, IgniteUIBlazorSettings settings)
    {
        ProjectInfo projectInfo = ClassAnalyzers.GetProjectInfo(settings.Project, _logger);
        context.SetSpecifiedTargetFramework(projectInfo.LowestSupportedTargetFramework);
        var projectDirectory = Path.GetDirectoryName(projectInfo.ProjectPath);
        if (projectInfo.CodeService is null || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        // Everything below inspects evaluated MSBuild projects, before any changes are made, so that an unsupported
        // project, a broken client reference or a package conflict stops the scaffolder without partial edits.
        // ClassAnalyzers.GetProjectInfo above has registered MSBuild.
        if (!IgniteUIBlazorProjectInspector.TryEvaluate(settings.Project, out var projectEvaluation, out var evaluationError))
        {
            _logger.LogError(evaluationError);
            return null;
        }

        bool isWebAssemblyProject = projectEvaluation!.UsesBlazorWebAssemblySdk;
        bool isMauiBlazorHybridProject = !isWebAssemblyProject && projectEvaluation.IsMauiBlazorHybrid;
        if (!isWebAssemblyProject && !isMauiBlazorHybridProject && !projectEvaluation.UsesWebSdk)
        {
            _logger.LogError(IgniteUIBlazorProjectInspector.GetUnsupportedProjectError(projectEvaluation, hasWebAssemblyClient: false));
            return null;
        }

        // A Blazor Web App server project may reference a WebAssembly client project that needs the same packages,
        // services and imports. Resolve it so that a broken reference, an evaluation failure or an ambiguous client
        // stops the scaffolder instead of leaving the client silently unconfigured.
        // A standalone WebAssembly project is the client itself, and a MAUI Blazor Hybrid app runs its components in
        // the app's own BlazorWebView, so neither has a client project to discover.
        string? clientProjectPath = null;
        string? clientImportsFilePath = null;
        IgniteUIBlazorProjectInspector.ProjectEvaluation? clientEvaluation = null;
        if (!isWebAssemblyProject && !isMauiBlazorHybridProject)
        {
            if (!BlazorWebAssemblyClientProjectResolver.TryGetClient(settings.Project, _fileSystem, out var client, out var error))
            {
                _logger.LogError(error);
                return null;
            }

            if (client is not null)
            {
                // The resolver returns fully qualified project paths, so the directory is always available.
                clientProjectPath = client.Value.ProjectPath;
                clientImportsFilePath = IgniteUIBlazorHelper.GetClientImportsFilePath(_fileSystem, Path.GetDirectoryName(clientProjectPath)!);
                _logger.LogInformation($"Found Blazor WebAssembly client project '{clientProjectPath}'; it will be configured as well.");
                if (!IgniteUIBlazorProjectInspector.TryEvaluate(clientProjectPath, out clientEvaluation, out evaluationError))
                {
                    _logger.LogError(evaluationError);
                    return null;
                }
            }
        }

        var unsupportedProjectError = IgniteUIBlazorProjectInspector.GetUnsupportedProjectError(projectEvaluation, hasWebAssemblyClient: clientEvaluation is not null);
        if (unsupportedProjectError is not null)
        {
            _logger.LogError(unsupportedProjectError);
            return null;
        }

        // IgniteUI.Blazor.Lite is installed in the server and in the client, so both must be free of the commercial package.
        var packageConflictErrors = new[] { projectEvaluation, clientEvaluation }
            .OfType<IgniteUIBlazorProjectInspector.ProjectEvaluation>()
            .Select(IgniteUIBlazorProjectInspector.GetCommercialPackageConflictError)
            .OfType<string>()
            .ToList();
        if (packageConflictErrors.Count > 0)
        {
            packageConflictErrors.ForEach(error => _logger.LogError(error));
            return null;
        }

        // Ignite UI components need interactivity. A Blazor Web App keeps its Server, WebAssembly or Auto configuration;
        // one without any gets Interactive Server support. Blazor Server and standalone WebAssembly apps are always
        // interactive, and a MAUI Blazor Hybrid app has no Program.cs.
        BlazorInteractivityAnalyzer.BlazorInteractivity? interactivity = null;
        bool addInteractiveServerSupport = false;
        if (!isWebAssemblyProject && !isMauiBlazorHybridProject &&
            await projectInfo.CodeService.GetDocumentAsync("Program.cs") is { } programDocument)
        {
            var programPath = programDocument.FilePath ?? Path.Combine(projectDirectory, "Program.cs");
            // The rest of the setup does not require a prior restore, so neither does this analysis.
            (interactivity, var interactivityError) = await BlazorInteractivityAnalyzer.AnalyzeAsync(programDocument, programPath, settings.Project, allowSyntaxFallback: true);
            if (interactivity is null)
            {
                _logger.LogError(interactivityError);
                return null;
            }

            addInteractiveServerSupport = interactivity.UsesRazorComponents && !interactivity.UsesInteractiveServer && !interactivity.UsesInteractiveWebAssembly;
            if (addInteractiveServerSupport && !interactivity.MapsRazorComponents)
            {
                _logger.LogError(
                    $"Ignite UI components require an interactive render mode, but '{programPath}' configures none and does not map its components with 'app.MapRazorComponents<App>()', so Interactive Server support cannot be added. " +
                    "Chain '.AddInteractiveServerComponents()' after 'builder.Services.AddRazorComponents()' and '.AddInteractiveServerRenderMode()' after 'app.MapRazorComponents<App>()', then re-run the scaffolder.");
                return null;
            }

            if (addInteractiveServerSupport)
            {
                _logger.LogInformation("This Blazor Web App has no interactive render mode configured; Interactive Server support will be added to Program.cs. Existing pages keep their render modes.");
            }
        }

        var hostPagePath = IgniteUIBlazorHelper.FindHostPage(_fileSystem, projectDirectory);
        var stylesheetPath = IgniteUIBlazorHelper.GetThemeStylesheetPath(settings.Theme, settings.ThemeVariant);
        if (hostPagePath is null)
        {
            // The remaining setup still runs; the theme step then fails and reports the <link> to add manually.
            _logger.LogWarning($"Could not find a host page ({string.Join(", ", IgniteUIBlazorHelper.HostPageCandidates)}) in '{projectDirectory}'. The theme stylesheet cannot be linked automatically.");
        }

        // The service registration is required, so its code modification configs must be available before anything changes.
        var codeModificationConfigPath = FindCodeModificationConfig(
            settings.Project,
            IgniteUIBlazorScaffolderBuilderExtensions.GetCodeModificationConfigFileName(isWebAssemblyProject, isMauiBlazorHybridProject));
        string? clientCodeModificationConfigPath = null;
        if (codeModificationConfigPath is null ||
            (clientProjectPath is not null &&
             (clientCodeModificationConfigPath = FindCodeModificationConfig(clientProjectPath, IgniteUIBlazorScaffolderBuilderExtensions.WasmCodeModificationConfigFileName)) is null))
        {
            return null;
        }

        // No option-filtered blocks exist in the Ignite UI code modification configs.
        projectInfo.CodeChangeOptions = [];

        return new IgniteUIBlazorModel
        {
            ProjectInfo = projectInfo,
            ProjectPath = settings.Project,
            BaseOutputPath = projectDirectory,
            Theme = settings.Theme,
            ThemeVariant = settings.ThemeVariant,
            StylesheetPath = stylesheetPath,
            IsWebAssemblyProject = isWebAssemblyProject,
            IsMauiBlazorHybridProject = isMauiBlazorHybridProject,
            HostPagePath = hostPagePath,
            ImportsFilePath = IgniteUIBlazorHelper.GetImportsFilePath(_fileSystem, projectDirectory),
            ClientProjectPath = clientProjectPath,
            ClientImportsFilePath = clientImportsFilePath,
            CodeModificationConfigPath = codeModificationConfigPath,
            ClientCodeModificationConfigPath = clientCodeModificationConfigPath,
            AddInteractiveServerSupport = addInteractiveServerSupport,
            RenderModeGuidance = interactivity is { UsesRazorComponents: true }
                ? IgniteUIBlazorHelper.GetRenderModeGuidance(
                    interactivity.UsesInteractiveServer || addInteractiveServerSupport,
                    interactivity.UsesInteractiveWebAssembly,
                    addInteractiveServerSupport,
                    HasGlobalRenderMode(hostPagePath))
                : null
        };
    }

    /// <summary>
    /// Finds the code modification config that registers the Ignite UI services in <paramref name="projectPath"/>,
    /// for the project's target framework. Logs an error when the config is missing from the tool installation.
    /// </summary>
    private string? FindCodeModificationConfig(string projectPath, string configFileName)
    {
        var targetFrameworkFolder = TargetFrameworkHelpers.GetTargetFrameworkFolder(projectPath);
        var configPath = GlobalToolFileFinder.FindCodeModificationConfigFile(configFileName, typeof(ValidateIgniteUIBlazorStep).Assembly, targetFrameworkFolder);
        if (string.IsNullOrEmpty(configPath))
        {
            _logger.LogError(
                $"Unable to find the code modification configuration '{configFileName}' for {targetFrameworkFolder}, which registers the Ignite UI for Blazor services in '{projectPath}'. " +
                "The dotnet-scaffold installation may be incomplete; reinstall the tool and re-run the scaffolder.");
            return null;
        }

        return configPath;
    }

    /// <summary>
    /// Returns true when the App.razor host page, or the Routes.razor next to it, declares a render mode, so that the
    /// app's pages are interactive globally.
    /// </summary>
    private bool HasGlobalRenderMode(string? hostPagePath)
    {
        if (hostPagePath is null || !hostPagePath.EndsWith("App.razor", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var routesRazorPath = Path.Combine(Path.GetDirectoryName(hostPagePath) ?? string.Empty, "Routes.razor");
        return IgniteUIBlazorHelper.DeclaresRenderMode(_fileSystem.ReadAllText(hostPagePath)) ||
               (_fileSystem.FileExists(routesRazorPath) && IgniteUIBlazorHelper.DeclaresRenderMode(_fileSystem.ReadAllText(routesRazorPath)));
    }
}
