// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;

internal static class StorageScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithStorageAddPackageSteps(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<AddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                step.PackageNames = [PackageConstants.StoragePackages.AppHostStoragePackageName];
                step.ProjectPath = commandSettings.AppHostProject;
                step.Prerelease = commandSettings.Prerelease;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        builder = builder.WithStep<AddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                if (!PackageConstants.StoragePackages.StoragePackagesDict.TryGetValue(commandSettings.Type, out string? projectPackageName) ||
                    string.IsNullOrEmpty(projectPackageName))
                {
                    step.SkipStep = true;
                    return;

                }

                step.PackageNames = [projectPackageName];
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

    public static IScaffoldBuilder WithStorageCodeModificationSteps(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<AddAspireCodeChangeStep>(config =>
        {
            CodeModifierConfig? codeModifierConfig = CodeModifierConfigHelper.GetCodeModifierConfig("db-apphost.json", System.Reflection.Assembly.GetExecutingAssembly());
            if (codeModifierConfig is not null &&
                config.Context.Properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                var step = config.Step;
                var codeModifierProperties = GetAppHostProperties(commandSettings);
                step.CodeModifierConfig = codeModifierConfig;
                step.CodeModifierProperties = codeModifierProperties;
                step.ProjectPath = commandSettings.AppHostProject;
                step.CodeChangeOptions = new CodeChangeOptions();
            }
        });

        builder = builder.WithStep<AddAspireCodeChangeStep>(config =>
        {
            CodeModifierConfig? codeModifierConfig = CodeModifierConfigHelper.GetCodeModifierConfig("db-webapi.json", System.Reflection.Assembly.GetExecutingAssembly());
            if (config.Context.Properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings &&
                codeModifierConfig is not null)
            {
                var step = config.Step;
                step.CodeModifierConfig = codeModifierConfig;
                step.CodeModifierProperties = GetApiProjectProperties(commandSettings);
                step.ProjectPath = commandSettings.Project;
                step.CodeChangeOptions = new CodeChangeOptions();
            }
        });

        return builder;
    }

    private static IDictionary<string, string> GetAppHostProperties(CommandSettings commandSettings)
    {
        var codeModifierProperties = new Dictionary<string, string>();
        if (StorageConstants.StoragePropertiesDict.TryGetValue(commandSettings.Type, out var storageProperties) && storageProperties is not null)
        {
            codeModifierProperties.Add("$(StorageVariableName)", storageProperties.VariableName);
            codeModifierProperties.Add("$(AddStorageMethodName)", storageProperties.AddClientMethodName);
        }

        return codeModifierProperties;
    }

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
