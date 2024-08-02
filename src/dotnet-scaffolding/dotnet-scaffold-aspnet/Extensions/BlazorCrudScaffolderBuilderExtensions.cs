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

internal static class BlazorCrudScaffolderBuilderExtensions
{
    //TODO : fix this extension method to not do extra work every 'preExecute'
    public static IScaffoldBuilder WithBlazorCrudTextTemplatingStep(this IScaffoldBuilder builder)
    {
        var allT4TemplatePaths = new TemplateFoldersUtilities().GetAllT4Templates(["BlazorCrud"]);
        foreach (var templatePath in allT4TemplatePaths)
        {
            var templateName = Path.GetFileNameWithoutExtension(templatePath);
            var templateType = BlazorCrudHelper.GetTemplateType(templatePath);
            if (!string.IsNullOrEmpty(templatePath) && templateType is not null)
            {
                builder = builder.WithStep<TextTemplatingStep>(config =>
                {
                    var context = config.Context;
                    context.Properties.TryGetValue(nameof(BlazorCrudModel), out var blazorCrudModelObj);
                    BlazorCrudModel blazorCrudModel = blazorCrudModelObj as BlazorCrudModel ??
                        throw new InvalidOperationException("missing 'BlazorCrudModel' in 'ScaffolderContext.Properties'");

                    if (!BlazorCrudHelper.IsValidTemplate(blazorCrudModel.PageType, templateName))
                    {
                        config.Step.SkipStep = true;
                        return;
                    }

                    string baseOutputPath = BlazorCrudHelper.GetBaseOutputPath(
                        blazorCrudModel.ModelInfo.ModelTypeName,
                        blazorCrudModel.ProjectInfo.ProjectPath);
                    string outputFileName = Path.Combine(baseOutputPath, $"{templateName}{Tools.Scaffold.AspNet.Common.Constants.BlazorExtension}");

                    var step = config.Step;
                    step.TemplatePath = templatePath;
                    step.TemplateType = templateType;
                    step.TemplateModelName = "Model";
                    step.TemplateModel = blazorCrudModel;
                    step.OutputPath = outputFileName;
                });
            }
        }

        return builder;
    }

    public static IScaffoldBuilder WithBlazorCrudAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            var packageList = new List<string>()
            {
                PackageConstants.EfConstants.EfToolsPackageName,
                PackageConstants.AspNetCorePackages.QuickGridEfAdapterPackageName,
                PackageConstants.AspNetCorePackages.AspNetCoreDiagnosticsEfCorePackageName
            };

            if (context.Properties.TryGetValue(nameof(BlazorCrudSettings), out var commandSettingsObj) &&
                commandSettingsObj is BlazorCrudSettings commandSettings)
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

    public static IScaffoldBuilder WithBlazorCrudCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<CodeModificationStep>(config =>
        {
            var step = config.Step;
            var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("blazorWebCrudChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(BlazorCrudSettings), out var blazorCrudSettingsObj);
            config.Context.Properties.TryGetValue(nameof(BlazorCrudModel), out var blazorCrudModelObj);
            config.Context.Properties.TryGetValue("CodeModifierProperties", out var codeModifierPropertiesObj);
            var blazorCrudSettings = blazorCrudSettingsObj as BlazorCrudSettings;
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var blazorCrudModel = blazorCrudModelObj as BlazorCrudModel;

            //initialize CodeModificationStep's properties
            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                blazorCrudSettings is not null &&
                codeModifierProperties is not null &&
                blazorCrudModel is not null)
            {
                step.CodeModifierConfigPath = codeModificationFilePath;
                step.CodeModifierProperties = codeModifierProperties;
                step.ProjectPath = blazorCrudSettings.Project;
                step.CodeChangeOptions = blazorCrudModel.ProjectInfo.CodeChangeOptions ?? [];
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        //blazor-crud scenario has some custom Program.cs additions that get decided after some analysis.
        //adding these changes with a programmatically created config
        builder = builder.WithStep<CodeModificationStep>(config =>
        {
            var step = config.Step;
            config.Context.Properties.TryGetValue(nameof(BlazorCrudSettings), out var blazorCrudSettingsObj);
            config.Context.Properties.TryGetValue("AdditionalCodeModifier", out var blazorCodeModifierStringObj);
            config.Context.Properties.TryGetValue(nameof(BlazorCrudModel), out var blazorCrudModelObj);
            var blazorCrudSettings = blazorCrudSettingsObj as BlazorCrudSettings;
            var blazorCrudModel = blazorCrudModelObj as BlazorCrudModel;
            var blazorCodeModifierString = blazorCodeModifierStringObj as string;
            if (blazorCrudSettings is not null &&
                blazorCrudModel is not null &&
                !string.IsNullOrEmpty(blazorCodeModifierString))
            {
                step.CodeModifierConfigJsonText = blazorCodeModifierString;
                step.CodeModifierProperties = new Dictionary<string, string>();
                step.ProjectPath = blazorCrudSettings.Project;
                step.CodeChangeOptions = blazorCrudModel.ProjectInfo.CodeChangeOptions ?? [];
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }
}
