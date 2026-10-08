// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Syncfusion Blazor
/// Toolkit setup scaffolding steps. The setup scaffolder only adds the NuGet package and
/// applies a small set of code changes (service registration, using directives, theme
/// stylesheet); it does not generate any Razor pages.
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
    /// The theme <c>FileName</c> entry in the JSON is rewritten at runtime to
    /// match the host file resolved by <c>ResolveSyncfusionBlazorToolkitThemeStep</c>
    /// (Components/App.razor for Blazor Web App, wwwroot/index.html for
    /// standalone Blazor WASM), or removed when no host file was found.
    /// The Components/_Imports.razor anchor entry is rewritten to a
    /// concrete, anchor-free Block-only entry when an _Imports.razor file
    /// was discovered, or removed when none was found.
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

            // Resolve the JSON in memory so the theme file can be set per project
            // (Components/App.razor vs wwwroot/index.html) or dropped when no
            // host file is available, and so the Components/_Imports.razor
            // anchor entry can be replaced with a discovered path or dropped
            // when no _Imports.razor was found. CodeModifierConfigJsonText
            // takes priority over CodeModifierConfigPath in
            // CodeModificationStep.
            //
            // We pass a NullLogger to the helper because the configuration
            // lambda does not have access to the DI container. The
            // downstream WrappedCodeModificationStep has its own logger
            // and will surface any errors it encounters.
            string? resolvedJson = SyncfusionBlazorToolkitHelper.BuildResolvedCodeModifierConfigJson(
                codeModificationFilePath!,
                settings.ThemeFile,
                settings.ImportsFile,
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
                    "Aborting the code-modification step to avoid writing the unresolved '$(ThemeFile)' placeholder to the project. " +
                    "Verify the embedded 'syncfusionBlazorToolkitChanges.json' is present and valid.");
            }

            step.ProjectPath = settings.Project;
            step.CodeChangeOptions = [];
        });
    }
}
