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

internal static class IdentityApiScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithIdentityApiAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            if (!config.Context.Properties.TryGetValue(nameof(IdentitySettings), out var value) ||
                value is not IdentitySettings settings)
            {
                throw new InvalidOperationException("Missing Identity settings for Identity API scaffolding.");
            }

            var packages = new List<Package>
            {
                PackageConstants.AspNetCorePackages.AspNetCoreIdentityEfPackage,
                PackageConstants.EfConstants.EfCoreToolsPackage,
                PackageConstants.EfConstants.EfCoreDesignPackage
            };
            if (!PackageConstants.EfConstants.IdentityEfPackagesDict.TryGetValue(settings.DatabaseProvider, out var provider))
            {
                throw new InvalidOperationException($"Unsupported Identity database provider '{settings.DatabaseProvider}'.");
            }

            packages.Add(provider);
            if (settings.DatabaseProvider == PackageConstants.EfConstants.SQLite)
            {
                packages.Add(PackageConstants.EfConstants.SqlitePclRawBundlePackage);
            }

            config.Step.ProjectPath = settings.Project;
            config.Step.Prerelease = settings.Prerelease;
            config.Step.Packages = packages;
        });
    }

    public static IScaffoldBuilder WithIdentityApiFilesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddIdentityApiFilesStep>(config =>
        {
            if (!config.Context.Properties.TryGetValue(nameof(IdentitySettings), out var value) ||
                value is not IdentitySettings settings)
            {
                throw new InvalidOperationException("Missing Identity settings for Identity API scaffolding.");
            }

            config.Step.ProjectPath = settings.Project;
            config.Step.Overwrite = settings.Overwrite;
        });
    }

    public static IScaffoldBuilder WithIdentityApiUserStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedTextTemplatingStep>(config =>
        {
            if (!config.Context.Properties.TryGetValue(nameof(IdentityModel), out var value) ||
                value is not IdentityModel model)
            {
                throw new InvalidOperationException("Missing Identity model for Identity API user generation.");
            }

            if (!model.GenerateUser)
            {
                config.Step.SkipStep = true;
                return;
            }

            var template = new TemplateFoldersUtilities()
                .GetAllT4TemplatesForTargetFramework(["Files"], model.ProjectInfo.ProjectPath)
                .FirstOrDefault(path => path.EndsWith("ApplicationUser.tt", StringComparison.OrdinalIgnoreCase));
            var property = IdentityHelper.GetApplicationUserTextTemplatingProperty(template, model);
            if (property is null)
            {
                throw new FileNotFoundException("Identity API user template was not found.");
            }

            config.Step.TextTemplatingProperties = [property];
            config.Step.DisplayName = "Identity API user";
            config.Step.Overwrite = model.Overwrite;
        });
    }

    public static IScaffoldBuilder WithIdentityApiCodeChangeStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            if (!config.Context.Properties.TryGetValue(nameof(IdentitySettings), out var settingsValue) ||
                settingsValue is not IdentitySettings settings ||
                !config.Context.Properties.TryGetValue(nameof(IdentityModel), out var modelValue) ||
                modelValue is not IdentityModel model ||
                !config.Context.Properties.TryGetValue(Internal.Constants.StepConstants.CodeModifierProperties, out var propertiesValue) ||
                propertiesValue is not Dictionary<string, string> properties)
            {
                throw new InvalidOperationException("Missing Identity model or settings for Identity API code changes.");
            }

            var framework = TargetFrameworkHelpers.GetTargetFrameworkFolder(settings.Project);
            var configPath = GlobalToolFileFinder.FindCodeModificationConfigFile(
                "identityApiChanges.json", System.Reflection.Assembly.GetExecutingAssembly(), framework);
            if (string.IsNullOrEmpty(configPath))
            {
                throw new FileNotFoundException("Identity API code modification configuration was not found.");
            }

            config.Step.CodeModifierConfigPath = configPath;
            foreach (var (key, value) in properties)
            {
                config.Step.CodeModifierProperties.TryAdd(key, value);
            }

            config.Step.ProjectPath = settings.Project;
            config.Step.CodeChangeOptions = model.ProjectInfo.CodeChangeOptions ?? [];
        });
    }
}
