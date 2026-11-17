// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;

/// <summary>
/// Registers <c>AddSyncfusionBlazorToolkit()</c> on the application's host builder, before
/// <c>Build()</c>, and adds <c>using Syncfusion.Blazor.Toolkit;</c> to the same file.
///
/// <para>The entry point is found from the project's compiled sources (see
/// <see cref="SyncfusionBlazorToolkitHelper.DiscoverEntryPoint"/>), the builder variable
/// name is reused as written, and a re-run is a no-op when the registration is already in
/// place. When no safe entry point or host builder can be identified the step changes
/// nothing and fails with guidance; it never creates a <c>Program.cs</c>.</para>
/// </summary>
internal class ApplySyncfusionBlazorToolkitProgramCsStep : ScaffoldStep
{
    private readonly ILogger _logger;

    /// <summary>
    /// Optional override for the project the step targets. When set,
    /// the step runs against this project path instead of the
    /// server-side project path on
    /// <see cref="SyncfusionBlazorToolkitSettings"/>. Used to register
    /// the service on a resolved Blazor WebAssembly client without
    /// coupling the helper to a per-step subclass.
    /// </summary>
    public string? TargetProjectPath { get; set; }

    public ApplySyncfusionBlazorToolkitProgramCsStep(ILogger<ApplySyncfusionBlazorToolkitProgramCsStep> logger)
    {
        _logger = logger;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Properties.TryGetValue(nameof(SyncfusionBlazorToolkitSettings), out var settingsObj) ||
            settingsObj is not SyncfusionBlazorToolkitSettings settings)
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit Program.cs step is missing required context: 'SyncfusionBlazorToolkitSettings'. " +
                "Aborting the Program.cs step.");
            return Task.FromResult(false);
        }

        string? projectToOperateOn = !string.IsNullOrEmpty(TargetProjectPath)
            ? TargetProjectPath
            : settings.Project;
        if (string.IsNullOrEmpty(projectToOperateOn))
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit Program.cs step received an empty project path. " +
                "Aborting the Program.cs step.");
            return Task.FromResult(false);
        }

        // The kind was decided once by the validate step. A client run targets the project the
        // resolver found by its UsingMicrosoftNETSdkBlazorWebAssembly property, so it is WebAssembly by construction.
        bool isServerRun = string.IsNullOrEmpty(TargetProjectPath);
        SyncfusionBlazorToolkitHelper.ProgramHostKind hostKind =
            !isServerRun || settings.AppKind == BlazorAppKind.StandaloneWasm
                ? SyncfusionBlazorToolkitHelper.ProgramHostKind.StandaloneWasm
                : SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp;

        SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome outcome;
        string? appliedRelativePath;
        try
        {
            (outcome, appliedRelativePath) = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
                projectToOperateOn,
                hostKind);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Syncfusion Blazor Toolkit Program.cs step failed for '{Project}'.",
                projectToOperateOn);
            return Task.FromResult(false);
        }

        // Persist what actually happened so the summary can report it per project.
        // Only the server run updates the shared entry-point fields; each run records
        // its own insertion on its own project.
        if (isServerRun)
        {
            settings.ProgramCsFile = appliedRelativePath;
            settings.ProgramCsFileSkipped = outcome == SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped;
            settings.ServicesApplied = outcome != SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped;
        }

        ProjectChanges changes = isServerRun ? settings.Server : settings.Client;
        changes.ServicesInserted = outcome == SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted;

        switch (outcome)
        {
            case SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.AlreadyPresent:
                _logger.LogInformation(
                    "Syncfusion Blazor Toolkit: AddSyncfusionBlazorToolkit() is already registered in '{ProgramCs}'.",
                    appliedRelativePath);
                break;
            case SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted:
                _logger.LogInformation(
                    "Syncfusion Blazor Toolkit: added AddSyncfusionBlazorToolkit() to '{ProgramCs}'.",
                    appliedRelativePath);
                break;
            case SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped:
            default:
                // Fail closed: never invent a second entry point (CS8802) or
                // register after Build(). The user adds the call themselves.
                _logger.LogError(
                    "Syncfusion Blazor Toolkit: could not find a safe place to register services for '{Project}'. " +
                    "The entry point (target: '{ProgramCs}') must declare the framework host builder " +
                    "('WebApplication.CreateBuilder(...)' / 'WebAssemblyHostBuilder.CreateDefault(...)', aliases allowed) as a " +
                    "top-level or Main statement and call 'Build()' on it; otherwise registration cannot be verified. " +
                    "No files were created or changed. Add " +
                    "'builder.Services.AddSyncfusionBlazorToolkit();' to your existing application entry point, " +
                    "before 'Build()', and re-run the scaffolder.",
                    projectToOperateOn,
                    appliedRelativePath ?? "Program.cs");
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
