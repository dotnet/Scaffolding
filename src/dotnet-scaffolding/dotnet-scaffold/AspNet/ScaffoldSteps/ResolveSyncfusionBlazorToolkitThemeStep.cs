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
/// If neither host file is present the theme step is skipped (logged as
/// a warning). Package install, Program.cs registration, and the
/// Components/_Imports.razor @using directive are still applied.
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
    ];

    public ResolveSyncfusionBlazorToolkitThemeStep(
        IFileSystem fileSystem,
        ILogger<ResolveSyncfusionBlazorToolkitThemeStep> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
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

        foreach (var (relativePath, kind) in ThemeHostCandidates)
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
                settings.ThemeFile = Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.SyncfusionBlazorToolkitHelper.ToOsNativePath(relativePath);
                settings.ThemeFileSkipped = false;
                _logger.LogInformation(
                    "Syncfusion Blazor Toolkit theme stylesheet will be added to '{ThemeFile}' ({Kind}).",
                    settings.ThemeFile,
                    kind);
                return Task.FromResult(true);
            }
        }

        // Neither Components/App.razor nor wwwroot/index.html was found.
        // Don't fail the whole scaffolder; the rest of the setup (package,
        // Program.cs, _Imports.razor) is still useful.
        settings.ThemeFile = null;
        settings.ThemeFileSkipped = true;
        _logger.LogWarning(
            "Syncfusion Blazor Toolkit theme stylesheet was skipped: neither '{AppRazor}' nor '{IndexHtml}' was found in the project. " +
            "The package, Program.cs registration, and Components/_Imports.razor @using directive were still applied; " +
            "add the theme <link> manually if your project uses a different host file.",
            Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.SyncfusionBlazorToolkitHelper.ToOsNativePath(Path.Combine("Components", "App.razor")),
            Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.SyncfusionBlazorToolkitHelper.ToOsNativePath(Path.Combine("wwwroot", "index.html")));
        return Task.FromResult(true);
    }
}
