// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Healthcare Tracker scaffolding steps.
/// </summary>
internal static class HealthcareTrackerScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a text-templating step that generates the Healthcare Tracker Blazor page.
    /// </summary>
    public static IScaffoldBuilder WithHealthcareTrackerTextTemplatingStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedTextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(HealthcareTrackerModel), out var modelObj);
            HealthcareTrackerModel model = modelObj as HealthcareTrackerModel ??
                throw new InvalidOperationException("missing 'HealthcareTrackerModel' in 'ScaffolderContext.Properties'");

            if (model.ProjectInfo is null || string.IsNullOrEmpty(model.ProjectInfo.ProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            var allT4TemplatePaths = new TemplateFoldersUtilities()
                .GetAllT4TemplatesForTargetFramework(["HealthcareTracker"], model.ProjectInfo.ProjectPath);
            var templateProperties = HealthcareTrackerHelper.GetTextTemplatingProperties(allT4TemplatePaths, model);
            if (templateProperties.Any())
            {
                step.TextTemplatingProperties = templateProperties;
                step.DisplayName = "Healthcare Tracker page (.razor)";
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

    /// <summary>
    /// Adds a step that installs Syncfusion.Blazor.Toolkit.
    /// </summary>
    public static IScaffoldBuilder WithHealthcareTrackerAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            var packages = new List<Package>
            {
                PackageConstants.AspNetCorePackages.SyncfusionBlazorToolkitPackage
            };

            if (context.Properties.TryGetValue(nameof(HealthcareTrackerSettings), out var settingsObj) &&
                settingsObj is HealthcareTrackerSettings settings)
            {
                step.ProjectPath = settings.Project;
                step.Prerelease = settings.Prerelease;
                step.Packages = packages;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

    /// <summary>
    /// Adds a single code-modification step driven by syncfusionHealthcareTrackerChanges.json.
    /// </summary>
    public static IScaffoldBuilder WithHealthcareTrackerCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            config.Context.Properties.TryGetValue(nameof(HealthcareTrackerSettings), out var settingsObj);
            var settings = settingsObj as HealthcareTrackerSettings;
            string targetFrameworkFolder = TargetFrameworkHelpers.GetTargetFrameworkFolder(settings?.Project);
            string? codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile(
                "syncfusionHealthcareTrackerChanges.json",
                System.Reflection.Assembly.GetExecutingAssembly(),
                targetFrameworkFolder);

            config.Context.Properties.TryGetValue(nameof(HealthcareTrackerModel), out var modelObj);
            config.Context.Properties.TryGetValue(Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var model = modelObj as HealthcareTrackerModel;

            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                settings is not null &&
                codeModifierProperties is not null &&
                model is not null)
            {
                step.CodeModifierConfigPath = codeModificationFilePath;
                foreach (var kvp in codeModifierProperties)
                {
                    step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                }

                step.ProjectPath = settings.Project;
                step.CodeChangeOptions = model.ProjectInfo.CodeChangeOptions ?? [];
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }
}
