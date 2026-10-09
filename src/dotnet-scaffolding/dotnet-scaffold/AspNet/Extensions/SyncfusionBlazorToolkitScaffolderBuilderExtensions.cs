// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Syncfusion Blazor
/// Toolkit setup scaffolding steps. The setup scaffolder only adds the NuGet package and
/// applies a small set of code changes (service registration, using directives, theme
/// stylesheet); it does not generate any Razor pages.
///
/// <para><b>Syncfusion.Blazor.Toolkit 2.0.0+ behavior:</b> Styles ship with the
/// assembly and are activated through <c>AddSyncfusionBlazorToolkit()</c>, so no
/// external <c>&lt;link&gt;</c> tag is injected into <c>App.razor</c> or
/// <c>wwwroot/index.html</c>. Components expose a single namespace
/// (<c>@using Syncfusion.Blazor.Toolkit</c>), so only one using directive is
/// added regardless of which components the project uses.</para>
/// </summary>
internal static class SyncfusionBlazorToolkitScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a step that installs the Syncfusion.Blazor.Toolkit package.
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
                settingsObj is SyncfusionBlazorToolkitSettings settings)
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
    /// Adds a code-modification step driven by syncfusionBlazorToolkitChanges.json.
    /// The Components/_Imports.razor anchor entry is rewritten to a concrete,
    /// anchor-free Block-only entry when an _Imports.razor file was discovered, or
    /// removed when none was found.
    ///
    /// <para>With Syncfusion.Blazor.Toolkit 2.0.0+ the theme parameter is forced to
    /// <c>null</c> so no external stylesheet is injected into
    /// <c>App.razor</c> or <c>wwwroot/index.html</c>. The theme detection result is
    /// still surfaced via the <c>ThemeFile</c> setting so the summary step can
    /// describe the host layout. The summary step explains the no-stylesheet
    /// behavior and that styles are activated through
    /// <c>AddSyncfusionBlazorToolkit()</c>.</para>
    /// </summary>
    public static IScaffoldBuilder WithSyncfusionBlazorToolkitCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            context.Properties.TryGetValue(nameof(SyncfusionBlazorToolkitSettings), out var settingsObj);
            var settings = settingsObj as SyncfusionBlazorToolkitSettings;

            string? codeModificationFilePath = null;
            if (settings is not null)
            {
                string targetFrameworkFolder = TargetFrameworkHelpers.GetTargetFrameworkFolder(settings.Project);
                codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile(
                    "syncfusionBlazorToolkitChanges.json",
                    System.Reflection.Assembly.GetExecutingAssembly(),
                    targetFrameworkFolder);
            }

            if (string.IsNullOrEmpty(codeModificationFilePath) || settings is null)
            {
                var missing = new System.Text.StringBuilder();
                if (string.IsNullOrEmpty(codeModificationFilePath))
                {
                    missing.Append("'syncfusionBlazorToolkitChanges.json' code-modification config path; ");
                }
                if (settings is null)
                {
                    missing.Append("'SyncfusionBlazorToolkitSettings'; ");
                }

                throw new InvalidOperationException(
                    "Syncfusion Blazor Toolkit code-modification step is missing required context: "
                    + missing.ToString().TrimEnd(' ', ';')
                    + ". Aborting the code-modification step.");
            }

            // Detect a multi-project (Blazor Web App Auto) layout where a
            // .Client sibling project exists alongside the current project.
            // In that case, the using directive and static assets usually
            // live on the .Client project, while service registration
            // belongs on the project that owns Program.cs
            // (WebApplication.CreateBuilder). The summary step picks this
            // up to print clear multi-project guidance.
            DetectMultiProjectLayout(settings, context);

            // Resolve the JSON in memory. With Syncfusion.Blazor.Toolkit
            // 2.0.0+ we pass themeFile: null so the helper never emits an
            // external stylesheet entry; styles are bundled with the
            // package and activated through AddSyncfusionBlazorToolkit().
            //
            // The Components/_Imports.razor anchor entry (if present in
            // the source JSON) is replaced with a concrete, anchor-free
            // Block-only entry for the discovered _Imports.razor path,
            // or dropped when no _Imports.razor was found.
            //
            // We pass a NullLogger to the helper because the
            // configuration lambda does not have access to the DI
            // container. The downstream WrappedCodeModificationStep has
            // its own logger and will surface any errors it encounters.
            string? resolvedJson = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
                codeModificationFilePath!,
                themeFile: null, // 2.0.0+: no external stylesheet needed.
                importsFile: settings.ImportsFile,
                logger: null);

            if (!string.IsNullOrEmpty(resolvedJson))
            {
                step.CodeModifierConfigJsonText = resolvedJson;
            }
            else
            {
                // Defensive: never pass the unresolved $(ThemeFile)
                // placeholder configuration to the downstream step. If
                // the helper returned null, the source JSON is missing
                // or invalid; fail the step with a clear, actionable
                // message instead of silently corrupting the project.
                step.SkipStep = true;
                throw new InvalidOperationException(
                    "Syncfusion Blazor Toolkit code-modification JSON could not be resolved. " +
                    "Aborting the code-modification step to avoid writing an invalid configuration to the project. " +
                    "Verify the embedded 'syncfusionBlazorToolkitChanges.json' is present and valid.");
            }

            step.ProjectPath = settings.Project;
            step.CodeChangeOptions = [];
        });
    }

    /// <summary>
    /// Detects a Blazor Web App Auto / Interactive WebAssembly multi-project
    /// layout where the current project has a sibling ".Client" project
    /// (the canonical Web App template places interactive components on
    /// MyApp.Client while Program.cs / DI live on MyApp).
    ///
    /// <para>The detection is best-effort: when a sibling .Client project
    /// is found, the layout kind and project paths are stashed on the
    /// settings and as context properties so the summary step can print
    /// targeted guidance ("run on Server for services, run on Client for
    /// package/usings if components live there").</para>
    /// </summary>
    private static void DetectMultiProjectLayout(
        SyncfusionBlazorToolkitSettings settings,
        ScaffolderContext context)
    {
        try
        {
            var projectDir = Path.GetDirectoryName(settings.Project);
            if (string.IsNullOrEmpty(projectDir) || !Directory.Exists(projectDir))
            {
                return;
            }

            // The project name is the .csproj file name without the
            // extension. The sibling client project follows the
            // conventional "<name>.Client.csproj" naming used by
            // dotnet new blazor --interactivity Auto --use-program-main
            // (and similar templates).
            string projectName = Path.GetFileNameWithoutExtension(settings.Project);
            string clientCsproj = Path.Combine(projectDir, $"{projectName}.Client.csproj");
            if (!File.Exists(clientCsproj))
            {
                // No .Client sibling; this is a single-project layout.
                settings.Layout = SyncfusionBlazorToolkitLayout.SingleProject;
                return;
            }

            // A .Client sibling exists. Inspect the current project's
            // SDK to figure out which side of the Auto split we are on.
            string currentText = File.ReadAllText(settings.Project);
            string clientText = File.ReadAllText(clientCsproj);

            bool currentIsWasm = currentText.Contains(
                "Microsoft.NET.Sdk.BlazorWebAssembly",
                StringComparison.OrdinalIgnoreCase);
            bool clientIsWasm = clientText.Contains(
                "Microsoft.NET.Sdk.BlazorWebAssembly",
                StringComparison.OrdinalIgnoreCase);

            if (clientIsWasm && !currentIsWasm)
            {
                // Server (Web) project with a .Client WASM sibling:
                // Program.cs lives here, components live in .Client.
                settings.Layout = SyncfusionBlazorToolkitLayout.AutoServer;
                settings.SiblingClientProject = clientCsproj;
            }
            else if (currentIsWasm && !clientIsWasm)
            {
                // Running on the .Client project: package + @using go here.
                settings.Layout = SyncfusionBlazorToolkitLayout.AutoClient;
            }
            else
            {
                settings.Layout = SyncfusionBlazorToolkitLayout.SingleProject;
            }
        }
        catch
        {
            // best-effort detection; never fail the scaffolder because of
            // it.
            settings.Layout = SyncfusionBlazorToolkitLayout.SingleProject;
        }
    }
}
