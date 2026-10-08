// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Logs a summary of what the Syncfusion Blazor Toolkit setup
/// scaffolder applied to the target project. Runs as the last step so
/// the user sees a single, actionable summary of the changes (and any
/// partial-success caveats).
/// </summary>
internal class SummarizeSyncfusionBlazorToolkitStep : ScaffoldStep
{
    private readonly ILogger _logger;

    public SummarizeSyncfusionBlazorToolkitStep(ILogger<SummarizeSyncfusionBlazorToolkitStep> logger)
    {
        _logger = logger;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Properties.TryGetValue(nameof(ScaffoldSteps.Settings.SyncfusionBlazorToolkitSettings), out var settingsObj) ||
            settingsObj is not ScaffoldSteps.Settings.SyncfusionBlazorToolkitSettings settings)
        {
            return Task.FromResult(true);
        }

        bool fullyConfigured = settings.PackageAlreadyReferenced
            && settings.ServicesAlreadyRegistered
            && (!string.IsNullOrEmpty(settings.ThemeFile) || settings.ThemeFileSkipped)
            && (!string.IsNullOrEmpty(settings.ImportsFile) || settings.ImportsFileSkipped);

        if (fullyConfigured)
        {
            _logger.LogInformation(
                "Syncfusion Blazor Toolkit is already fully configured in this project. " +
                "Re-running the scaffolder was a no-op (idempotent).");
        }
        else
        {
            _logger.LogInformation("Syncfusion Blazor Toolkit setup summary:");
        }

        _logger.LogInformation(
            settings.PackageAlreadyReferenced
                ? "  - Package reference: Syncfusion.Blazor.Toolkit (already present; no change)"
                : "  - Added NuGet package reference: Syncfusion.Blazor.Toolkit");

        _logger.LogInformation(
            settings.ServicesAlreadyRegistered
                ? "  - Program.cs: AddSyncfusionBlazorToolkit() already registered (no change)"
                : "  - Updated Program.cs: using Syncfusion.Blazor.Toolkit; + builder.Services.AddSyncfusionBlazorToolkit();");

        if (!string.IsNullOrEmpty(settings.ImportsFile))
        {
            _logger.LogInformation(
                settings.ImportsFileSkipped
                    ? "  - Imports: skipped (already present)"
                    : "  - Updated '{ImportsFile}': added @using Syncfusion.Blazor.Toolkit",
                settings.ImportsFile);
        }
        else if (settings.ImportsFileSkipped)
        {
            _logger.LogWarning(
                "  - _Imports.razor: SKIPPED (no suitable file found). " +
                "Add '@using Syncfusion.Blazor.Toolkit' to your _Imports file manually if your project uses one.");
        }

        if (!string.IsNullOrEmpty(settings.ThemeFile))
        {
            _logger.LogInformation(
                settings.ThemeFileSkipped
                    ? "  - Theme stylesheet: skipped (already present)"
                    : "  - Updated '{ThemeFile}': added Syncfusion theme stylesheet link.",
                settings.ThemeFile);
        }
        else if (settings.ThemeFileSkipped)
        {
            _logger.LogWarning(
                "  - Theme stylesheet was SKIPPED: no suitable host file (Components/App.razor or wwwroot/index.html) was found. " +
                "Add the <link href=\"_content/Syncfusion.Blazor.Toolkit/styles/fluent.min.css\" rel=\"stylesheet\" /> tag manually if needed.");
        }

        _logger.LogInformation(
            "Next steps: " +
            "(1) Add Syncfusion Blazor Toolkit components to your pages (e.g. <SfButton>, <SfGrid>). " +
            "(2) Verify the page or component hosting the Toolkit is rendered with an interactive render mode " +
            "(@rendermode InteractiveServer / InteractiveAuto / InteractiveWebAssembly). " +
            "Static SSR is not supported by Syncfusion Blazor Toolkit components. " +
            "(3) Re-run the scaffolder at any time: the package, Program.cs, _Imports, and theme are all idempotent.");

        return Task.FromResult(true);
    }
}
