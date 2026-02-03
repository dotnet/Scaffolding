// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for EF Controller scaffolding, including template and output path utilities.
/// </summary>
internal static class EfControllerHelper
{
    /// <summary>
    /// Gets the text templating properties for the EF Controller based on the given model.
    /// </summary>
    /// <param name="efControllerModel">The model containing information about the EF Controller.</param>
    /// <returns>A <see cref="TextTemplatingProperty"/> representing the properties needed for text templating.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the template for the specified controller type cannot be found.</exception>
    internal static TextTemplatingProperty GetEfControllerTemplatingProperty(EfControllerModel efControllerModel)
    {
        var allT4Templates = new TemplateFoldersUtilities().GetAllT4Templates(["net11.0\\EfController"]);
        string? t4TemplatePath = null;
        if (efControllerModel.ControllerType.Equals("API", StringComparison.OrdinalIgnoreCase))
        {
            t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("ApiEfController.tt", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("MvcEfController.tt", StringComparison.OrdinalIgnoreCase));
        }

        var templateType = GetCrudControllerType(t4TemplatePath);

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
    /// <returns>The <see cref="Type"/> representing the CRUD controller, or null if the template path is null or empty.</returns>
    private static Type? GetCrudControllerType(string? templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        switch (Path.GetFileName(templatePath))
        {
            case "ApiEfController.tt":
                return typeof(Templates.net10.EfController.ApiEfController);
            case "MvcEfController.tt":
                return typeof(Templates.net10.EfController.MvcEfController);
            default:
                break;
        }

        return null;
    }
}
