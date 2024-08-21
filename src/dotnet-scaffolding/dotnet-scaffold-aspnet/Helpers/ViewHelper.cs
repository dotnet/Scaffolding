// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.Views;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal class ViewHelper
{
    internal const string CreateTemplate = "Create.tt";
    internal const string DeleteTemplate = "Delete.tt";
    internal const string DetailsTemplate = "Details.tt";
    internal const string EditTemplate = "Edit.tt";
    internal const string IndexTemplate = "Index.tt";

    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allT4TemplatePaths, ViewModel viewModel)
    {
        var textTemplatingProperties = new List<TextTemplatingProperty>();
        if (allT4TemplatePaths is not null && allT4TemplatePaths.Any()) 
        {
            foreach (var templatePath in allT4TemplatePaths)
            {
                var templateName = Path.GetFileNameWithoutExtension(templatePath);
                var templateType = GetTemplateType(templatePath);
                if (!string.IsNullOrEmpty(templatePath) && templateType is not null)
                {
                    if (!IsValidTemplate(viewModel.PageType, templateName))
                    {
                        break;
                    }

                    string baseOutputPath = GetBaseOutputPath(viewModel.ModelInfo.ModelTypeName, viewModel.ProjectInfo.ProjectPath);
                    string outputFileName = Path.Combine(baseOutputPath, $"{templateName}{Common.Constants.ViewExtension}");
                    textTemplatingProperties.Add(new()
                    {
                        TemplateModel = viewModel,
                        TemplateModelName = "Model",
                        TemplatePath = templatePath,
                        TemplateType = templateType,
                        OutputPath = outputFileName
                    });
                }
            }
        }
        
        return textTemplatingProperties;
    }

    internal static Type? GetTemplateType(string? templatePath)
    {
        if (string.IsNullOrEmpty(templatePath))
        {
            return null;
        }

        Type? templateType = null;

        switch (Path.GetFileName(templatePath))
        {
            case CreateTemplate:
                templateType = typeof(Create);
                break;
            case IndexTemplate:
                templateType = typeof(Templates.Views.Index);
                break;
            case DeleteTemplate:
                templateType = typeof(Delete);
                break;
            case EditTemplate:
                templateType = typeof(Edit);
                break;
            case DetailsTemplate:
                templateType = typeof(Details);
                break;
        }

        return templateType;
    }

    private static bool IsValidTemplate(string templateType, string templateFileName)
    {
        if (string.Equals("CRUD", templateType, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(templateType, templateFileName, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBaseOutputPath(string modelName, string? projectPath)
    {
        string projectBasePath = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        return Path.Combine(projectBasePath, "Views", modelName);
    }
}
