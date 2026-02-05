// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Razor Pages scaffolding steps.
/// </summary>
internal static class RazorPagesScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a code change step for Razor Pages to the scaffold builder.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    public static IScaffoldBuilder WithRazorPagesCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("razorPagesChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(CrudSettings), out var crudSettingsObj);
            config.Context.Properties.TryGetValue(nameof(RazorPageModel), out var razorPageModelObj);
            config.Context.Properties.TryGetValue(Internal.Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
            var crudSettings = crudSettingsObj as CrudSettings;
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var razorPageModel = razorPageModelObj as RazorPageModel;

            //initialize CodeModificationStep's properties
            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                crudSettings is not null &&
                codeModifierProperties is not null &&
                razorPageModel is not null)
            {
                step.CodeModifierConfigPath = codeModificationFilePath;
                foreach (var kvp in codeModifierProperties)
                {
                    step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                }

                step.ProjectPath = crudSettings.Project;
                step.CodeChangeOptions = razorPageModel.ProjectInfo.CodeChangeOptions ?? [];
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    /// <summary>
    /// Adds a text templating step for Razor Pages to the scaffold builder.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    public static IScaffoldBuilder WithRazorPagesTextTemplatingStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedTextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(RazorPageModel), out var razorPagesModelObj);
            RazorPageModel razorPagesModel = razorPagesModelObj as RazorPageModel ??
                throw new InvalidOperationException("missing 'RazorPageModel' in 'ScaffolderContext.Properties'");

            if (razorPagesModel.ProjectInfo is null || string.IsNullOrEmpty(razorPagesModel.ProjectInfo.ProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            var allT4TemplatePaths = new TemplateFoldersUtilities().GetAllT4TemplatesForTargetFramework(["RazorPages"], razorPagesModel.ProjectInfo.ProjectPath);
            var razorPageTemplateProperties = RazorPagesHelper.GetTextTemplatingProperties(allT4TemplatePaths, razorPagesModel);
            if (razorPageTemplateProperties.Any())
            {
                step.TextTemplatingProperties = razorPageTemplateProperties;
                step.DisplayName = "Razor page files (.cshtml and .cshtml.cs)";
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

    /// <summary>
    /// Adds a step to add packages for Razor Pages to the scaffold builder.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    public static IScaffoldBuilder WithRazorPagesAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            List<Package> packages = [PackageConstants.EfConstants.EfCoreToolsPackage];
            if (context.Properties.TryGetValue(nameof(CrudSettings), out var commandSettingsObj) &&
                commandSettingsObj is CrudSettings commandSettings)
            {
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
                if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
                    PackageConstants.EfConstants.EfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out Package? projectPackage))
                {
                    packages.Add(projectPackage);
                }

                step.Packages = packages;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }
}
