// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public static class ScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithTextTemplatingStep(
        this IScaffoldBuilder builder,
        string templateFilePath,
        Type templateType,
        string templateModelName,
        object templateModel,
        string outputPath)
    {
        return builder.WithStep<TextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            step.TemplatePath = templateFilePath;
            step.TemplateType = templateType;
            step.TemplateModelName = templateModelName;
            step.TemplateModel = templateModel;
            step.OutputPath = outputPath;
        });
    }
}
