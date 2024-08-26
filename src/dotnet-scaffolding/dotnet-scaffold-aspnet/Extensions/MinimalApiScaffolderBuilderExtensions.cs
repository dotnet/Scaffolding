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
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

internal static class MinimalApiScaffolderBuilderExtensions
{

    public static IScaffoldBuilder WithMinimalApiCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<CodeModificationStep>(config =>
        {
            var step = config.Step;
            var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("minimalApiChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(MinimalApiSettings), out var minimalApiSettingsObj);
            config.Context.Properties.TryGetValue(nameof(MinimalApiModel), out var minimalApiModelObj);
            config.Context.Properties.TryGetValue(Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
            var minimalApiSettings = minimalApiSettingsObj as MinimalApiSettings;
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var minimalApiModel = minimalApiModelObj as MinimalApiModel;

            //initialize CodeModificationStep's properties
            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                minimalApiSettings is not null &&
                codeModifierProperties is not null &&
                minimalApiModel is not null)
            {
                step.CodeModifierConfigPath = codeModificationFilePath;
                foreach (var kvp in codeModifierProperties)
                {
                    step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                }

                step.ProjectPath = minimalApiSettings.Project;
                step.CodeChangeOptions = minimalApiModel.ProjectInfo.CodeChangeOptions ?? [];
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
            List<string> packageList = [];
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

                if (commandSettings.OpenApi)
                {
                    packageList.Add(PackageConstants.AspNetCorePackages.OpenApiPackageName);
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
            step.TextTemplatingProperties = [MinimalApiHelper.GetMinimalApiTemplatingProperty(minimalApiModel)];
            step.DisplayName = "Minimal API controller";
        });
    }
}
