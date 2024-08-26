// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

internal static class RazorPagesScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithRazorPagesCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<CodeModificationStep>(config =>
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

    public static IScaffoldBuilder WithRazorPagesTextTemplatingStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<TextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(RazorPageModel), out var razorPagesModelObj);
            RazorPageModel razorPagesModel = razorPagesModelObj as RazorPageModel ??
                throw new InvalidOperationException("missing 'RazorPageModel' in 'ScaffolderContext.Properties'");

            var allT4TemplatePaths = new TemplateFoldersUtilities().GetAllT4Templates(["RazorPages"]);
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

    public static IScaffoldBuilder WithRazorPagesAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            List<string> packageList = [];
            if (context.Properties.TryGetValue(nameof(CrudSettings), out var commandSettingsObj) &&
                commandSettingsObj is CrudSettings commandSettings)
            {
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
                if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
                    PackageConstants.EfConstants.EfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out string? projectPackageName))
                {
                    packageList.Add(projectPackageName);
                }

                step.PackageNames = packageList;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }
}
