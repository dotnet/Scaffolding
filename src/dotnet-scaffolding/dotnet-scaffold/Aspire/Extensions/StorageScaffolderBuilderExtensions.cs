// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Tools.Scaffold.Aspire;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Extension methods for <see cref="IScaffoldBuilder"/> to add storage-related scaffolding steps.
/// </summary>
internal static class StorageScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds steps to the <see cref="IScaffoldBuilder"/> for adding storage-related NuGet packages.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The scaffold builder with storage package steps added.</returns>
    public static IScaffoldBuilder WithStorageAddPackageSteps(this IScaffoldBuilder builder)
    {
        // Step 1: Add storage package to the AppHost project
        builder = builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            // Retrieve CommandSettings from context
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                // Set package details for AppHost storage
                step.Packages = [PackageConstants.StoragePackages.AppHostStoragePackage];
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

        // Step 2: Add storage package to the target project
        builder = builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            // Retrieve CommandSettings from context
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                // Get the package name for the project type
                if (!PackageConstants.StoragePackages.StoragePackagesDict.TryGetValue(commandSettings.Type, out Package? projectPackage) ||
                    projectPackage is null || string.IsNullOrEmpty(projectPackage.Name))
                {
                    // Skip if no package found for type
                    step.SkipStep = true;
                    return;

                }

                // Set package details for the project
                step.Packages = [projectPackage];
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
    /// Adds steps to the <see cref="IScaffoldBuilder"/> for modifying code to support storage integration.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The scaffold builder with storage code modification steps added.</returns>
    public static IScaffoldBuilder WithStorageCodeModificationSteps(this IScaffoldBuilder builder)
    {
        // Step 1: Add code changes to AppHost project for storage
        builder = builder.WithStep<AddAspireCodeChangeStep>(config =>
        {
            if (config.Context.Properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                // Find the code modification config file for AppHost storage
                var codeModificationFilePath = AspireCodeModificationHelper.FindCodeModificationConfigFile("Storage", "storage-apphost.json", System.Reflection.Assembly.GetExecutingAssembly(), commandSettings.AppHostProject);
                if (!string.IsNullOrEmpty(codeModificationFilePath))
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
            }
        });

        // Step 2: Add code changes to the target project for storage
        builder = builder.WithStep<AddAspireCodeChangeStep>(config =>
        {
            if (config.Context.Properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                // Find the code modification config file for API storage
                var codeModificationFilePath = AspireCodeModificationHelper.FindCodeModificationConfigFile("Storage", "storage-webapi.json", System.Reflection.Assembly.GetExecutingAssembly(), commandSettings.Project);
                if (!string.IsNullOrEmpty(codeModificationFilePath))
                {
                    var step = config.Step;
                    // Set code modification details for the project
                    step.CodeModifierConfigPath = codeModificationFilePath;
                    var codeModifierProperties = GetApiProjectProperties(commandSettings);
                    foreach (var kvp in codeModifierProperties)
                    {
                        step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                    }

                    step.ProjectPath = commandSettings.Project;
                    step.CodeChangeOptions = [];
                }
            }
        });

        return builder;
    }

    /// <summary>
    /// Gets code modifier properties for the AppHost project based on the storage type.
    /// </summary>
    /// <param name="commandSettings">The command settings containing the storage type.</param>
    /// <returns>A dictionary of code modifier properties for AppHost.</returns>
    private static IDictionary<string, string> GetAppHostProperties(CommandSettings commandSettings)
    {
        var codeModifierProperties = new Dictionary<string, string>();
        if (StorageConstants.StoragePropertiesDict.TryGetValue(commandSettings.Type, out var storageProperties) && storageProperties is not null)
        {
            codeModifierProperties.Add("$(StorageVariableName)", storageProperties.VariableName);
            codeModifierProperties.Add("$(AddStorageMethodName)", storageProperties.AddMethodName);
        }

        return codeModifierProperties;
    }

    /// <summary>
    /// Gets code modifier properties for the API project based on the storage type.
    /// </summary>
    /// <param name="commandSettings">The command settings containing the storage type.</param>
    /// <returns>A dictionary of code modifier properties for the API project.</returns>
    private static IDictionary<string, string> GetApiProjectProperties(CommandSettings commandSettings)
    {
        var codeModifierProperties = new Dictionary<string, string>();
        if (StorageConstants.StoragePropertiesDict.TryGetValue(commandSettings.Type, out var storageProperties) &&
            storageProperties is not null)
        {
            codeModifierProperties.Add("$(AddClientMethodName)", storageProperties.AddClientMethodName);
            codeModifierProperties.Add("$(StorageVariableName)", storageProperties.VariableName);
        }

        return codeModifierProperties;
    }
}
