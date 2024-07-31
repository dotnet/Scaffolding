// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

internal static class MinimalApiScaffolderBuilderExtensions
{

    public static IScaffoldBuilder WithMinimalApiCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<CodeModificationStep>(config =>
        {
            var step = config.Step;
            CodeModifierConfig? codeModifierConfig = CodeModifierConfigHelper.GetCodeModifierConfig("minimalApiChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(MinimalApiSettings), out var minimalApiSettingsObj);
            config.Context.Properties.TryGetValue(nameof(MinimalApiModel), out var minimalApiModelObj);
            config.Context.Properties.TryGetValue("CodeModifierProperties", out var codeModifierPropertiesObj);
            var minimalApiSettings = minimalApiSettingsObj as MinimalApiSettings;
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var minimalApiModel = minimalApiModelObj as MinimalApiModel;

            //initialize CodeModificationStep's properties
            if (codeModifierConfig is not null &&
                minimalApiSettings is not null &&
                codeModifierProperties is not null &&
                minimalApiModel is not null)
            {
                step.CodeModifierConfig = codeModifierConfig;
                step.CodeModifierProperties = codeModifierProperties;
                step.ProjectPath = minimalApiSettings.Project;
                step.CodeChangeOptions = minimalApiModel.ProjectInfo.CodeChangeOptions ?? new CodeChangeOptions();
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    public static IScaffoldBuilder WithMinimalApiAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            //add Microsoft.EntityFrameworkCore.Tools package regardless of the DatabaseProvider
            var packageList = new List<string>()
            {
                PackageConstants.EfConstants.EfToolsPackageName
            };

            if (context.Properties.TryGetValue(nameof(MinimalApiSettings), out var commandSettingsObj) &&
                commandSettingsObj is MinimalApiSettings commandSettings)
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

    public static IScaffoldBuilder WithMinimalApiTextTemplatingStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<TextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            MinimalApiModel? minimalApiModel = null;
            if (context.Properties.TryGetValue(nameof(MinimalApiModel), out var minimalApiModelObj) &&
                minimalApiModelObj is MinimalApiModel)
            {
                minimalApiModel = minimalApiModelObj as MinimalApiModel;
            }

            ArgumentNullException.ThrowIfNull(minimalApiModel);
            var allT4Templates = new TemplateFoldersUtilities().GetAllT4Templates(["MinimalApi"]);
            string? t4TemplatePath = null;
            if (minimalApiModel.DbContextInfo.EfScenario)
            {
                t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("MinimalApiEf.tt", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("MinimalApi.tt", StringComparison.OrdinalIgnoreCase));
            }

            var templateType = MinimalApiHelper.GetMinimalApiTemplateType(t4TemplatePath);

            if (string.IsNullOrEmpty(t4TemplatePath) ||
                string.IsNullOrEmpty(minimalApiModel.EndpointsPath) ||
                templateType is null)
            {
                throw new InvalidOperationException("could not get minimal api template");
            }

            step.TemplatePath = t4TemplatePath;
            step.TemplateType = templateType;
            step.TemplateModel = minimalApiModel;
            step.TemplateModelName = "Model";
            step.OutputPath = minimalApiModel.EndpointsPath;
        });
    }
}
