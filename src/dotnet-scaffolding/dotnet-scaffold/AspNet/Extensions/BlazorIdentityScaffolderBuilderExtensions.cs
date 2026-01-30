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

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Blazor Identity scaffolding steps.
/// </summary>
internal static class BlazorIdentityScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a code change step for Blazor Identity scaffolding.
    /// </summary>
    public static IScaffoldBuilder WithBlazorIdentityCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("blazorIdentityChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(IdentitySettings), out var blazorSettingsObj);
            config.Context.Properties.TryGetValue(nameof(IdentityModel), out var blazorIdentityModelObj);
            config.Context.Properties.TryGetValue(Internal.Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
            var blazorIdentitySettings = blazorSettingsObj as IdentitySettings;
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var blazorIdentityModel = blazorIdentityModelObj as IdentityModel;

            //initialize CodeModificationStep's properties
            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                blazorIdentitySettings is not null &&
                codeModifierProperties is not null &&
                blazorIdentityModel is not null)
            {
                step.CodeModifierConfigPath = codeModificationFilePath;
                foreach (var kvp in codeModifierProperties)
                {
                    step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                }

                step.ProjectPath = blazorIdentitySettings.Project;
                step.CodeChangeOptions = blazorIdentityModel.ProjectInfo.CodeChangeOptions ?? [];
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
    /// Adds a text templating step for Blazor Identity scaffolding.
    /// </summary>
    public static IScaffoldBuilder WithBlazorIdentityTextTemplatingStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedTextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(IdentityModel), out var blazorIdentityModelObj);
            IdentityModel blazorIdentityModel = blazorIdentityModelObj as IdentityModel ??
                throw new InvalidOperationException("missing 'IdentityModel' in 'ScaffolderContext.Properties'");
            var templateFolderUtilities = new TemplateFoldersUtilities();
            var allBlazorIdentityFiles = templateFolderUtilities.GetAllT4Templates(["net10.0\\BlazorIdentity"]);
            var applicationUserFile = templateFolderUtilities.GetAllT4Templates(["net10.0\\Files"])
                .FirstOrDefault(x => x.EndsWith("ApplicationUser.tt", StringComparison.OrdinalIgnoreCase));
            var blazorIdentityProperties = BlazorIdentityHelper.GetTextTemplatingProperties(allBlazorIdentityFiles, blazorIdentityModel);
            var applicationUserProperty = BlazorIdentityHelper.GetApplicationUserTextTemplatingProperty(applicationUserFile, blazorIdentityModel);
            if (applicationUserProperty is not null)
            {
                blazorIdentityProperties = blazorIdentityProperties.Append(applicationUserProperty);
            }

            if (blazorIdentityProperties is not null && blazorIdentityProperties.Any())
            {
                step.TextTemplatingProperties = blazorIdentityProperties;
                step.DisplayName = "Blazor identity files";
                step.Overwrite = blazorIdentityModel.Overwrite;
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
    /// Adds a step to add static files required for Blazor Identity scaffolding.
    /// </summary>
    public static IScaffoldBuilder WithBlazorIdentityStaticFilesStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<AddFileStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            if (context.Properties.TryGetValue(nameof(IdentitySettings), out var commandSettingsObj) && commandSettingsObj is IdentitySettings commandSettings)
            {
                var projectDirectory = Path.GetDirectoryName(commandSettings.Project);
                if (Directory.Exists(projectDirectory))
                {
                    step.BaseOutputDirectory = Path.Combine(projectDirectory, "Components", "Account", "Shared");
                    step.FileName = "PasskeySubmit.razor.js";
                    return;
                }
            }

            step.SkipStep = true;
        });

        return builder;
    }

    /// <summary>
    /// Adds a step to add NuGet packages required for Blazor Identity scaffolding.
    /// </summary>
    public static IScaffoldBuilder WithBlazorIdentityAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            List<Package> packages = [
                PackageConstants.AspNetCorePackages.AspNetCoreIdentityEfPackage,
                PackageConstants.AspNetCorePackages.AspNetCoreDiagnosticsEfCorePackage,
                PackageConstants.EfConstants.EfCoreToolsPackage
            ];

            if (context.Properties.TryGetValue(nameof(IdentitySettings), out var commandSettingsObj) &&
                commandSettingsObj is IdentitySettings commandSettings)
            {
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
                if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
                    PackageConstants.EfConstants.IdentityEfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out Package? projectPackage))
                {
                    packages.Add(projectPackage);
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
}
