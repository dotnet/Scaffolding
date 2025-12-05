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

internal static class IdentityScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithIdentityAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            List<string> packageList = [
                PackageConstants.AspNetCorePackages.AspNetCoreIdentityEfPackageName,
                PackageConstants.AspNetCorePackages.AspNetCoreIdentityUiPackageName,
                PackageConstants.EfConstants.EfCoreToolsPackageName
            ];

            if (context.Properties.TryGetValue(nameof(IdentitySettings), out var commandSettingsObj) &&
                commandSettingsObj is IdentitySettings commandSettings)
            {
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
                if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
                    PackageConstants.EfConstants.IdentityEfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out string? dbProviderPackageName))
                {
                    packageList.Add(dbProviderPackageName);
                }

                step.Packages = [.. packageList.Select(p => new Package(p))];
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

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
            //all the .cshtml and their model class (.cshtml.cs) templates
            var allIdentityPageFiles = templateFolderUtilities.GetAllT4Templates(["Identity"]);
            //ApplicationUser.tt template
            var applicationUserFile = templateFolderUtilities.GetAllT4Templates(["Files"])
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

    public static IScaffoldBuilder WithIdentityCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("identityChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(IdentitySettings), out var identitySettingsObj);
            config.Context.Properties.TryGetValue(nameof(IdentityModel), out var identityModelObj);
            config.Context.Properties.TryGetValue(Internal.Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
            var identitySettings = identitySettingsObj as IdentitySettings;
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
