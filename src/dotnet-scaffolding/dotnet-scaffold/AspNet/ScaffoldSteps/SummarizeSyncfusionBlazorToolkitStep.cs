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
///
/// <para><b>Syncfusion.Blazor.Toolkit 2.0.0+:</b> The summary explicitly
/// notes that no external stylesheet is required in
/// <c>App.razor</c> or <c>wwwroot/index.html</c> and that a single
/// <c>@using Syncfusion.Blazor.Toolkit</c> directive is sufficient for
/// every Toolkit component (domain-specific usings are no longer
/// required).</para>
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
                : "  - Added NuGet package reference: Syncfusion.Blazor.Toolkit (2.0.0+ ships styles with the assembly; no external stylesheet needed).");

        _logger.LogInformation(
            settings.ServicesAlreadyRegistered
                ? "  - Program.cs: AddSyncfusionBlazorToolkit() already registered (no change)"
                : "  - Updated Program.cs: using Syncfusion.Blazor.Toolkit; + builder.Services.AddSyncfusionBlazorToolkit();");

        if (!string.IsNullOrEmpty(settings.ImportsFile))
        {
            _logger.LogInformation(
                settings.ImportsFileSkipped
                    ? "  - Imports: skipped (already present)"
                    : "  - Updated '{ImportsFile}': added @using Syncfusion.Blazor.Toolkit (the single unified namespace used by every Toolkit component in 2.0.0+).",
                settings.ImportsFile);
        }
        else if (settings.ImportsFileSkipped)
        {
            _logger.LogWarning(
                "  - _Imports.razor: SKIPPED (no suitable file found). " +
                "Add '@using Syncfusion.Blazor.Toolkit' to your _Imports file manually if your project uses one.");
        }

        _logger.LogInformation(
            "  - Styles: Syncfusion.Blazor.Toolkit 2.0.0+ ships styles with the assembly. No host stylesheet link was added.");

        // Multi-project (Blazor Web App Auto) guidance. The detection
        // is best-effort and only fires when a .Client sibling project
        // is found next to the current project.
        switch (settings.Layout)
        {
            case ScaffoldSteps.Settings.SyncfusionBlazorToolkitLayout.AutoServer:
                _logger.LogInformation(
                    "  - Detected Blazor Web App Auto layout. This is the SERVER project: Program.cs (AddSyncfusionBlazorToolkit) and DI live here. " +
                    "Re-run the scaffolder with --project pointing at the .Client project too so the @using Syncfusion.Blazor.Toolkit directive is added to the client-side _Imports.razor where components are compiled.");
                break;
            case ScaffoldSteps.Settings.SyncfusionBlazorToolkitLayout.AutoClient:
                _logger.LogInformation(
                    "  - Detected Blazor Web App Auto layout. This is the CLIENT project: components and static assets live here. " +
                    "Make sure to also run the scaffolder on the SERVER project so AddSyncfusionBlazorToolkit() is registered in the host that builds the DI pipeline.");
                break;
        }

        _logger.LogInformation(
            "Next steps: " +
            "(1) Use Toolkit components with an interactive render mode " +
            "(@rendermode InteractiveServer / InteractiveAuto / InteractiveWebAssembly). " +
            "(2) For Blazor Web App (Auto) multi-project solutions, run this scaffolder on BOTH the server project (for AddSyncfusionBlazorToolkit) and the .Client project (for the @using directive). " +
            "(3) Syncfusion.Blazor.Toolkit 2.0.0+ ships styles with the assembly and exposes a single @using namespace, so no App.razor / wwwroot/index.html link tag and no per-component usings are required. " +
            "(4) Re-run is idempotent for package, Program.cs, and _Imports.");

        return Task.FromResult(true);
    }
}
