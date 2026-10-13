// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;

/// <summary>
/// Dedicated executable step that writes
/// <c>builder.Services.AddSyncfusionBlazorToolkit();</c> into the
/// project's <c>Program.cs</c> and the
/// <c>using Syncfusion.Blazor.Toolkit;</c> directive at the top of the
/// file.
///
/// <para>This step replaces the shared JSON/Roslyn code-modification
/// recipe (which produced invalid C# for standalone Blazor
/// WebAssembly projects because the framework's
/// <c>WebApplication.CreateBuilder</c> → <c>builder</c> variable-name
/// transformation does not recognise the
/// <c>WebAssemblyHostBuilder.CreateDefault</c> pattern). The shared
/// recipe is preserved in the embedded config files for any external
/// consumer but its <c>CodeChanges</c> list is now empty.</para>
///
/// <para>Detection is done by <see cref="SyncfusionBlazorToolkitHelper"/>:
/// the function locates <c>Program.cs</c> in the project root or one
/// level of subdirectories, fails with guidance when none exists
/// (it never creates one), and re-runs are no-ops when the registration is already
/// present. The configured builder variable name is reused so projects
/// that pick a non-default name (<c>appBuilder</c>, <c>wasmBuilder</c>,
/// etc.) keep working.</para>
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

        var hostKind = DetectProgramHostKind(projectToOperateOn);
        if (hostKind is null)
        {
            _logger.LogError(
                "Syncfusion Blazor Toolkit Program.cs step could not determine the hosting model for '{Project}'. " +
                "The project must declare Microsoft.NET.Sdk.Web, Microsoft.NET.Sdk.Blazor, or " +
                "Microsoft.NET.Sdk.BlazorWebAssembly. Aborting the Program.cs step.",
                projectToOperateOn);
            return Task.FromResult(false);
        }

        SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome outcome;
        string? appliedRelativePath;
        try
        {
            (outcome, appliedRelativePath) = SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(
                projectToOperateOn,
                hostKind.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Syncfusion Blazor Toolkit Program.cs step failed for '{Project}'.",
                projectToOperateOn);
            return Task.FromResult(false);
        }

        // Persist the outcome so the summary step can distinguish
        // "applied", "already present", and "skipped" instead of
        // guessing from pre-scaffolding flags. Only the server-side
        // run (TargetProjectPath == null) updates the shared settings
        // so the client run does not overwrite the server's outcome.
        bool isServerRun = string.IsNullOrEmpty(TargetProjectPath);
        if (isServerRun)
        {
            settings.ProgramCsFile = appliedRelativePath;
            settings.ProgramCsFileSkipped = outcome == SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped;
            settings.ServicesApplied = outcome != SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Skipped;
            settings.ServicesInserted =
                outcome == SyncfusionBlazorToolkitHelper.ServiceRegistrationOutcome.Inserted;
        }

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

    /// <summary>
    /// Detects whether the supplied project is a Blazor Web App / Server
    /// (<c>WebApplication.CreateBuilder</c> host) or a standalone
    /// Blazor WebAssembly (<c>WebAssemblyHostBuilder.CreateDefault</c>
    /// host) by reading the SDK declaration in the .csproj. Returns
    /// <c>null</c> when the SDK does not match either pattern — the
    /// caller treats that as a hard failure because no recipe can
    /// produce a sensible <c>Program.cs</c> for it.
    /// </summary>
    private static SyncfusionBlazorToolkitHelper.ProgramHostKind? DetectProgramHostKind(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            return null;
        }

        string text;
        try
        {
            text = File.ReadAllText(projectPath);
        }
        catch
        {
            return null;
        }

        if (Regex.IsMatch(text, "Microsoft\\.NET\\.Sdk\\.BlazorWebAssembly", RegexOptions.IgnoreCase))
        {
            return SyncfusionBlazorToolkitHelper.ProgramHostKind.StandaloneWasm;
        }

        if (Regex.IsMatch(text, "Microsoft\\.NET\\.Sdk\\.Web\\b", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(text, "Microsoft\\.NET\\.Sdk\\.Blazor\\b", RegexOptions.IgnoreCase))
        {
            return SyncfusionBlazorToolkitHelper.ProgramHostKind.WebApp;
        }

        return null;
    }
}
