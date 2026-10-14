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
/// Steps of the Syncfusion Blazor Toolkit scaffolder, in the order they run:
/// <list type="number">
///   <item><description>Validate the project and record its starting state.</description></item>
///   <item><description>Add the NuGet package to the server project, and to the referenced WebAssembly client when there is one.</description></item>
///   <item><description>Register <c>AddSyncfusionBlazorToolkit()</c> in the server entry point, and in the client's when there is one.</description></item>
///   <item><description>Add <c>@using Syncfusion.Blazor.Toolkit</c> to the server's <c>_Imports.razor</c>, and to the client's.</description></item>
///   <item><description>Verify the resulting project state.</description></item>
///   <item><description>Summarize what changed.</description></item>
/// </list>
/// Nothing else is generated: no pages, sample UI, or stylesheet link (2.0.0+ ships its styles with the assembly).
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