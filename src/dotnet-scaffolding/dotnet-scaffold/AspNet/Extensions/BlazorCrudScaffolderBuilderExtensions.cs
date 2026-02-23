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
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Blazor CRUD scaffolding steps.
/// </summary>
internal static class BlazorCrudScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a step for generating Blazor CRUD text templating files.
    /// </summary>
    public static IScaffoldBuilder WithBlazorCrudTextTemplatingStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedTextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(BlazorCrudModel), out var blazorCrudModelObj);
            BlazorCrudModel blazorCrudModel = blazorCrudModelObj as BlazorCrudModel ??
                throw new InvalidOperationException("missing 'BlazorCrudModel' in 'ScaffolderContext.Properties'");

            if (blazorCrudModel.ProjectInfo is null || string.IsNullOrEmpty(blazorCrudModel.ProjectInfo.ProjectPath))
            {
                step.SkipStep = true;
                return;
            }

            var allT4TemplatePaths = new TemplateFoldersUtilities().GetAllT4TemplatesForTargetFramework(["BlazorCrud"], blazorCrudModel.ProjectInfo.ProjectPath);
            var blazorCrudTemplateProperties = BlazorCrudHelper.GetTextTemplatingProperties(allT4TemplatePaths, blazorCrudModel);
            if (blazorCrudTemplateProperties.Any())
            {
                step.TextTemplatingProperties = blazorCrudTemplateProperties;
                step.DisplayName = "Blazor CRUD files (.razor)";
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

    /// <summary>
    /// Adds a step for adding required NuGet packages for Blazor CRUD scaffolding.
    /// </summary>
    public static IScaffoldBuilder WithBlazorCrudAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            var packages = new List<Package>()
            {
                PackageConstants.AspNetCorePackages.QuickGridEfAdapterPackage,
                PackageConstants.AspNetCorePackages.AspNetCoreDiagnosticsEfCorePackage,
                PackageConstants.EfConstants.EfCoreToolsPackage
            };

            if (context.Properties.TryGetValue(nameof(CrudSettings), out var commandSettingsObj) &&
                commandSettingsObj is CrudSettings commandSettings)
            {
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
                if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
                    PackageConstants.EfConstants.EfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out Package? projectPackage))
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

    /// <summary>
    /// Adds steps for code modification and additional code changes for Blazor CRUD scaffolding.
    /// </summary>
    public static IScaffoldBuilder WithBlazorCrudCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(CrudSettings), out var blazorCrudSettingsObj);
            var blazorCrudSettings = blazorCrudSettingsObj as CrudSettings;
            string targetFrameworkFolder = TargetFrameworkHelpers.GetTargetFrameworkFolder(blazorCrudSettings?.Project);
            string? codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("blazorWebCrudChanges.json", System.Reflection.Assembly.GetExecutingAssembly(), targetFrameworkFolder);
            config.Context.Properties.TryGetValue(nameof(BlazorCrudModel), out var blazorCrudModelObj);
            config.Context.Properties.TryGetValue(Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var blazorCrudModel = blazorCrudModelObj as BlazorCrudModel;

            //initialize CodeModificationStep's properties
            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                blazorCrudSettings is not null &&
                codeModifierProperties is not null &&
                blazorCrudModel is not null)
            {
                step.CodeModifierConfigPath = codeModificationFilePath;
                foreach (var kvp in codeModifierProperties)
                {
                    step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                }

                step.ProjectPath = blazorCrudSettings.Project;
                step.CodeChangeOptions = blazorCrudModel.ProjectInfo.CodeChangeOptions ?? [];
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        //blazor-crud scenario has some custom Program.cs additions that get decided after some analysis.
        //adding these changes with a programmatically created config
        builder = builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            config.Context.Properties.TryGetValue(nameof(CrudSettings), out var blazorCrudSettingsObj);
            config.Context.Properties.TryGetValue(Constants.StepConstants.AdditionalCodeModifier, out var blazorCodeModifierStringObj);
            config.Context.Properties.TryGetValue(nameof(BlazorCrudModel), out var blazorCrudModelObj);
            var blazorCrudSettings = blazorCrudSettingsObj as CrudSettings;
            var blazorCrudModel = blazorCrudModelObj as BlazorCrudModel;
            var blazorCodeModifierString = blazorCodeModifierStringObj as string;
            if (blazorCrudSettings is not null &&
                blazorCrudModel is not null &&
                !string.IsNullOrEmpty(blazorCodeModifierString))
            {
                step.CodeModifierConfigJsonText = blazorCodeModifierString;
                step.ProjectPath = blazorCrudSettings.Project;
                step.CodeChangeOptions = blazorCrudModel.ProjectInfo.CodeChangeOptions ?? [];
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
