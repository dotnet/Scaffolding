// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Extensions.Syncfusion;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Syncfusion Blazor
/// Toolkit setup scaffolding steps. The setup scaffolder only adds the NuGet package and
/// applies a small set of code changes (a dedicated Program.cs service-registration
/// step and a dedicated imports step); it does not generate any Razor pages or inject
/// a host stylesheet.
///
/// <para><b>Syncfusion.Blazor.Toolkit 2.0.0+ behavior:</b> Styles ship with the
/// assembly and are activated through <c>AddSyncfusionBlazorToolkit()</c>, so no
/// external <c>&lt;link&gt;</c> tag is injected into <c>App.razor</c> or
/// <c>wwwroot/index.html</c>. Components expose a single namespace
/// (<c>@using Syncfusion.Blazor.Toolkit</c>), so only one using directive is
/// added regardless of which components the project uses.</para>
///
/// <para>Configuration flows in the following order:</para>
/// <list type="number">
///   <item><description>Validate (normalize path, validate project kind, detect client / hosting).</description></item>
///   <item><description>Add server NuGet package.</description></item>
///   <item><description>Add client NuGet package (only when a WebAssembly client is resolved).</description></item>
///   <item><description>Register <c>AddSyncfusionBlazorToolkit()</c> in the server's <c>Program.cs</c>.</description></item>
///   <item><description>Register <c>AddSyncfusionBlazorToolkit()</c> in the resolved client's <c>Program.cs</c> (if any).</description></item>
///   <item><description>Dedicated imports step for the server project's <c>_Imports.razor</c>.</description></item>
///   <item><description>Dedicated imports step for the resolved client's <c>_Imports.razor</c>.</description></item>
///   <item><description>Verification step captures the actual post-state of the project.</description></item>
///   <item><description>Summary step reports the outcome.</description></item>
/// </list>
/// </summary>
internal static class SyncfusionBlazorToolkitScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a step that installs the Syncfusion.Blazor.Toolkit package on
    /// the selected server project. Skips itself when the settings are
    /// missing from the scaffolder context.
    /// </summary>
    public static IScaffoldBuilder WithSyncfusionBlazorToolkitAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            var packages = new List<Package>
            {
                PackageConstants.AspNetCorePackages.SyncfusionBlazorToolkitPackage
            };

            if (context.Properties.TryGetValue(nameof(SyncfusionBlazorToolkitSettings), out var settingsObj) &&
                settingsObj is SyncfusionBlazorToolkitSettings settings &&
                !string.IsNullOrEmpty(settings.Project))
            {
                step.ProjectPath = settings.Project;
                step.Packages = packages;
            }
            else
            {
                // Not a user-facing prerequisite and not another scaffolder:
                // 'SyncfusionBlazorToolkitSettings' is the in-memory state the
                // first step of this scaffolder (ValidateSyncfusionBlazorToolkitStep)
                // stores in the context. Reaching this point means that step
                // did not run or did not succeed, i.e. a step-ordering bug.
                throw new InvalidOperationException(
                    "Internal error: the add-packages step ran before the Syncfusion Blazor Toolkit validation step " +
                    "stored its project settings. The validation step must run first and succeed. " +
                    "Aborting the add-packages step.");
            }
        });
    }

    /// <summary>
    /// Adds a step that installs the Syncfusion.Blazor.Toolkit package on
    /// the resolved Blazor WebAssembly client project (when one exists).
    /// Self-skips when no client was detected.
    /// </summary>
    public static IScaffoldBuilder WithSyncfusionBlazorToolkitAddClientPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            if (!context.Properties.TryGetValue(nameof(SyncfusionBlazorToolkitSettings), out var settingsObj) ||
                settingsObj is not SyncfusionBlazorToolkitSettings settings ||
                string.IsNullOrEmpty(settings.ClientProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            step.ProjectPath = settings.ClientProjectPath;
            step.Packages = new List<Package>
            {
                PackageConstants.AspNetCorePackages.SyncfusionBlazorToolkitPackage
            };
        });
    }

    /// <summary>
    /// Adds a Program.cs service-registration step for the server
    /// project. The step is self-contained: it discovers (or creates)
    /// <c>Program.cs</c>, locates the configured builder variable, and
    /// idempotently appends
    /// <c>builder.Services.AddSyncfusionBlazorToolkit();</c> together
    /// with the <c>using Syncfusion.Blazor.Toolkit;</c> directive.
    /// </summary>
    public static IScaffoldBuilder WithSyncfusionBlazorToolkitProgramCsStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<ApplySyncfusionBlazorToolkitProgramCsStep>();
    }

    /// <summary>
    /// Adds a Program.cs service-registration step for the resolved
    /// Blazor WebAssembly client. Self-skips when no client was detected.
    /// </summary>
    public static IScaffoldBuilder WithSyncfusionBlazorToolkitClientProgramCsStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<ApplySyncfusionBlazorToolkitProgramCsStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            if (!context.Properties.TryGetValue(nameof(SyncfusionBlazorToolkitSettings), out var settingsObj) ||
                settingsObj is not SyncfusionBlazorToolkitSettings settings ||
                string.IsNullOrEmpty(settings.ClientProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            // Target the resolved client so the helper runs against the
            // client project's Program.cs instead of the server's. The
            // step will not overwrite the server-side settings on its
            // shared state.
            step.TargetProjectPath = settings.ClientProjectPath;
        });
    }

    /// <summary>
    /// Dedicated imports step that writes
    /// <c>@using Syncfusion.Blazor.Toolkit</c> into the server project's
    /// <c>_Imports.razor</c> on disk. Idempotent. Creates the file in
    /// the conventional location when none exists.
    /// </summary>
    public static IScaffoldBuilder WithSyncfusionBlazorToolkitImportsStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<ApplySyncfusionBlazorToolkitImportsStep>();
    }

    /// <summary>
    /// Dedicated imports step for the resolved Blazor WebAssembly
    /// client. Self-skips when no client was detected.
    /// </summary>
    public static IScaffoldBuilder WithSyncfusionBlazorToolkitImportsClientStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<ApplySyncfusionBlazorToolkitImportsStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            if (!context.Properties.TryGetValue(nameof(SyncfusionBlazorToolkitSettings), out var settingsObj) ||
                settingsObj is not SyncfusionBlazorToolkitSettings settings ||
                string.IsNullOrEmpty(settings.ClientProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            // Target the resolved client so the helper runs against
            // the client project's _Imports.razor instead of the
            // server's. The imports step will not overwrite the
            // server-side settings on its shared state.
            step.TargetProjectPath = settings.ClientProjectPath;
        });
    }

}