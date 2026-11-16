// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;

/// <summary>
/// Dedicated executable step that ensures the
/// <c>@using Syncfusion.Blazor.Toolkit</c> directive is present in the
/// project's <c>_Imports.razor</c> file.
///
/// <para>The detection, placement, and idempotency are all driven by
/// <see cref="SyncfusionBlazorToolkitHelper.EnsureImportsUsing"/> so
/// the import update does not depend on ad-hoc helper side-effects
/// from other steps. The outcome (already present / inserted / created /
/// skipped) is recorded on the settings so the summary step can give
/// an accurate, outcome-based report.</para>
/// </summary>
internal class ApplySyncfusionBlazorToolkitImportsStep : ScaffoldStep
{
    private readonly ILogger _logger;

    /// <summary>
    /// Optional override for the project the imports step targets.
    /// When set, the step runs against this project path instead of
    /// the server project path on <see cref="SyncfusionBlazorToolkitSettings"/>.
    /// Used to apply the directive to the resolved Blazor WebAssembly
    /// client without coupling the imports helper to a per-step
    /// subclass.
    /// </summary>
    public string? TargetProjectPath { get; set; }

    public ApplySyncfusionBlazorToolkitImportsStep(ILogger<ApplySyncfusionBlazorToolkitImportsStep> logger)
    {
        _logger = logger;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Properties.TryGetValue(nameof(SyncfusionBlazorToolkitSettings), out var settingsObj) ||
            settingsObj is not SyncfusionBlazorToolkitSettings settings)
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit imports step is missing required context: 'SyncfusionBlazorToolkitSettings'. " +
                "Aborting the imports step.");
            return Task.FromResult(false);
        }

        string? projectToOperateOn = !string.IsNullOrEmpty(TargetProjectPath) ? TargetProjectPath : settings.Project;
        if (string.IsNullOrEmpty(projectToOperateOn))
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit imports step received an empty project path. " +
                "Aborting the imports step.");
            return Task.FromResult(false);
        }

        SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome outcome;
        string? appliedRelativePath;
        try
        {
            (outcome, appliedRelativePath) = SyncfusionBlazorToolkitHelper.EnsureImportsUsing(projectToOperateOn);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex.Message);
            settings.ImportsFileSkipped = true;
            settings.ImportsApplied = false;
            return Task.FromResult(false);
        }

        // Persist what actually happened. The shared fields describe the server project;
        // each run records its own insertion on its own project.
        bool isServerRun = string.IsNullOrEmpty(TargetProjectPath);
        if (isServerRun)
        {
            settings.ImportsFile = appliedRelativePath;
            settings.ImportsFileSkipped = outcome == SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Skipped;
            settings.ImportsApplied = outcome != SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Skipped;
        }

        ProjectChanges changes = isServerRun ? settings.Server : settings.Client;
        changes.ImportsInserted =
            outcome == SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Inserted ||
            outcome == SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Created;

        switch (outcome)
        {
            case SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.AlreadyPresent:
                _logger.LogInformation(
                    "Syncfusion Blazor Toolkit: @using Syncfusion.Blazor.Toolkit is already present in '{ImportsFile}'.",
                    appliedRelativePath);
                break;
            case SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Inserted:
                _logger.LogInformation(
                    "Syncfusion Blazor Toolkit: added @using Syncfusion.Blazor.Toolkit to '{ImportsFile}'.",
                    appliedRelativePath);
                break;
            case SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Created:
                _logger.LogInformation(
                    "Syncfusion Blazor Toolkit: created '{ImportsFile}' with @using Syncfusion.Blazor.Toolkit.",
                    appliedRelativePath);
                break;
            case SyncfusionBlazorToolkitHelper.ImportsUpdateOutcome.Skipped:
            default:
                _logger.LogWarning(
                    "Syncfusion Blazor Toolkit: no suitable _Imports.razor found or created under '{ProjectDirectory}'. " +
                    "Add '@using Syncfusion.Blazor.Toolkit' to your _Imports file manually if your project uses one.",
                    Path.GetDirectoryName(settings.Project) ?? settings.Project);
                break;
        }

        return Task.FromResult(true);
    }
}
