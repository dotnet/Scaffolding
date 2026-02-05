// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Minimal API scaffolding steps.
/// </summary>
internal static class MinimalApiScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a code change step for the Minimal API to the scaffold builder.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The modified scaffold builder.</returns>
    public static IScaffoldBuilder WithMinimalApiCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(MinimalApiSettings), out var minimalApiSettingsObj);
            var minimalApiSettings = minimalApiSettingsObj as MinimalApiSettings;
            string targetFrameworkFolder = "net11.0"; //TODO invoke TargetFrameworkHelpers.GetTargetFrameworkFolder(minimalApiSettings?.Project); when other tfm supported
            string? codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("minimalApiChanges.json", System.Reflection.Assembly.GetExecutingAssembly(), targetFrameworkFolder);
            config.Context.Properties.TryGetValue(nameof(MinimalApiModel), out var minimalApiModelObj);
            config.Context.Properties.TryGetValue(Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
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

    /// <summary>
    /// Adds a step to add the necessary packages for the Minimal API to the scaffold builder.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The modified scaffold builder.</returns>
    public static IScaffoldBuilder WithMinimalApiAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            //this scaffolder has a non-EF scenario, only add EF packages if DataContext is provided.
            List<Package> packages = [];
            if (context.Properties.TryGetValue(nameof(MinimalApiSettings), out var commandSettingsObj) &&
                commandSettingsObj is MinimalApiSettings commandSettings)
            {
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
                if (!string.IsNullOrEmpty(commandSettings.DataContext) &&
                    !string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
                    PackageConstants.EfConstants.EfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out Package? projectPackage))
                {
                    packages.Add(PackageConstants.EfConstants.EfCoreToolsPackage);
                    packages.Add(projectPackage);
                }

                if (commandSettings.OpenApi)
                {
                    packages.Add(PackageConstants.AspNetCorePackages.OpenApiPackage);
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

    /// <summary>
    /// Adds a text templating step for the Minimal API to the scaffold builder.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The modified scaffold builder.</returns>
    public static IScaffoldBuilder WithMinimalApiTextTemplatingStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedTextTemplatingStep>(config =>
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
