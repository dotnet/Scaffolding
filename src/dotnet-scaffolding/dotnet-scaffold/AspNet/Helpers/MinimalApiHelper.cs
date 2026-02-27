// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for Minimal API scaffolding, including template and output path utilities.
/// </summary>
internal static class MinimalApiHelper
{
    internal const string MinimalApiTemplate = "MinimalApi.tt";
    internal const string MinimalApiEfTemplate = "MinimalApiEf.tt";

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
            t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith(MinimalApiEfTemplate, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith(MinimalApiTemplate, StringComparison.OrdinalIgnoreCase));
        }

        var templateType = GetMinimalApiTemplateType(t4TemplatePath, minimalApiModel.ProjectInfo.LowestSupportedTargetFramework);

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
    /// <param name="targetFramework">The target framework of the project.</param>
    /// <returns>The template type, or null if the template path is invalid.</returns>
    private static Type? GetMinimalApiTemplateType(string? templatePath, TargetFramework? targetFramework)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        var fileName = Path.GetFileName(templatePath);

        switch (targetFramework)
        {
            case TargetFramework.Net8:
            case TargetFramework.Net9:
                return fileName switch
                {
                    MinimalApiTemplate => typeof(Templates.net9.MinimalApi.MinimalApi),
                    MinimalApiEfTemplate => typeof(Templates.net9.MinimalApi.MinimalApiEf),
                    _ => null
                };
            case TargetFramework.Net10:
                return fileName switch
                {
                    MinimalApiTemplate => typeof(Templates.net10.MinimalApi.MinimalApi),
                    MinimalApiEfTemplate => typeof(Templates.net10.MinimalApi.MinimalApiEf),
                    _ => null
                };
            case TargetFramework.Net11:
            default:
                return fileName switch
                {
                    MinimalApiTemplate => typeof(Templates.net11.MinimalApi.MinimalApi),
                    MinimalApiEfTemplate => typeof(Templates.net11.MinimalApi.MinimalApiEf),
                    _ => null
                };
        }
    }
}
