// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for Minimal API scaffolding, including template and output path utilities.
/// </summary>
internal static class MinimalApiHelper
{
    /// <summary>
    /// Gets the <see cref="TextTemplatingProperty"/> for the specified <see cref="MinimalApiModel"/>.
    /// </summary>
    /// <param name="minimalApiModel">The model for which to get the templating property.</param>
    /// <returns>The <see cref="TextTemplatingProperty"/> for the specified model.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the minimal API template cannot be determined.</exception>
    internal static TextTemplatingProperty GetMinimalApiTemplatingProperty(MinimalApiModel minimalApiModel)
    {
        if (minimalApiModel.ProjectInfo is null || string.IsNullOrEmpty(minimalApiModel.ProjectInfo.ProjectPath))
        {
            throw new InvalidOperationException($"Could not find project file.");
        }

        var allT4Templates = new TemplateFoldersUtilities().GetAllT4TemplatesForTargetFramework(["MinimalApi"], minimalApiModel.ProjectInfo.ProjectPath);
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

    /// <summary>
    /// Gets the template type for the specified template path.
    /// </summary>
    /// <param name="templatePath">The path of the template for which to get the type.</param>
    /// <returns>The template type, or null if the template path is invalid.</returns>
    private static Type? GetMinimalApiTemplateType(string? templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        switch (Path.GetFileName(templatePath))
        {
            case "MinimalApi.tt":
                return typeof(Templates.net10.MinimalApi.MinimalApi);
            case "MinimalApiEf.tt":
                return typeof(Templates.net10.MinimalApi.MinimalApiEf);
            default:
                break;
        }

        return null;
    }
}
