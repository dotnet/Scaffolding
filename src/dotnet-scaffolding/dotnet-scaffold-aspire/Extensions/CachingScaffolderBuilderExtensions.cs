// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.Aspire;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

internal static class CachingScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithCachingAddPackageSteps(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                step.Packages = new Dictionary<string, string?>() { { PackageConstants.CachingPackages.AppHostRedisPackageName, string.Empty } };
                step.ProjectPath = commandSettings.AppHostProject;
                step.Prerelease = commandSettings.Prerelease;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        builder = builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                if (!PackageConstants.CachingPackages.CachingPackagesDict.TryGetValue(commandSettings.Type, out string? projectPackageName) ||
                    string.IsNullOrEmpty(projectPackageName))
                {
                    step.SkipStep = true;
                    return;
                }

                step.Packages = new Dictionary<string, string?>() { { projectPackageName, string.Empty } };
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    public static IScaffoldBuilder WithCachingCodeModificationSteps(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<AddAspireCodeChangeStep>(config =>
        {
            var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("redis-apphost.json", System.Reflection.Assembly.GetExecutingAssembly());
            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                config.Context.Properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                var step = config.Step;
                step.CodeModifierConfigPath = codeModificationFilePath;
                step.ProjectPath = commandSettings.AppHostProject;
                step.CodeChangeOptions = [];
            }
            else
            {
                config.Step.SkipStep = true;
                return;
            }
        });

        builder = builder.WithStep<AddAspireCodeChangeStep>(config =>
        {
            var step = config.Step;
            if (config.Context.Properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                var configName = commandSettings.Type.Equals("redis-with-output-caching", StringComparison.OrdinalIgnoreCase) ? "redis-webapp-oc.json" : "redis-webapp.json";
                var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile(configName, System.Reflection.Assembly.GetExecutingAssembly());
                if (string.IsNullOrEmpty(codeModificationFilePath))
                {
                    step.SkipStep = true;
                    return;
                }

                step.CodeModifierConfigPath = codeModificationFilePath;
                step.ProjectPath = commandSettings.Project;
                step.CodeChangeOptions = [];
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
