// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor.BlazorCrud;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

internal static class ScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithMinimalApiTextTemplatingStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<TextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            MinimalApiModel? minimalApiModel = null;
            if (context.Properties.TryGetValue(nameof(MinimalApiModel), out var minimalApiModelObj) &&
                minimalApiModelObj is MinimalApiModel)
            {
                minimalApiModel = minimalApiModelObj as MinimalApiModel;
            }

            ArgumentNullException.ThrowIfNull(minimalApiModel);
            var allT4Templates = new TemplateFoldersUtilities().GetAllT4Templates(["MinimalApi"]);
            string? t4TemplatePath = null;
            if (minimalApiModel.DbContextInfo.EfScenario)
            {
                t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("MinimalApiEf.tt", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("MinimalApi.tt", StringComparison.OrdinalIgnoreCase));
            }

            var templateType = MinimalApiHelper.GetMinimalApiTemplateType(t4TemplatePath);

            if (string.IsNullOrEmpty(t4TemplatePath) ||
                string.IsNullOrEmpty(minimalApiModel.EndpointsPath) ||
                templateType is null)
            {
                throw new InvalidOperationException("could not get minimal api template");
            }

            step.TemplatePath = t4TemplatePath;
            step.TemplateType = templateType;
            step.TemplateModel = minimalApiModel;
            step.TemplateModelName = "Model";
            step.OutputPath = minimalApiModel.EndpointsPath;
        });
        
    }

    //TODO : fix this extension method to not do extra work every 'preExecute'
    public static IScaffoldBuilder WithBlazorCrudTextTemplatingStep(this IScaffoldBuilder builder)
    {
        var allT4TemplatePaths = new TemplateFoldersUtilities().GetAllT4Templates(["BlazorCrud"]);
        foreach (var templatePath in allT4TemplatePaths)
        {
            var templateName = Path.GetFileNameWithoutExtension(templatePath);
            var templateType = BlazorCrudHelper.GetTemplateType(templatePath);
            if (!string.IsNullOrEmpty(templatePath) && templateType is not null)
            {
                builder = builder.WithStep<TextTemplatingStep>(config =>
                {
                    var context = config.Context;
                    context.Properties.TryGetValue(nameof(BlazorCrudModel), out var blazorCrudModelObj);
                    BlazorCrudModel blazorCrudModel = blazorCrudModelObj as BlazorCrudModel ??
                        throw new InvalidOperationException("missing 'BlazorCrudModel' in 'ScaffolderContext.Properties'");

                    if (!BlazorCrudHelper.IsValidTemplate(blazorCrudModel.PageType, templateName))
                    {
                        config.Step.SkipStep = true;
                        return;
                    }

                    string baseOutputPath = BlazorCrudHelper.GetBaseOutputPath(
                        blazorCrudModel.ModelInfo.ModelTypeName,
                        blazorCrudModel.ProjectInfo.ProjectPath);
                    string outputFileName = Path.Combine(baseOutputPath, $"{templateName}{Constants.BlazorExtension}");

                    var step = config.Step;
                    step.TemplatePath = templatePath;
                    step.TemplateType = templateType;
                    step.TemplateModelName = "Model";
                    step.TemplateModel = blazorCrudModel;
                    step.OutputPath = outputFileName;
                });
            }
        }

        return builder;
    }

    public static IScaffoldBuilder WithBlazorCrudAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            var packageList = new List<string>()
            {
                PackageConstants.EfConstants.EfToolsPackageName,
                PackageConstants.EfConstants.QuickGridEfAdapterPackageName,
                PackageConstants.EfConstants.AspNetCoreDiagnosticsEfCorePackageName
            };

            if (context.Properties.TryGetValue(nameof(BlazorCrudSettings), out var commandSettingsObj) &&
                commandSettingsObj is BlazorCrudSettings commandSettings)
            {
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
                if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
                    PackageConstants.EfConstants.EfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out string? projectPackageName))
                {
                    packageList.Add(projectPackageName);
                }

                step.PackageNames = packageList;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

    public static IScaffoldBuilder WithBlazorCrudCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<CodeModificationStep>(async config =>
        {
            var step = config.Step;
            CodeModifierConfig? codeModifierConfig = CodeModifierConfigHelper.GetCodeModifierConfig("blazorWebCrudChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(BlazorCrudSettings), out var blazorCrudSettingsObj);
            config.Context.Properties.TryGetValue(nameof(BlazorCrudModel), out var blazorCrudModelObj);
            config.Context.Properties.TryGetValue("CodeModifierProperties", out var codeModifierPropertiesObj);
            var blazorCrudSettings = blazorCrudSettingsObj as BlazorCrudSettings;
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var blazorCrudModel = blazorCrudModelObj as BlazorCrudModel;

            //initialize CodeModificationStep's properties
            if (codeModifierConfig is not null &&
                blazorCrudSettings is not null &&
                codeModifierProperties is not null &&
                blazorCrudModel is not null)
            {
                codeModifierConfig = await BlazorCrudHelper.EditConfigForBlazorCrudAsync(codeModifierConfig, blazorCrudModel);
                step.CodeModifierConfig = codeModifierConfig;
                step.CodeModifierProperties = codeModifierProperties;
                step.ProjectPath = blazorCrudSettings.Project;
                step.CodeChangeOptions = blazorCrudModel.ProjectInfo.CodeChangeOptions ?? new CodeChangeOptions();
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    public static IScaffoldBuilder WithMinimalApiCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<CodeModificationStep>(config =>
        {
            var step = config.Step;
            CodeModifierConfig? codeModifierConfig = CodeModifierConfigHelper.GetCodeModifierConfig("minimalApiChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(MinimalApiSettings), out var minimalApiSettingsObj);
            config.Context.Properties.TryGetValue(nameof(MinimalApiModel), out var minimalApiModelObj);
            config.Context.Properties.TryGetValue("CodeModifierProperties", out var codeModifierPropertiesObj);
            var minimalApiSettings = minimalApiSettingsObj as MinimalApiSettings;
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var minimalApiModel = minimalApiModelObj as MinimalApiModel;

            //initialize CodeModificationStep's properties
            if (codeModifierConfig is not null &&
                minimalApiSettings is not null &&
                codeModifierProperties is not null &&
                minimalApiModel is not null)
            {
                step.CodeModifierConfig = codeModifierConfig;
                step.CodeModifierProperties = codeModifierProperties;
                step.ProjectPath = minimalApiSettings.Project;
                step.CodeChangeOptions = minimalApiModel.ProjectInfo.CodeChangeOptions ?? new CodeChangeOptions();
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    public static IScaffoldBuilder WithMinimalApiAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            //add Microsoft.EntityFrameworkCore.Tools package regardless of the DatabaseProvider
            var packageList = new List<string>()
            {
                PackageConstants.EfConstants.EfToolsPackageName
            };
                                           
            if (context.Properties.TryGetValue(nameof(MinimalApiSettings), out var commandSettingsObj) &&
                commandSettingsObj is MinimalApiSettings commandSettings)
            {
                step.ProjectPath = commandSettings.Project;
                step.Prerelease = commandSettings.Prerelease;
                if (!string.IsNullOrEmpty(commandSettings.DatabaseProvider) &&
                    PackageConstants.EfConstants.EfPackagesDict.TryGetValue(commandSettings.DatabaseProvider, out string? projectPackageName))
                {
                    packageList.Add(projectPackageName);
                }

                step.PackageNames = packageList;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

    public static IScaffoldBuilder WithConnectionStringStep(
        this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<AddConnectionStringStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            DbContextProperties? dbContextProperties = null;
            if (context.Properties.TryGetValue("DbContextProperties", out var dbContextPropertiesObj) &&
                dbContextPropertiesObj is DbContextProperties)
            {
                dbContextProperties = dbContextPropertiesObj as DbContextProperties;
            }

            if (dbContextProperties is null ||
                string.IsNullOrEmpty(dbContextProperties.DbContextPath) ||
                string.IsNullOrEmpty(dbContextProperties.NewDbConnectionString))
            {
                config.Step.SkipStep = true;
                return;
            }

            context.Properties.TryGetValue("BaseProjectPath", out var baseProjectPathObj);
            var baseProjectPathVal = baseProjectPathObj?.ToString();
            if (string.IsNullOrEmpty(baseProjectPathVal))
            {
                throw new ArgumentException("'BaseProjectPath' not provided in 'WithAddDbContextStep' or provided in ScaffolderContext.Properties");
            }

            step.BaseProjectPath = baseProjectPathVal;
            step.ConnectionString = string.Format(dbContextProperties.NewDbConnectionString, dbContextProperties.DbContextName);
            step.ConnectionStringName = dbContextProperties.DbContextName;
        });

        return builder;
    }

    private static void GetDbContextTemplatingStepProperties(
        out string templatePath,
        out Type templateType,
        out string templateModelName)
    {
        //get .tt template file path
        var templateUtilities = new TemplateFoldersUtilities();
        var allT4Templates = templateUtilities.GetAllT4Templates(["DbContext"]);
        string? t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("NewDbContext.tt", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(t4TemplatePath))
        {
            throw new Exception();
        }

        //get System.Type for NewDbContext
        templateType = typeof(NewDbContext);
        templatePath = t4TemplatePath;
        templateModelName = "Model";
    }
}
