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

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Identity scaffolding steps.
/// </summary>
internal static class IdentityScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a step to the scaffold builder to install the necessary NuGet packages for Identity.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    public static IScaffoldBuilder WithIdentityAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            List<Package> packages = [
                PackageConstants.AspNetCorePackages.AspNetCoreIdentityEfPackage,
                PackageConstants.AspNetCorePackages.AspNetCoreIdentityUiPackage,
                PackageConstants.EfConstants.EfCoreToolsPackage
            ];

            if (context.Properties.TryGetValue(nameof(IdentitySettings), out var commandSettingsObj) &&
                commandSettingsObj is IdentitySettings commandSettings)
            {
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
                if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
                    PackageConstants.EfConstants.IdentityEfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out Package? dbProviderPackage))
                {
                    packages.Add(dbProviderPackage);
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
    /// Adds a step to the scaffold builder to perform text templating for Identity files.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    public static IScaffoldBuilder WithIdentityTextTemplatingStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedTextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(IdentityModel), out var blazorIdentityModelObj);
            IdentityModel identityModel = blazorIdentityModelObj as IdentityModel ??
                throw new InvalidOperationException("missing 'IdentityModel' in 'ScaffolderContext.Properties'");
            var templateFolderUtilities = new TemplateFoldersUtilities();

            if (identityModel.ProjectInfo is null || string.IsNullOrEmpty(identityModel.ProjectInfo.ProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            //all the .cshtml and their model class (.cshtml.cs) templates
            var allIdentityPageFiles = templateFolderUtilities.GetAllT4TemplatesForTargetFramework(["Identity"], identityModel.ProjectInfo.ProjectPath);
            //ApplicationUser.tt template
            var applicationUserFile = templateFolderUtilities.GetAllT4TemplatesForTargetFramework(["Files"], identityModel.ProjectInfo.ProjectPath)
                .FirstOrDefault(x => x.EndsWith("ApplicationUser.tt", StringComparison.OrdinalIgnoreCase));
            var identityFileProperties = IdentityHelper.GetTextTemplatingProperties(allIdentityPageFiles, identityModel);
            var applicationUserProperty = IdentityHelper.GetApplicationUserTextTemplatingProperty(applicationUserFile, identityModel);
            if (applicationUserProperty is not null)
            {
                identityFileProperties = identityFileProperties.Append(applicationUserProperty);
            }

            if (identityFileProperties is not null && identityFileProperties.Any())
            {
                step.TextTemplatingProperties = identityFileProperties;
                step.DisplayName = "Identity files";
                step.Overwrite = identityModel.Overwrite;
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
    /// Adds a step to the scaffold builder to apply code changes for Identity.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    public static IScaffoldBuilder WithIdentityCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(IdentitySettings), out var identitySettingsObj);
            var identitySettings = identitySettingsObj as IdentitySettings;
            string targetFrameworkFolder = "net11.0"; //TODO invoke TargetFrameworkHelpers.GetTargetFrameworkFolder(identitySettings?.Project); when other tfm supported
            string? codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("identityChanges.json", System.Reflection.Assembly.GetExecutingAssembly(), targetFrameworkFolder);
            config.Context.Properties.TryGetValue(nameof(IdentityModel), out var identityModelObj);
            config.Context.Properties.TryGetValue(Internal.Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var identityModel = identityModelObj as IdentityModel;

            //initialize CodeModificationStep's properties
            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                identitySettings is not null &&
                codeModifierProperties is not null &&
                identityModel is not null)
            {
                step.CodeModifierConfigPath = codeModificationFilePath;
                foreach (var kvp in codeModifierProperties)
                {
                    step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                }

                step.ProjectPath = identitySettings.Project;
                step.CodeChangeOptions = identityModel.ProjectInfo.CodeChangeOptions ?? [];
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
