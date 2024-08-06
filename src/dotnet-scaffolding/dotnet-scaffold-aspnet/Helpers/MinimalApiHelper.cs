// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal static class MinimalApiHelper
{
    internal static TextTemplatingProperty GetMinimalApiTemplatingProperty(MinimalApiModel minimalApiModel)
    {
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

        var templateType = GetMinimalApiTemplateType(t4TemplatePath);

        if (string.IsNullOrEmpty(t4TemplatePath) ||
            string.IsNullOrEmpty(minimalApiModel.EndpointsPath) ||
            templateType is null)
        {
            throw new InvalidOperationException("could not get minimal api template");
        }

        return new TextTemplatingProperty
        {
            TemplatePath = t4TemplatePath,
            TemplateType = templateType,
            TemplateModel = minimalApiModel,
            TemplateModelName = "Model",
            OutputPath = minimalApiModel.EndpointsPath
        };
    }

    private static Type? GetMinimalApiTemplateType(string? templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        switch (Path.GetFileName(templatePath))
        {
            case "MinimalApi.tt":
                return typeof(Templates.MinimalApi.MinimalApi);
            case "MinimalApiEf.tt":
                return typeof(Templates.MinimalApi.MinimalApiEf);
            default:
                break;
        }

        return null;
    }
}
