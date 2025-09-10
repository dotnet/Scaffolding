// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.Aspire;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Extension methods for <see cref="IScaffoldBuilder"/> to add caching-related scaffolding steps.
/// </summary>
internal static class CachingScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds steps to the <see cref="IScaffoldBuilder"/> for adding caching-related NuGet packages.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The scaffold builder with caching package steps added.</returns>
    public static IScaffoldBuilder WithCachingAddPackageSteps(this IScaffoldBuilder builder)
    {
        // Step 1: Add Redis package to the AppHost project
        builder = builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            // Retrieve CommandSettings from context
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                // Set package details for AppHost Redis
                step.PackageNames = [PackageConstants.CachingPackages.AppHostRedisPackageName];
                step.ProjectPath = commandSettings.AppHostProject;
                step.Prerelease = commandSettings.Prerelease;
            }
            else
            {
                // Skip step if CommandSettings not found
                step.SkipStep = true;
                return;
            }
        });

        // Step 2: Add caching package to the target project
        builder = builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            // Retrieve CommandSettings from context
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                // Get the package name for the project type
                if (!PackageConstants.CachingPackages.CachingPackagesDict.TryGetValue(commandSettings.Type, out string? projectPackageName) ||
                    string.IsNullOrEmpty(projectPackageName))
                {
                    // Skip if no package found for type
                    step.SkipStep = true;
                    return;

                }

                // Set package details for the project
                step.PackageNames = [projectPackageName];
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
            }
            else
            {
                // Skip step if CommandSettings not found
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    /// <summary>
    /// Adds steps to the <see cref="IScaffoldBuilder"/> for modifying code to support caching.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The scaffold builder with caching code modification steps added.</returns>
    public static IScaffoldBuilder WithCachingCodeModificationSteps(this IScaffoldBuilder builder)
    {
        // Step 1: Add code changes to AppHost project for Redis
        builder = builder.WithStep<AddAspireCodeChangeStep>(config =>
        {
            // Find the code modification config file for AppHost Redis
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
                // Skip step if config file or CommandSettings not found
                config.Step.SkipStep = true;
                return;
            }
        });

        // Step 2: Add code changes to the target project for Redis (with or without output caching)
        builder = builder.WithStep<AddAspireCodeChangeStep>(config =>
        {
            var step = config.Step;
            // Retrieve CommandSettings from context
            if (config.Context.Properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                // Choose config file based on type
                var configName = commandSettings.Type.Equals("redis-with-output-caching", StringComparison.OrdinalIgnoreCase) ? "redis-webapp-oc.json" : "redis-webapp.json";
                var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile(configName, System.Reflection.Assembly.GetExecutingAssembly());
                if (string.IsNullOrEmpty(codeModificationFilePath))
                {
                    // Skip if config file not found
                    step.SkipStep = true;
                    return;
                }

                // Set code modification details for the project
                step.CodeModifierConfigPath = codeModificationFilePath;
                step.ProjectPath = commandSettings.Project;
                step.CodeChangeOptions = [];
            }
            else
            {
                // Skip step if CommandSettings not found
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }
}
