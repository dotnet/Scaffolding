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
/// applies a small set of code changes (service registration and using directives);
/// it does not generate any Razor pages or inject a host stylesheet.
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
    /// Adds a code-modification step driven by syncfusionBlazorToolkitChanges.json
    /// (Program.cs only) and writes <c>@using Syncfusion.Blazor.Toolkit</c> into
    /// the discovered <c>_Imports.razor</c>. No host stylesheet is added.
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

            // Program.cs only. _Imports.razor is edited on disk by this
            // step so the using is applied even when the razor file is
            // not in the Roslyn workspace (Components folder or project root).
            string? resolvedJson = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
                codeModificationFilePath!,
                logger: null);
            // Registration is applied on disk so it is not dropped when the
            // shared code-modification workspace does not load Program.cs.
            // The JSON config remains the idempotent check for the shared step.
            SyncfusionBlazorToolkitHelper.EnsureServiceRegistration(settings.Project);
            SyncfusionBlazorToolkitHelper.EnsureImportsUsing(settings.Project, settings.ImportsFile);

            if (!string.IsNullOrEmpty(resolvedJson))
            {
                step.CodeModifierConfigJsonText = resolvedJson;
            }
            else
            {
                // If the helper returned null, the source JSON is missing
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
