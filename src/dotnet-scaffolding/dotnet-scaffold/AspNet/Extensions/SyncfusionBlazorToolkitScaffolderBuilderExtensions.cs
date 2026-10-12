// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Syncfusion Blazor
/// Toolkit setup scaffolding steps. The setup scaffolder only adds the NuGet package and
/// applies a small set of code changes (service registration via the shared JSON/Roslyn
/// pipeline and a dedicated imports step); it does not generate any Razor pages or
/// inject a host stylesheet.
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
///   <item><description>Apply the shared code-modification recipe to Program.cs.</description></item>
///   <item><description>Apply the same recipe to the resolved client program (if any).</description></item>
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
                step.Prerelease = settings.Prerelease;
                step.Packages = packages;
            }
            else
            {
                throw new InvalidOperationException(
                    "Syncfusion Blazor Toolkit scaffolder requires 'SyncfusionBlazorToolkitSettings' to be present " +
                    "in 'ScaffolderContext.Properties' before the add-packages step can run. " +
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
            step.Prerelease = settings.Prerelease;
            step.Packages = new List<Package>
            {
                PackageConstants.AspNetCorePackages.SyncfusionBlazorToolkitPackage
            };
        });
    }

    /// <summary>
    /// Adds a code-modification step driven by
    /// <c>syncfusionBlazorToolkitChanges.json</c> that registers
    /// <c>AddSyncfusionBlazorToolkit()</c> on the server project via the
    /// shared JSON/Roslyn pipeline. The recipe targets
    /// <c>WebApplication.CreateBuilder.Services</c> /
    /// <c>WebAssemblyHostBuilder.CreateDefault.Services</c> via
    /// <c>MemberAccess</c>, so it is robust against differently-named
    /// <c>builder</c> variables and multi-line <c>CreateBuilder()</c>
    /// calls — and treats a commented-out registration as not yet
    /// present.
    /// </summary>
    public static IScaffoldBuilder WithSyncfusionBlazorToolkitCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            if (!context.Properties.TryGetValue(nameof(SyncfusionBlazorToolkitSettings), out var settingsObj) ||
                settingsObj is not SyncfusionBlazorToolkitSettings settings ||
                string.IsNullOrEmpty(settings.Project))
            {
                step.SkipStep = true;
                return;
            }

            string codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile(
                "syncfusionBlazorToolkitChanges.json",
                System.Reflection.Assembly.GetExecutingAssembly(),
                TargetFrameworkHelpers.GetTargetFrameworkFolder(settings.Project));

            if (string.IsNullOrEmpty(codeModificationFilePath))
            {
                throw new InvalidOperationException(
                    "Syncfusion Blazor Toolkit code-modification config " +
                    "'syncfusionBlazorToolkitChanges.json' was not found for the selected target framework. " +
                    "Aborting the code-modification step to avoid writing an invalid configuration to the project.");
            }

            // We always send the full JSON config to the shared code-modification
            // step and filter via CodeChangeOptions: 'WebApp' is added when
            // WebApplication.CreateBuilder is detected (Blazor Web App / Server)
            // and 'StandaloneWasm' is added when WebAssemblyHostBuilder.CreateDefault
            // is detected (standalone Blazor WebAssembly). The recipe's
            // per-snippet 'Options' array filters the right registration block.
            string? resolvedJson = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
                codeModificationFilePath);
            if (string.IsNullOrEmpty(resolvedJson))
            {
                throw new InvalidOperationException(
                    "Syncfusion Blazor Toolkit code-modification JSON could not be resolved. " +
                    "Verify the embedded 'syncfusionBlazorToolkitChanges.json' is present and valid.");
            }

            step.ProjectPath = settings.Project;
            step.CodeModifierConfigJsonText = resolvedJson;
            step.CodeChangeOptions = DetectProjectOptions(settings.Project);
        });
    }

    /// <summary>
    /// Adds a code-modification step for the resolved Blazor WebAssembly
    /// client. Self-skips when no client was detected.
    /// </summary>
    public static IScaffoldBuilder WithSyncfusionBlazorToolkitClientCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedCodeModificationStep>(config =>
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

            string codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile(
                "syncfusionBlazorToolkitChanges.json",
                System.Reflection.Assembly.GetExecutingAssembly(),
                TargetFrameworkHelpers.GetTargetFrameworkFolder(settings.ClientProjectPath));

            if (string.IsNullOrEmpty(codeModificationFilePath))
            {
                step.SkipStep = true;
                return;
            }

            string? resolvedJson = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
                codeModificationFilePath);
            if (string.IsNullOrEmpty(resolvedJson))
            {
                step.SkipStep = true;
                return;
            }

            step.ProjectPath = settings.ClientProjectPath;
            step.CodeModifierConfigJsonText = resolvedJson;
            // Standalone Blazor WebAssembly projects use
            // WebAssemblyHostBuilder.CreateDefault.
            step.CodeChangeOptions = new List<string> { "StandaloneWasm" };
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

    /// <summary>
    /// Inspects Program.cs of the supplied project and returns the
    /// <c>CodeChangeOptions</c> that enable the matching code-modification
    /// snippet. <c>WebApp</c> is added when a Blazor Web App / Server
    /// host (WebApplication.CreateBuilder) is detected; <c>StandaloneWasm</c>
    /// is added when a Blazor WebAssembly host (WebAssemblyHostBuilder.CreateDefault)
    /// is detected.
    /// </summary>
    private static List<string> DetectProjectOptions(string projectPath)
    {
        var options = new List<string>();
        try
        {
            string programPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "Program.cs");
            if (File.Exists(programPath))
            {
                string text = File.ReadAllText(programPath);
                if (text.Contains("WebApplication.CreateBuilder", StringComparison.Ordinal))
                {
                    options.Add("WebApp");
                }

                if (text.Contains("WebAssemblyHostBuilder.CreateDefault", StringComparison.Ordinal))
                {
                    options.Add("StandaloneWasm");
                }
            }
        }
        catch
        {
            // best-effort; if we cannot read Program.cs we let the
            // shared step fail with its own diagnostics.
        }

        return options;
    }
}