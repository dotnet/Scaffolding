// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
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
/// Validates Syncfusion Blazor Toolkit settings and seeds the project context
/// for the setup scaffolder.
///
/// <para>Setup is intentionally minimal: only a supported Blazor
/// application is accepted. The validation step normalizes the supplied
/// <c>--project</c> path, rejects Hybrid / MAUI projects explicitly
/// (because <c>MauiProgram.cs</c> is not a supported host), and detects
/// whether the project is the server or client side of a Blazor Web App
/// Auto split using the existing
/// <see cref="BlazorWebAssemblyClientProjectResolver"/>.</para>
///
/// <para>Detected values are surfaced on the resulting
/// <see cref="SyncfusionBlazorToolkitSettings"/> instance so that
/// downstream steps can pick the right code-modification recipe and the
/// summary step can give accurate, hosting-specific guidance.</para>
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

        // Validate that the project is a supported Blazor application —
        // not merely a .csproj that exists. Hybrid / MAUI Blazor Hybrid
        // projects are explicitly rejected because they target
        // MauiProgram.cs which the shared code-modification pipeline
        // does not (and cannot, with the existing recipes) modify.
        var projectKind = DetectProjectKind(settings.Project);
        if (projectKind == ProjectKind.UnsupportedHybrid)
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit scaffolder does not support Hybrid / MAUI Blazor Hybrid projects. " +
                "Run the scaffolder against the Blazor WebAssembly or Blazor Web App project inside the solution, " +
                "not on a MAUI host project ('{Project}').",
                settings.Project);
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
                nameof(ValidateSyncfusionBlazorToolkitStep),
                context.Scaffolder.DisplayName,
                result: false));
            return Task.FromResult(false);
        }

        if (projectKind == ProjectKind.NotBlazor)
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit scaffolder requires a Blazor Web App, Blazor Server, or Blazor WebAssembly project. " +
                "'{Project}' does not target the Microsoft.NET.Sdk.Web, Microsoft.NET.Sdk.BlazorWebAssembly, or " +
                "Microsoft.NET.Sdk.Blazor (with Server-side rendering) SDK. Add Blazor support or pick a different project.",
                settings.Project);
            _telemetryService.TrackEvent(new ValidateScaffolderTelemetryEvent(
                nameof(ValidateSyncfusionBlazorToolkitStep),
                context.Scaffolder.DisplayName,
                result: false));
            return Task.FromResult(false);
        }

        settings.Layout = projectKind switch
        {
            ProjectKind.StandaloneWasm => SyncfusionBlazorToolkitLayout.SingleProject,
            _ => SyncfusionBlazorToolkitLayout.SingleProject,
        };

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
        if (projectKind == ProjectKind.WebApp)
        {
            clientLookupOk = BlazorWebAssemblyClientProjectResolver.TryGetClient(
                settings.Project, _fileSystem, out var client, out clientError);
            if (clientLookupOk && client is not null)
            {
                settings.ClientProjectPath = client.Value.ProjectPath;
                settings.Layout = SyncfusionBlazorToolkitLayout.AutoServer;
            }
            else if (!clientLookupOk && ProjectReferencesWebAssemblyClient(settings.Project))
            {
                // Hard failure: missing referenced project, evaluation
                // error, or ambiguous clients. We deliberately do not
                // gate this on `LooksLikeMultiProjectReference()` (the
                // old sibling-only check) — the standard Blazor Web App
                // Auto template places the client in a sibling folder,
                // not next to the server. We DO gate on
                // `ProjectReferencesWebAssemblyClient` so that a
                // resolver failure on a genuinely single-project .csproj
                // is not surfaced as a hard error.
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

        // Detect hosting configuration (render mode, hosting support) so
        // the summary can give accurate next-steps guidance instead of
        // unconditionally listing every interactive render mode.
        settings.DetectedRenderMode = DetectRenderMode(projectDirectory);

        // Pre-scaffolding flags for the summary.
        settings.PackageAlreadyReferenced = IsPackageAlreadyReferenced(settings.Project);
        settings.ServicesAlreadyRegistered = IsServicesAlreadyRegistered(projectDirectory);

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

    /// <summary>
    /// True when the supplied .csproj file declares any
    /// <c>&lt;ProjectReference&gt;</c> entries. This is a fast,
    /// evaluator-free check (no MSBuild load) that lets the validate
    /// step decide whether a missing/ambiguous Blazor WebAssembly
    /// client is a real failure (the project actually tries to
    /// reference one but evaluation cannot find/match it) or just a
    /// harmless artifact of evaluating a single-project test fixture
    /// that the resolver cannot fully load.
    /// </summary>
    private static bool ProjectReferencesWebAssemblyClient(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            return false;
        }

        try
        {
            string text = File.ReadAllText(projectPath);
            return text.Contains("<ProjectReference", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Detects whether the supplied project is a Blazor Web App,
    /// standalone Blazor WebAssembly, Blazor Server (Microsoft.NET.Sdk.Blazor),
    /// Hybrid / MAUI Blazor Hybrid, or not a Blazor project at all.
    /// </summary>
    private ProjectKind DetectProjectKind(string projectPath)
    {
        string text;
        try
        {
            text = File.ReadAllText(projectPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Syncfusion Blazor Toolkit could not read the project file '{Project}' to detect its SDK.",
                projectPath);
            return ProjectKind.Unknown;
        }

        // MAUI / Hybrid has the most specific SDK and wins first.
        if (Regex.IsMatch(text, "Microsoft\\.NET\\.Sdk\\.Maui", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(text, "<UseMaui\\b", RegexOptions.IgnoreCase))
        {
            return ProjectKind.UnsupportedHybrid;
        }

        if (Regex.IsMatch(text, "Microsoft\\.NET\\.Sdk\\.BlazorWebAssembly", RegexOptions.IgnoreCase))
        {
            return ProjectKind.StandaloneWasm;
        }

        if (Regex.IsMatch(text, "Microsoft\\.NET\\.Sdk\\.Web\\b", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(text, "Microsoft\\.NET\\.Sdk\\.Blazor\\b", RegexOptions.IgnoreCase))
        {
            return ProjectKind.WebApp;
        }

        return ProjectKind.NotBlazor;
    }

    private string DetectRenderMode(string projectDirectory)
    {
        try
        {
            string appRazorPath = Path.Combine(projectDirectory, "Components", "App.razor");
            if (File.Exists(appRazorPath))
            {
                string appText = File.ReadAllText(appRazorPath);
                if (appText.Contains("@rendermode=\"InteractiveAuto\"", StringComparison.OrdinalIgnoreCase))
                {
                    return "InteractiveAuto";
                }

                if (appText.Contains("@rendermode=\"InteractiveServer\"", StringComparison.OrdinalIgnoreCase))
                {
                    return "InteractiveServer";
                }

                if (appText.Contains("@rendermode=\"InteractiveWebAssembly\"", StringComparison.OrdinalIgnoreCase))
                {
                    return "InteractiveWebAssembly";
                }
            }

            // Static SSR / no global render mode set.
            return "None";
        }
        catch
        {
            return "None";
        }
    }

    /// <summary>
    /// Inspects the project file for an existing
    /// <c>Syncfusion.Blazor.Toolkit</c> PackageReference. Best-effort:
    /// a missing file, parse error, or unparseable XML is treated as
    /// "unknown" and reported as not-yet-referenced.
    /// </summary>
    private bool IsPackageAlreadyReferenced(string projectPath)
    {
        try
        {
            string csprojText = File.ReadAllText(projectPath);
            return csprojText.Contains("Syncfusion.Blazor.Toolkit", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Syncfusion Blazor Toolkit could not inspect the project file for an existing package reference. " +
                "Idempotency detection will report the package as 'not yet referenced'.");
            return false;
        }
    }

    /// <summary>
    /// Inspects Program.cs for an existing
    /// <c>AddSyncfusionBlazorToolkit</c> service registration. Returns
    /// false when Program.cs is missing or cannot be read.
    /// </summary>
    private bool IsServicesAlreadyRegistered(string projectDirectory)
    {
        try
        {
            string programPath = Path.Combine(projectDirectory, "Program.cs");
            if (!_fileSystem.FileExists(programPath))
            {
                return false;
            }

            string programText = File.ReadAllText(programPath);
            return programText.Contains("AddSyncfusionBlazorToolkit", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Syncfusion Blazor Toolkit could not inspect Program.cs for an existing service registration. " +
                "Idempotency detection will report services as 'not yet registered'.");
            return false;
        }
    }

    private enum ProjectKind
    {
        Unknown = 0,
        WebApp = 1,
        StandaloneWasm = 2,
        UnsupportedHybrid = 3,
        NotBlazor = 4,
    }
}