// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Constants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

internal static class EfControllerScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithEfControllerTextTemplatingStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<TextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(EfControllerModel), out var efControllerModelObj);
            EfControllerModel efControllerModel = efControllerModelObj as EfControllerModel ??
                throw new InvalidOperationException("missing 'EfControllerModel' in 'ScaffolderContext.Properties'");

            var efControllerTemplateProperty = EfControllerHelper.GetEfControllerTemplatingProperty(efControllerModel);
            if (efControllerTemplateProperty is not null)
            {
                step.TextTemplatingProperties = [efControllerTemplateProperty];
                step.DisplayName = $"{efControllerModel.ControllerType} controller";
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

    public static IScaffoldBuilder WithEfControllerAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            List<string> packageList = [];
            if (context.Properties.TryGetValue(nameof(EfControllerSettings), out var commandSettingsObj) &&
                commandSettingsObj is EfControllerSettings commandSettings)
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

    public static IScaffoldBuilder WithEfControllerCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<CodeModificationStep>(config =>
        {
            var step = config.Step;
            var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("efControllerChanges.json", System.Reflection.Assembly.GetExecutingAssembly());
            //get needed properties and cast them as needed
            config.Context.Properties.TryGetValue(nameof(EfControllerSettings), out var efControllerSettingsObj);
            config.Context.Properties.TryGetValue(nameof(EfControllerModel), out var efControllerModelObj);
            config.Context.Properties.TryGetValue(Internal.Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
            var efControllerSettings = efControllerSettingsObj as EfControllerSettings;
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
            var efControllerModel = efControllerModelObj as EfControllerModel;

            //initialize CodeModificationStep's properties
            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                efControllerSettings is not null &&
                codeModifierProperties is not null &&
                efControllerModel is not null)
            {
                step.CodeModifierConfigPath = codeModificationFilePath;
                foreach (var kvp in codeModifierProperties)
                {
                    step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                }

                step.ProjectPath = efControllerSettings.Project;
                step.CodeChangeOptions = efControllerModel.ProjectInfo.CodeChangeOptions ?? [];
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    public static IScaffoldBuilder WithMvcViewsStep(this IScaffoldBuilder builder)
    {
        string missingViewModelExceptionMssg =
            "Missing 'ViewModel' in 'ScaffolderContext.Properties'" +
            Environment.NewLine +
            "'ViewModel' is required when using '--views' option";

        builder = builder
            .WithStep<ValidateViewsStep>(config =>
            {
                var step = config.Step;
                var context = config.Context;
                var addViews = context.GetOptionResult<bool>(Constants.CliOptions.ViewsOption);
                if (!addViews)
                {
                    step.SkipStep = true;
                    return;
                }

                step.Project = context.GetOptionResult<string>(Constants.CliOptions.ProjectCliOption);
                step.Model = context.GetOptionResult<string>(Constants.CliOptions.ModelCliOption);
                step.Page = BlazorCrudHelper.CrudPageType;
            })
            .WithStep<TextTemplatingStep>(config =>
            {
                var step = config.Step;
                var context = config.Context;
                var addViews = context.GetOptionResult<bool>(Constants.CliOptions.ViewsOption);
                if (!addViews)
                {
                    step.SkipStep = true;
                    return;
                }

                context.Properties.TryGetValue(nameof(ViewModel), out var viewModelObj);
                ViewModel viewModel = viewModelObj as ViewModel ??
                    throw new InvalidOperationException(missingViewModelExceptionMssg);

                //TODO add extensions if 'TemplateFoldersUtilities' is not reworked.
                var allT4TemplatePaths = new TemplateFoldersUtilities().GetAllT4Templates(["Views"]);
                var viewTemplateProperties = ViewHelper.GetTextTemplatingProperties(allT4TemplatePaths, viewModel);
                if (viewTemplateProperties.Any())
                {
                    step.TextTemplatingProperties = viewTemplateProperties;
                    step.DisplayName = "Razor view files (.cshtml)";
                }
                else
                {
                    step.SkipStep = true;
                    return;
                }
            })
            .WithStep<AddFileStep>(config =>
            {
                var step = config.Step;
                var context = config.Context;
                var addViews = context.GetOptionResult<bool>(Constants.CliOptions.ViewsOption);
                if (!addViews)
                {
                    step.SkipStep = true;
                    return;
                }

                context.Properties.TryGetValue(nameof(CrudSettings), out var viewSettingsObj);
                var viewSettings = viewSettingsObj as CrudSettings ??
                    throw new InvalidOperationException(missingViewModelExceptionMssg);
                var projectDirectory = Path.GetDirectoryName(viewSettings.Project);
                if (Directory.Exists(projectDirectory))
                {
                    var sharedViewsDirectory = Path.Combine(projectDirectory, "Views", "Shared");
                    step.BaseOutputDirectory = sharedViewsDirectory;
                    step.FileName = "_ValidationScriptsPartial.cshtml";
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
