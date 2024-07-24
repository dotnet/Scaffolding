// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor.BlazorCrud;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.MinimalApi;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

internal static class ScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithMinimalApiTextTemplatingStep(this IScaffoldBuilder builder)
    {
        var allT4Templates = new TemplateFoldersUtilities().GetAllT4Templates(["MinimalApi"]);
        string? t4TemplatePath = null;
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

    public static IScaffoldBuilder WithBlazorCrudTextTemplatingStep(
        this IScaffoldBuilder builder)
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
                        blazorCrudModel.ProjectInfo.AppSettings?.Workspace().InputPath);
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
}
