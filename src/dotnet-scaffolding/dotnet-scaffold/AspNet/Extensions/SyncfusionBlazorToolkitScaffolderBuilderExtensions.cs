// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

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
    /// Adds a single code-modification step driven by syncfusionBlazorToolkitChanges.json.
    /// </summary>
    public static IScaffoldBuilder WithSyncfusionBlazorToolkitCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            context.Properties.TryGetValue(nameof(SyncfusionBlazorToolkitSettings), out var settingsObj);
            var settings = settingsObj as SyncfusionBlazorToolkitSettings;
            string targetFrameworkFolder = TargetFrameworkHelpers.GetTargetFrameworkFolder(settings?.Project);
            string? codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile(
                "syncfusionBlazorToolkitChanges.json",
                System.Reflection.Assembly.GetExecutingAssembly(),
                targetFrameworkFolder);

            context.Properties.TryGetValue(Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;

            if (string.IsNullOrEmpty(codeModificationFilePath) ||
                settings is null ||
                codeModifierProperties is null)
            {
                var missing = new System.Text.StringBuilder();
                if (string.IsNullOrEmpty(codeModificationFilePath))
                {
                    missing.Append("'syncfusionBlazorToolkitChanges.json' code-modification config path (could not resolve via GlobalToolFileFinder for target framework folder '")
                        .Append(targetFrameworkFolder)
                        .Append("'); ");
                }
                if (settings is null)
                {
                    missing.Append("'SyncfusionBlazorToolkitSettings'; ");
                }
                if (codeModifierProperties is null)
                {
                    missing.Append("CodeModifierProperties (Constants.StepConstants.CodeModifierProperties entry); ");
                }

                throw new InvalidOperationException(
                    "Syncfusion Blazor Toolkit code-modification step is missing required context: "
                    + missing.ToString().TrimEnd(' ', ';')
                    + ". Aborting the code-modification step.");
            }

            step.CodeModifierConfigPath = codeModificationFilePath;
            foreach (var kvp in codeModifierProperties)
            {
                step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
            }

            step.ProjectPath = settings.Project;
            step.CodeChangeOptions = [];
        });
    }
}
