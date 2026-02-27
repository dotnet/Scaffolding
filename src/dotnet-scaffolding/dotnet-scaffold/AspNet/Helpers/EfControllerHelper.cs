// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for EF Controller scaffolding, including template and output path utilities.
/// </summary>
internal static class EfControllerHelper
{
    internal const string ApiEfControllerTemplate = "ApiEfController.tt";
    internal const string MvcEfControllerTemplate = "MvcEfController.tt";

    /// <summary>
    /// Gets the text templating properties for the EF Controller based on the given model.
    /// </summary>
    /// <param name="efControllerModel">The model containing information about the EF Controller.</param>
    /// <returns>A <see cref="TextTemplatingProperty"/> representing the properties needed for text templating.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the template for the specified controller type cannot be found.</exception>
    internal static TextTemplatingProperty GetEfControllerTemplatingProperty(EfControllerModel efControllerModel)
    {
        if (efControllerModel.ProjectInfo is null || string.IsNullOrEmpty(efControllerModel.ProjectInfo.ProjectPath))
        {
            throw new InvalidOperationException($"Could not find project file.");
        }

        var allT4Templates = new TemplateFoldersUtilities().GetAllT4TemplatesForTargetFramework(["EfController"], efControllerModel.ProjectInfo.ProjectPath);
        string? t4TemplatePath = null;
        if (efControllerModel.ControllerType.Equals("API", StringComparison.OrdinalIgnoreCase))
        {
            t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith(ApiEfControllerTemplate, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith(MvcEfControllerTemplate, StringComparison.OrdinalIgnoreCase));
        }

        var templateType = GetCrudControllerType(t4TemplatePath, efControllerModel.ProjectInfo.LowestSupportedTargetFramework);

        if (string.IsNullOrEmpty(t4TemplatePath) ||
            templateType is null)
        {
            throw new InvalidOperationException($"Could not find '{efControllerModel.ControllerType}' template");
        }

        return new TextTemplatingProperty
        {
            TemplatePath = t4TemplatePath,
            TemplateType = templateType,
            TemplateModel = efControllerModel,
            TemplateModelName = "Model",
            OutputPath = Path.Combine(efControllerModel.ControllerOutputPath, $"{efControllerModel.ControllerName}.cs")
        };
    }

    /// <summary>
    /// Gets the CRUD controller type associated with the specified template path.
    /// </summary>
    /// <param name="templatePath">The path of the template file.</param>
    /// <param name="targetFramework">The target framework of the project.</param>
    /// <returns>The <see cref="Type"/> representing the CRUD controller, or null if the template path is null or empty.</returns>
    private static Type? GetCrudControllerType(string? templatePath, TargetFramework? targetFramework)
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
                    ApiEfControllerTemplate => typeof(Templates.net9.EfController.ApiEfController),
                    MvcEfControllerTemplate => typeof(Templates.net9.EfController.MvcEfController),
                    _ => null
                };
            case TargetFramework.Net10:
                return fileName switch
                {
                    ApiEfControllerTemplate => typeof(Templates.net10.EfController.ApiEfController),
                    MvcEfControllerTemplate => typeof(Templates.net10.EfController.MvcEfController),
                    _ => null
                };
            case TargetFramework.Net11:
            default:
                return fileName switch
                {
                    ApiEfControllerTemplate => typeof(Templates.net11.EfController.ApiEfController),
                    MvcEfControllerTemplate => typeof(Templates.net11.EfController.MvcEfController),
                    _ => null
                };
        }
    }
}
