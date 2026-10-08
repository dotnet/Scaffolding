// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Validates Syncfusion Blazor Toolkit settings and seeds the project context
/// for the setup scaffolder.
///
/// <para>The setup scaffolder requires only a valid .csproj path. It then
/// performs a soft discovery pass for the optional host files it can
/// modify:</para>
/// <list type="bullet">
///   <item>Components/_Imports.razor — preferred, then _Imports.razor at
///         the project root, then any _Imports.razor under the project. If
///         none is found, the using-directive step is skipped (logged as
///         a warning) instead of failing the whole scaffolder.</item>
///   <item>Components/App.razor — preferred theme host, then
///         wwwroot/index.html. If neither is found, the theme step is
///         skipped (logged as a warning).</item>
/// </list>
///
/// <para>The package install and Program.cs service-registration steps
/// run regardless of which optional files are present, so the scaffolder
/// is useful in Blazor Web App, standalone Blazor WASM, and hybrid
/// layouts.</para>
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

        // Only the project file itself is strictly required. The
        // _Imports.razor and theme host files are discovered with a
        // soft-fail so the scaffolder still completes successfully when
        // they are absent.
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

        // Soft-fail imports discovery. Components/_Imports.razor is
        // preferred; _Imports.razor at the project root and any
        // _Imports.razor under the project are accepted as fallbacks
        // (older Blazor layouts, standalone WASM, hybrid layouts, and
        // global-using scenarios). When none is present, the using
        // insertion is skipped with a warning.
        string? importsPath = DiscoverImportsFile(projectDirectory);
        if (!string.IsNullOrEmpty(importsPath))
        {
            // Use OS-native separators for the path that the CodeModifier
            // will look up. Path.GetRelativePath returns OS-native form
            // ("Components\_Imports.razor" on Windows,
            // "Components/_Imports.razor" on Linux). Canonicalizing to
            // forward slashes would silently break the file lookup on
            // Windows and cause the @using directive to be skipped.
            string relativePath = Path.GetRelativePath(projectDirectory, importsPath);
            settings.ImportsFile = SyncfusionBlazorToolkitHelper.ToOsNativePath(relativePath);
            settings.ImportsFileSkipped = false;
        }
        else
        {
            settings.ImportsFile = null;
            settings.ImportsFileSkipped = true;
            _logger.LogWarning(
                "Syncfusion Blazor Toolkit could not find any '_Imports.razor' file under '{ProjectDirectory}' " +
                "(checked Components/_Imports.razor, _Imports.razor, and any nested _Imports.razor). " +
                "The @using directive will not be added automatically. " +
                "Add '@using Syncfusion.Blazor.Toolkit' to your _Imports file manually if your project uses one outside these locations.",
                projectDirectory);
        }

        // Detect pre-existing configuration so the summary can describe
        // an idempotent re-run instead of a fresh install.
        settings.PackageAlreadyReferenced = IsPackageAlreadyReferenced(projectDirectory);
        settings.ServicesAlreadyRegistered = IsServicesAlreadyRegistered(projectDirectory);

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

        return new SyncfusionBlazorToolkitSettings
        {
            Project = Project,
            Prerelease = Prerelease,
        };
    }

    /// <summary>
    /// Looks for an _Imports.razor file in the conventional Blazor
    /// locations. The first match wins. Returns the absolute path of
    /// the discovered file, or null when no candidate exists.
    /// </summary>
    private string? DiscoverImportsFile(string projectDirectory)
    {
        // Preferred order mirrors the conventional Blazor Web App layout,
        // then standalone Blazor WASM (no Components folder), then a
        // catch-all for any _Imports.razor under the project root.
        string[] candidates =
        [
            Path.Combine(projectDirectory, "Components", "_Imports.razor"),
            Path.Combine(projectDirectory, "_Imports.razor"),
        ];

        foreach (string candidate in candidates)
        {
            if (_fileSystem.FileExists(candidate))
            {
                return candidate;
            }
        }

        // Catch-all: any _Imports.razor under the project. Enumerate
        // manually so we stay IFileSystem-agnostic and avoid recursing
        // into well-known excluded directories (bin, obj, node_modules).
        return FindFirstImportsRazor(projectDirectory, projectDirectory);
    }

    private string? FindFirstImportsRazor(string projectDirectory, string currentDirectory)
    {
        if (!_fileSystem.DirectoryExists(currentDirectory))
        {
            return null;
        }

        string directMatch = Path.Combine(currentDirectory, "_Imports.razor");
        if (_fileSystem.FileExists(directMatch))
        {
            return directMatch;
        }

        foreach (string subdirectory in _fileSystem.EnumerateDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(subdirectory);
            if (string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Don't recurse into nested project directories; their
            // _Imports.razor belongs to a different project.
            if (subdirectory != projectDirectory &&
                _fileSystem.EnumerateFiles(subdirectory, "*.csproj", SearchOption.TopDirectoryOnly).Any())
            {
                continue;
            }

            string? nested = FindFirstImportsRazor(projectDirectory, subdirectory);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    /// <summary>
    /// Inspects the project file for an existing
    /// <c>Syncfusion.Blazor.Toolkit</c> PackageReference. Best-effort:
    /// a missing file, parse error, or unparseable XML is treated as
    /// "unknown" and reported as not-yet-referenced.
    /// </summary>
    private bool IsPackageAlreadyReferenced(string projectDirectory)
    {
        try
        {
            // Look for a project file at the directory root.
            string? csprojPath = _fileSystem.EnumerateFiles(projectDirectory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (string.IsNullOrEmpty(csprojPath))
            {
                return false;
            }

            string csprojText = File.ReadAllText(csprojPath);
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
}
