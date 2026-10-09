// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Resolves which file should host the Syncfusion Blazor Toolkit theme
/// stylesheet (Components/App.razor for Blazor Web App; wwwroot/index.html
/// for standalone Blazor WASM). The chosen path is stored on the
/// SyncfusionBlazorToolkitSettings in the ScaffolderContext so the
/// code-modification step can target the right file.
///
/// <para><b>Syncfusion.Blazor.Toolkit 2.0.0+:</b> Theme styles are
/// bundled with the package and activated through the DI registration
/// (<c>AddSyncfusionBlazorToolkit()</c>). The scaffolder no longer
/// injects an external <c>&lt;link href="_content/.../fluent.min.css"/&gt;</c>
/// tag into the host file. The <c>ThemeFile</c> / <c>ThemeFileSkipped</c>
/// settings are still computed so the summary step can describe what
/// was (or wasn't) changed, and so callers that explicitly target a
/// pre-2.0.0 version of the package still work.</para>
///
/// <para>If no host file is found, the theme step is skipped (logged as a
/// warning). Package install, Program.cs registration, and the
/// Components/_Imports.razor @using directive are still applied.</para>
/// </summary>
internal class ResolveSyncfusionBlazorToolkitThemeStep : ScaffoldStep
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    // Theme host candidates in priority order. The first one that exists
    // on disk wins. Paths are project-relative and use Path.Combine so
    // they work on Windows and Unix.
    private static readonly (string RelativePath, string Kind)[] ThemeHostCandidates =
    [
        (Path.Combine("Components", "App.razor"), "Blazor Web App (Components/App.razor)"),
        (Path.Combine("wwwroot", "index.html"), "Standalone Blazor WASM (wwwroot/index.html)"),
        (Path.Combine("App.razor"), "Hybrid / older template (root App.razor)"),
    ];

    public ResolveSyncfusionBlazorToolkitThemeStep(
        IFileSystem fileSystem,
        ILogger<ResolveSyncfusionBlazorToolkitThemeStep> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Returns the theme host candidate list reordered so the project's SDK is
    /// preferred. Standalone Blazor WASM projects put their host page at
    /// <c>wwwroot/index.html</c>; Blazor Web App / Server projects put it at
    /// <c>Components/App.razor</c>. Putting the matching one first avoids the
    /// rare case where a project legitimately has both files (e.g. a hybrid
    /// sample) and the wrong host wins.
    /// </summary>
    private static (string RelativePath, string Kind)[] GetThemeHostCandidates(string projectDirectory)
    {
        var csproj = SafeEnumerateCsproj(projectDirectory);
        var isWasm = false;
        if (csproj is not null)
        {
            try
            {
                var text = File.ReadAllText(csproj);
                isWasm = text.Contains("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // best-effort; fall through with the default order
                isWasm = false;
            }
        }

        // WASM: prefer index.html; Web App / Server: prefer Components/App.razor
        return isWasm
            ?
            [
                (Path.Combine("wwwroot", "index.html"), "Standalone Blazor WASM (wwwroot/index.html)"),
                (Path.Combine("Components", "App.razor"), "Blazor Web App (Components/App.razor)"),
                (Path.Combine("App.razor"), "Hybrid / older template (root App.razor)"),
            ]
            :
            [
                (Path.Combine("Components", "App.razor"), "Blazor Web App (Components/App.razor)"),
                (Path.Combine("wwwroot", "index.html"), "Standalone Blazor WASM (wwwroot/index.html)"),
                (Path.Combine("App.razor"), "Hybrid / older template (root App.razor)"),
            ];
    }

    private static string? SafeEnumerateCsproj(string projectDirectory)
    {
        try
        {
            return Directory.EnumerateFiles(projectDirectory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Properties.TryGetValue(nameof(ScaffoldSteps.Settings.SyncfusionBlazorToolkitSettings), out var settingsObj) ||
            settingsObj is not ScaffoldSteps.Settings.SyncfusionBlazorToolkitSettings settings)
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit theme resolution requires 'SyncfusionBlazorToolkitSettings' to be present " +
                "in 'ScaffolderContext.Properties'. Skipping theme step.");
            return Task.FromResult(false);
        }

        var projectDirectory = Path.GetDirectoryName(settings.Project);
        if (string.IsNullOrEmpty(projectDirectory) || !_fileSystem.DirectoryExists(projectDirectory))
        {
            _logger.LogWarning(
                "Syncfusion Blazor Toolkit theme stylesheet was skipped because the project directory '{ProjectDirectory}' could not be found.",
                projectDirectory);
            settings.ThemeFile = null;
            settings.ThemeFileSkipped = true;
            return Task.FromResult(true);
        }

        foreach (var (relativePath, kind) in GetThemeHostCandidates(projectDirectory))
        {
            var fullPath = Path.Combine(projectDirectory, relativePath);
            if (_fileSystem.FileExists(fullPath))
            {
                // Store the path in OS-native separator form (backslash on
                // Windows, forward slash on Linux). The CodeModifier looks
                // the file up via EndsWith against AdditionalDocument
                // paths, which are always OS-native. Canonicalizing to
                // forward slashes here would silently break the lookup on
                // Windows and cause the theme stylesheet to be skipped.
                //
                // With Syncfusion.Blazor.Toolkit 2.0.0+ the resolved path
                // is recorded for the summary step and any pre-2.0.0
                // compatibility path; the code-modification step does not
                // emit a theme entry by default.
                settings.ThemeFile = Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.SyncfusionBlazorToolkitHelper.ToOsNativePath(relativePath);
                settings.ThemeFileSkipped = false;
                _logger.LogInformation(
                    "Syncfusion Blazor Toolkit detected theme host '{ThemeFile}' ({Kind}). " +
                    "No external stylesheet link will be injected because the 2.0.0 package ships styles with the assembly and activates them through AddSyncfusionBlazorToolkit().",
                    settings.ThemeFile,
                    kind);
                return Task.FromResult(true);
            }
        }

        // No theme host found. Don't fail the whole scaffolder; the rest
        // of the setup (package, Program.cs, _Imports.razor) is still
        // useful.
        settings.ThemeFile = null;
        settings.ThemeFileSkipped = true;
        _logger.LogInformation(
            "Syncfusion Blazor Toolkit did not find a theme host file (Components/App.razor, wwwroot/index.html, or root App.razor) under '{ProjectDirectory}'. " +
            "No external stylesheet link is required for the 2.0.0+ package; styles are bundled and activated through AddSyncfusionBlazorToolkit().",
            projectDirectory);
        return Task.FromResult(true);
    }
}
