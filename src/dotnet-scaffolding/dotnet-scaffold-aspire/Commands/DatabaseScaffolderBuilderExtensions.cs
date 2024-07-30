// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;

internal static class DatabaseScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithDatabaseAddPackageSteps(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<AddPackagesStep>(config =>
        {
            var step = config.Step;
            var properties = config.Context.Properties;
            if (properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings)
            {
                if (!PackageConstants.DatabasePackages.DatabasePackagesAppHostDict.TryGetValue(commandSettings.Type, out string? appHostPackageName))
                {
                    step.SkipStep = true;
                    return;
                }

                step.PackageNames = [appHostPackageName];
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
                if (!PackageConstants.DatabasePackages.DatabasePackagesApiServiceDict.TryGetValue(commandSettings.Type, out string? projectPackageName) ||
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

    public static IScaffoldBuilder WithDatabaseCodeModificationSteps(this IScaffoldBuilder builder)
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
            var step = config.Step;
            CodeModifierConfig? codeModifierConfig = CodeModifierConfigHelper.GetCodeModifierConfig("db-webapi.json", System.Reflection.Assembly.GetExecutingAssembly());
            if (config.Context.Properties.TryGetValue(nameof(CommandSettings), out var commandSettingsObj) &&
                commandSettingsObj is CommandSettings commandSettings &&
                codeModifierConfig is not null)
            {

                step.CodeModifierConfig = codeModifierConfig;
                step.CodeModifierProperties = GetApiProjectProperties(commandSettings);
                step.ProjectPath = commandSettings.Project;
                step.CodeChangeOptions = new CodeChangeOptions();
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    internal static Dictionary<string, string> GetAppHostProperties(CommandSettings commandSettings)
    {
        var codeModifierProperties = new Dictionary<string, string>();
        if (AspireCommandHelpers.DatabaseTypeDefaults.TryGetValue(commandSettings.Type, out var dbProperties) && dbProperties is not null)
        {
            codeModifierProperties.Add("$(DbName)", dbProperties.AspireDbName);
            codeModifierProperties.Add("$(AddDbMethod)", dbProperties.AspireAddDbMethod);
            codeModifierProperties.Add("$(DbType)", dbProperties.AspireDbType);
        }

        return codeModifierProperties;
    }

    internal static Dictionary<string, string> GetApiProjectProperties(CommandSettings commandSettings)
    {
        var codeModifierProperties = new Dictionary<string, string>();
        if (AspireCommandHelpers.DatabaseTypeDefaults.TryGetValue(commandSettings.Type, out var dbProperties) &&
            AspireCommandHelpers.DbContextTypeDefaults.TryGetValue(commandSettings.Type, out var dbContextProperties) &&
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
