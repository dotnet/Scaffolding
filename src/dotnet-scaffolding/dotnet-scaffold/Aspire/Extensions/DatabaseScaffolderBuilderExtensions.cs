// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.Aspire;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Command;
using Microsoft.DotNet.Scaffolding.Core.Model;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Extension methods for <see cref="IScaffoldBuilder"/> to add database-related scaffolding steps.
/// </summary>
internal static class DatabaseScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds steps to the <see cref="IScaffoldBuilder"/> for adding database-related NuGet packages.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The scaffold builder with database package steps added.</returns>
    public static IScaffoldBuilder WithDatabaseAddPackageSteps(this IScaffoldBuilder builder)
    {
        // Step 1: Add database package to the AppHost project
        builder = builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            // Retrieve CommandSettings from context
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                // Get the AppHost package name for the database type
                if (!PackageConstants.DatabasePackages.DatabasePackagesAppHostDict.TryGetValue(commandSettings.Type, out string? appHostPackageName))
                {
                    // Skip if no package found for type
                    step.SkipStep = true;
                    return;
                }

                // Set package details for AppHost
                step.Packages = [new Package(appHostPackageName)];
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

        // Step 2: Add database package to the API service project
        builder = builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            // Retrieve CommandSettings from context
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                // Get the API service package name for the database type
                if (!PackageConstants.DatabasePackages.DatabasePackagesApiServiceDict.TryGetValue(commandSettings.Type, out string? projectPackageName) ||
                    string.IsNullOrEmpty(projectPackageName))
                {
                    // Skip if no package found for type
                    step.SkipStep = true;
                    return;

                }

                // Set package details for the API service project
                step.Packages = [new Package(projectPackageName)];
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
    /// Adds steps to the <see cref="IScaffoldBuilder"/> for modifying code to support database integration.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The scaffold builder with database code modification steps added.</returns>
    public static IScaffoldBuilder WithDatabaseCodeModificationSteps(this IScaffoldBuilder builder)
    {
        // Step 1: Add code changes to AppHost project for database
        builder = builder.WithStep<AddAspireCodeChangeStep>(config =>
        {
            // Find the code modification config file for AppHost database
            var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("db-apphost.json", System.Reflection.Assembly.GetExecutingAssembly());
            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                config.Context.Properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                var step = config.Step;
                // Get code modifier properties for AppHost
                var codeModifierProperties = GetAppHostProperties(commandSettings);
                step.CodeModifierConfigPath = codeModificationFilePath;
                foreach (var kvp in codeModifierProperties)
                {
                    step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                }

                step.ProjectPath = commandSettings.AppHostProject;
                step.CodeChangeOptions = [];
            }
        });

        // Step 2: Add code changes to API project for database
        builder = builder.WithStep<AddAspireCodeChangeStep>(config =>
        {
            var step = config.Step;
            // Find the code modification config file for API database
            var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("db-webapi.json", System.Reflection.Assembly.GetExecutingAssembly());
            if (config.Context.Properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings &&
                !string.IsNullOrEmpty(codeModificationFilePath))
            {
                // Set code modification details for the API project
                step.CodeModifierConfigPath = codeModificationFilePath;
                var codeModifierProperties = GetApiProjectProperties(commandSettings);
                foreach (var kvp in codeModifierProperties)
                {
                    step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                }

                step.ProjectPath = commandSettings.Project;
                step.CodeChangeOptions = [];
            }
            else
            {
                // Skip step if CommandSettings or config file not found
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    /// <summary>
    /// Gets code modifier properties for the AppHost project based on the database type.
    /// </summary>
    /// <param name="commandSettings">The command settings containing the database type.</param>
    /// <returns>A dictionary of code modifier properties for AppHost.</returns>
    internal static Dictionary<string, string> GetAppHostProperties(CommandSettings commandSettings)
    {
        var codeModifierProperties = new Dictionary<string, string>();
        if (AspireCliStrings.Database.DatabaseTypeDefaults.TryGetValue(commandSettings.Type, out var dbProperties) && dbProperties is not null)
        {
            codeModifierProperties.Add("$(DbName)", dbProperties.AspireDbName);
            codeModifierProperties.Add("$(AddDbMethod)", dbProperties.AspireAddDbMethod);
            codeModifierProperties.Add("$(DbType)", dbProperties.AspireDbType);
        }

        return codeModifierProperties;
    }

    /// <summary>
    /// Gets code modifier properties for the API project based on the database type.
    /// </summary>
    /// <param name="commandSettings">The command settings containing the database type.</param>
    /// <returns>A dictionary of code modifier properties for the API project.</returns>
    internal static Dictionary<string, string> GetApiProjectProperties(CommandSettings commandSettings)
    {
        var codeModifierProperties = new Dictionary<string, string>();
        if (AspireCliStrings.Database.DatabaseTypeDefaults.TryGetValue(commandSettings.Type, out var dbProperties) &&
            AspireCliStrings.Database.DbContextTypeDefaults.TryGetValue(commandSettings.Type, out var dbContextProperties) &&
            dbProperties is not null &&
            dbContextProperties is not null)
        {
            codeModifierProperties.Add("$(DbName)", dbProperties.AspireDbName);
            codeModifierProperties.Add("$(AddDbContextMethod)", dbProperties.AspireAddDbContextMethod);
            codeModifierProperties.Add("$(DbContextName)", dbContextProperties.DbContextName);
        }

        return codeModifierProperties;
    }
}
