// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal static class EfControllerHelper
{
    internal static TextTemplatingProperty GetEfControllerTemplatingProperty(EfControllerModel efControllerModel)
    {
        var allT4Templates = new TemplateFoldersUtilities().GetAllT4Templates(["EfController"]);
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

    private static Type? GetCrudControllerType(string? templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        switch (Path.GetFileName(templatePath))
        {
            case "ApiEfController.tt":
                return typeof(Templates.EfController.ApiEfController);
            case "MvcEfController.tt":
                return typeof(Templates.EfController.MvcEfController);
            default:
                break;
        }

        return null;
    }
}
