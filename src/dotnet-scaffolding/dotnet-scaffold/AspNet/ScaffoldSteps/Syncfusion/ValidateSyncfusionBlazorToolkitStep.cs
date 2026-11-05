// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;

/// <summary>
/// First step of the Syncfusion Blazor Toolkit scaffolder. It normalizes the <c>--project</c>
/// path, classifies the project once from evaluated MSBuild state (a Blazor Web App, a standalone
/// Blazor WebAssembly app, or unsupported such as a Web API or MAUI Hybrid app), resolves a
/// referenced WebAssembly client, and records the starting state (package version, existing
/// registration, hosting support) on <see cref="SyncfusionBlazorToolkitSettings"/> for the steps
/// that follow.
/// </summary>
internal class ValidateSyncfusionBlazorToolkitStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Path to the project file (relative or absolute).
    /// </summary>
    public string? Project { get; set; }

    /// <summary>Whether prerelease Toolkit packages may be installed (<c>--prerelease</c>).</summary>
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

        // We need a real directory and a real file.
        string projectDirectory = Path.GetDirectoryName(settings.Project)!;
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

        // This step evaluates projects with MSBuild before any Roslyn workspace exists, so it must
        // register MSBuild through the shared policy first (as ClassAnalyzers.GetProjectInfo does for
        // the other validate steps). Registration fails once Microsoft.Build is already loaded.
        new MsBuildInitializer(_logger).Initialize();

        // Classify the project once, from evaluated MSBuild state. Every later step
        // reads the result from the settings instead of re-reading the project file.
        BlazorProjectInfo projectInfo = BlazorProjectClassifier.Classify(settings.Project);
        settings.AppKind = projectInfo.Kind;

        if (projectInfo.Error is not null)
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit scaffolder could not determine the project type of '{Project}': {Error}",
                settings.Project,
                projectInfo.Error);
            return Fail(context);
        }

        if (projectInfo.Kind == BlazorAppKind.Hybrid)
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit scaffolder does not support Hybrid / MAUI Blazor Hybrid projects. " +
                "Run the scaffolder against the Blazor WebAssembly or Blazor Web App project inside the solution, " +
                "not on a MAUI host project ('{Project}').",
                settings.Project);
            return Fail(context);
        }

        if (projectInfo.Kind == BlazorAppKind.NotBlazor)
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit scaffolder requires a Blazor Web App, Blazor Server, or Blazor WebAssembly project. " +
                "'{Project}' is not one: a Blazor Web App is an executable Microsoft.NET.Sdk.Web project that contains Razor " +
                "components, and a standalone app uses Microsoft.NET.Sdk.BlazorWebAssembly. Add Blazor support or pick a different project.",
                settings.Project);
            return Fail(context);
        }

        settings.Layout = SyncfusionBlazorToolkitLayout.SingleProject;

        // Resolve a referenced WebAssembly client (if any). This is the
        // single source of truth for multi-project layouts: when a
        // Blazor Web App Auto server project references a
        // Microsoft.NET.Sdk.BlazorWebAssembly project, we configure the
        // client too — package on the client, services + imports on the
        // server.
        //
        // `BlazorWebAssemblyClientProjectResolver.TryGetClient` returns
        //   true  + null client → no client was found, single-project.
        //   true  + client      → exactly one client, use it.
        //   false + error       → a referenced project is missing,
        //                         evaluation failed, or multiple
        //                         clients were found. In all of those
        //                         cases the user must fix their project
        //                         references before the scaffolder can
        //                         safely proceed, regardless of whether
        //                         `*.Client.csproj` happens to live next
        //                         to the server. Silent single-project
        //                         fallback would leave the client
        //                         unconfigured.
        string? clientError = null;
        bool clientLookupOk = false;
        if (projectInfo.Kind == BlazorAppKind.WebApp)
        {
            clientLookupOk = BlazorWebAssemblyClientProjectResolver.TryGetClient(
                settings.Project, _fileSystem, out var client, out clientError);
            if (clientLookupOk && client is not null)
            {
                settings.ClientProjectPath = client.Value.ProjectPath;
                settings.Layout = SyncfusionBlazorToolkitLayout.AutoServer;
            }
            else if (!clientLookupOk)
            {
                // Hard failure: missing referenced project, evaluation
                // error, or ambiguous clients. We do not pre-filter on the
                // raw .csproj text: references can come from imported
                // .props/.targets, and the resolver evaluates MSBuild.
                _logger.LogError(
                    "Syncfusion Blazor Toolkit scaffolder could not resolve the Blazor WebAssembly client for '{Project}': {Error}. " +
                    "Fix the server project's ProjectReference entries (or remove the WebAssembly client reference for a single-project setup), then re-run the scaffolder.",
                    settings.Project,
                    clientError);
                _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
                    nameof(ValidateSyncfusionBlazorToolkitStep),
                    context.Scaffolder.DisplayName,
                    result: false));
                return Task.FromResult(false);
            }
        }

        // Hosting support the entry point has actually registered, kept separate from
        // the application kind and from the global page render mode.
        settings.Hosting = InspectHosting(settings.Project, projectDirectory);

        // Baselines for the summary: what each project had BEFORE this run.
        settings.Server.PackageVersionBefore = ReadPackageVersion(settings.Project);
        if (!string.IsNullOrEmpty(settings.ClientProjectPath))
        {
            settings.Client.PackageVersionBefore = ReadPackageVersion(settings.ClientProjectPath);
        }

        settings.ServicesAlreadyRegistered = IsServicesAlreadyRegistered(settings.Project);

        // Pre-discover the imports file so the dedicated imports step
        // can use the same convention. The dedicated step may still
        // create a file if discovery fails.
        string? importsPath = SyncfusionBlazorToolkitHelper.DiscoverImportsFile(projectDirectory);
        if (!string.IsNullOrEmpty(importsPath))
        {
            settings.ImportsFile = Path.GetRelativePath(projectDirectory, importsPath);
        }
        else
        {
            settings.ImportsFile = null;
            settings.ImportsFileSkipped = true;
            _logger.LogWarning(
                "Syncfusion Blazor Toolkit could not find any '_Imports.razor' under '{ProjectDirectory}'. " +
                "The dedicated imports step will create one in the conventional location.",
                projectDirectory);
        }

        context.Properties.Add(nameof(SyncfusionBlazorToolkitSettings), settings);
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

        // Normalize the supplied --project path to a full path so
        // downstream steps always see an absolute path that survives
        // Path.GetDirectoryName. A relative "App.csproj" otherwise
        // passes File.Exists but Path.GetDirectoryName returns an
        // empty string and the scaffolder cannot find Program.cs.
        string normalizedProject;
        try
        {
            normalizedProject = Path.GetFullPath(Project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Syncfusion Blazor Toolkit could not normalize the supplied --project path '{Project}'.",
                Project);
            return null;
        }

        return new SyncfusionBlazorToolkitSettings
        {
            Project = normalizedProject,
            Prerelease = Prerelease,
        };
    }

    private BlazorHostingFacts InspectHosting(string projectPath, string projectDirectory)
    {
        try
        {
            string? entryPoint = SyncfusionBlazorToolkitHelper.DiscoverEntryPoint(projectPath, out _);
            string? entryPointText = entryPoint is null ? null : File.ReadAllText(entryPoint);

            string appRazor = Path.Combine(projectDirectory, "Components", "App.razor");
            string? appRazorText = File.Exists(appRazor) ? File.ReadAllText(appRazor) : null;

            return BlazorHostingInspector.Inspect(entryPointText, appRazorText);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex,
                "Syncfusion Blazor Toolkit could not read the entry point or App.razor to inspect hosting support. " +
                "Hosting guidance will be omitted.");
            return BlazorHostingFacts.Unknown;
        }
    }

    /// <summary>
    /// The evaluated Toolkit package version of a project: <c>null</c> when it is
    /// not referenced (or cannot be evaluated - the verify step reports that).
    /// </summary>
    private string? ReadPackageVersion(string projectPath)
    {
        if (!SyncfusionBlazorToolkitHelper.TryGetReferencedPackageVersion(projectPath, out string? version, out string? error))
        {
            _logger.LogWarning(
                "Syncfusion Blazor Toolkit could not evaluate the package references of '{Project}': {Error}",
                projectPath,
                error);
            return null;
        }

        return version;
    }

    private Task<bool> Fail(ScaffolderContext context)
    {
        _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
            nameof(ValidateSyncfusionBlazorToolkitStep),
            context.Scaffolder.DisplayName,
            result: false));
        return Task.FromResult(false);
    }

    /// <summary>
    /// Inspects the evaluated application entry point for an existing
    /// <c>AddSyncfusionBlazorToolkit</c> service registration. Returns
    /// false when no entry point is found or it cannot be read.
    /// </summary>
    private bool IsServicesAlreadyRegistered(string projectPath)
    {
        try
        {
            string? programPath = SyncfusionBlazorToolkitHelper.DiscoverEntryPoint(projectPath, out _);
            if (string.IsNullOrEmpty(programPath))
            {
                return false;
            }

            return SyncfusionBlazorToolkitHelper.IsActiveServiceRegistrationPresent(File.ReadAllText(programPath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Syncfusion Blazor Toolkit could not inspect the entry point for an existing service registration. " +
                "Idempotency detection will report services as 'not yet registered'.");
            return false;
        }
    }
}