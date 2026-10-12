// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;

/// <summary>
/// Logs a summary of what the Syncfusion Blazor Toolkit setup
/// scaffolder actually applied to the target project. Runs as the
/// last step so the user sees a single, actionable summary of the
/// outcomes (and any partial-success caveats) based on real step
/// results — not on flags captured before the scaffolder ran.
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
        if (!context.Properties.TryGetValue(nameof(ScaffoldSteps.Syncfusion.Settings.SyncfusionBlazorToolkitSettings), out var settingsObj) ||
            settingsObj is not ScaffoldSteps.Syncfusion.Settings.SyncfusionBlazorToolkitSettings settings)
        {
            return Task.FromResult(true);
        }

        // Compute outcome categories based on actual step results so
        // we can distinguish "changes applied", "already present", and
        // "incomplete setup". Pre-scaffolding flags are no longer used
        // as the primary signal.
        bool packageChanged = settings.PackageApplied && !settings.PackageAlreadyReferenced;
        bool packageAlreadyPresent = settings.PackageApplied && settings.PackageAlreadyReferenced;
        bool servicesChanged = settings.ServicesApplied && !settings.ServicesAlreadyRegistered;
        bool servicesAlreadyPresent = settings.ServicesApplied && settings.ServicesAlreadyRegistered;
        bool importsChanged = settings.ImportsInserted && !string.IsNullOrEmpty(settings.ImportsFile);
        bool importsMissing = !settings.ImportsApplied && settings.ImportsFileSkipped;

        bool fullyConfigured = settings.PackageApplied
            && settings.ServicesApplied
            && !importsMissing;

        bool noOp = packageAlreadyPresent
            && servicesAlreadyPresent
            && !importsChanged
            && !importsMissing
            && settings.PackageApplied
            && settings.ServicesApplied;

        if (noOp)
        {
            _logger.LogInformation(
                "Syncfusion Blazor Toolkit is already fully configured in this project. " +
                "Re-running the scaffolder was a no-op (idempotent).");
        }
        else
        {
            _logger.LogInformation("Syncfusion Blazor Toolkit setup summary:");
        }

        // Package outcome.
        if (packageChanged)
        {
            _logger.LogInformation(
                "  - Added NuGet package reference: Syncfusion.Blazor.Toolkit " +
                "(2.0.0+ ships styles with the assembly; no external stylesheet needed).");
        }
        else if (packageAlreadyPresent)
        {
            _logger.LogInformation(
                "  - Package reference: Syncfusion.Blazor.Toolkit (already present; no change)");
        }
        else
        {
            _logger.LogInformation("  - Package reference: Syncfusion.Blazor.Toolkit (no change)");
        }

        // Services outcome — never claim Program.cs was updated if
        // registration was silently skipped.
        if (servicesChanged)
        {
            _logger.LogInformation(
                "  - Updated Program.cs: builder.Services.AddSyncfusionBlazorToolkit(); + using Syncfusion.Blazor.Toolkit;");
        }
        else if (servicesAlreadyPresent)
        {
            _logger.LogInformation(
                "  - Program.cs: AddSyncfusionBlazorToolkit() already registered (no change)");
        }
        else
        {
            _logger.LogWarning(
                "  - Program.cs: AddSyncfusionBlazorToolkit() was NOT registered — the code-modification step could " +
                "not match an anchor in your Program.cs. Add the registration manually: builder.Services.AddSyncfusionBlazorToolkit(); " +
                "and 'using Syncfusion.Blazor.Toolkit;'.");
        }

        // Imports outcome.
        if (!string.IsNullOrEmpty(settings.ImportsFile) && importsChanged)
        {
            _logger.LogInformation(
                "  - Updated '{ImportsFile}': added @using Syncfusion.Blazor.Toolkit " +
                "(the single unified namespace used by every Toolkit component in 2.0.0+).",
                settings.ImportsFile);
        }
        else if (!string.IsNullOrEmpty(settings.ImportsFile))
        {
            _logger.LogInformation(
                "  - Imports: '{ImportsFile}' already contains @using Syncfusion.Blazor.Toolkit (no change).",
                settings.ImportsFile);
        }
        else if (importsMissing)
        {
            _logger.LogWarning(
                "  - _Imports.razor: SKIPPED (no suitable file could be found or created). " +
                "Add '@using Syncfusion.Blazor.Toolkit' to your _Imports file manually if your project uses one.");
        }

        // Multi-project (Blazor Web App Auto) guidance. The detection
        // is best-effort and only fires when a .Client sibling project
        // is resolved via MSBuildProjectReferences.
        if (!string.IsNullOrEmpty(settings.ClientProjectPath))
        {
            switch (settings.Layout)
            {
                case ScaffoldSteps.Syncfusion.Settings.SyncfusionBlazorToolkitLayout.AutoServer:
                    _logger.LogInformation(
                        "  - Detected Blazor Web App Auto layout. This is the SERVER project: Program.cs (AddSyncfusionBlazorToolkit) lives here. " +
                        "The scaffolder also installed the Syncfusion.Blazor.Toolkit package on the resolved client project ('{ClientProject}').",
                        Path.GetFileName(settings.ClientProjectPath));
                    break;
                case ScaffoldSteps.Syncfusion.Settings.SyncfusionBlazorToolkitLayout.AutoClient:
                    _logger.LogInformation(
                        "  - Detected Blazor Web App Auto layout. This is the CLIENT project: components live here. " +
                        "Make sure to also run the scaffolder on the SERVER project so AddSyncfusionBlazorToolkit() is registered in the host that builds the DI pipeline.");
                    break;
            }
        }

        // Hosting / render-mode guidance — only mention what actually applies.
        _logger.LogInformation("  - Hosting: " + DescribeHostingGuidance(settings));

        if (fullyConfigured && !noOp)
        {
            _logger.LogInformation(
                "  - Styles: Syncfusion.Blazor.Toolkit 2.0.0+ ships styles with the assembly. No host stylesheet link was added.");
        }
        else
        {
            _logger.LogInformation(
                "  - Styles: Syncfusion.Blazor.Toolkit 2.0.0+ ships styles with the assembly. No host stylesheet link was added.");
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Returns hosting guidance that reflects what the project actually
    /// has configured, instead of unconditionally listing every
    /// interactive render mode. Does not change global render modes
    /// or page render modes — only suggests the missing piece.
    /// </summary>
    private string DescribeHostingGuidance(ScaffoldSteps.Syncfusion.Settings.SyncfusionBlazorToolkitSettings settings)
    {
        return settings.DetectedRenderMode switch
        {
            "InteractiveAuto" =>
                "Detected InteractiveAuto. Use Toolkit components with an interactive render mode; no further hosting changes required.",
            "InteractiveServer" =>
                "Detected InteractiveServer. Use Toolkit components with @rendermode InteractiveServer; the Syncfusion.Blazor.Toolkit package on the server is enough — no server-side webapi render-mode registration required.",
            "InteractiveWebAssembly" =>
                "Detected InteractiveWebAssembly. Use Toolkit components with @rendermode InteractiveWebAssembly; static SSR is fine for the host page.",
            _ when !string.IsNullOrEmpty(settings.ClientProjectPath) =>
                "Detected Blazor Web App Auto with no global render mode. Use Toolkit components with an explicit render mode (InteractiveServer or InteractiveWebAssembly) on the client side; no global rendermode change was applied.",
            _ =>
                "Detected static SSR / no interactivity configured. Use Toolkit components inside an interactive child component, or add interactivity (AddInteractiveServerRenderMode / AddInteractiveWebAssemblyRenderMode) before using @rendermode InteractiveAuto.",
        };
    }
}