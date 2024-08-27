// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal static class RazorPagesHelper
{
    internal const string CreateTemplate = "Create.tt";
    internal const string CreateModelTemplate = "CreateModel.tt";
    internal const string DeleteTemplate = "Delete.tt";
    internal const string DeleteModelTemplate = "DeleteModel.tt";
    internal const string DetailsTemplate = "Details.tt";
    internal const string DetailsModelTemplate = "DetailsModel.tt";
    internal const string EditTemplate = "Edit.tt";
    internal const string EditModelTemplate = "EditModel.tt";
    internal const string IndexTemplate = "Index.tt";
    internal const string IndexModelTemplate = "IndexModel.tt";
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
                templateType = typeof(Templates.RazorPages.Create);
                break;
            case CreateModelTemplate:
                templateType = typeof(Templates.RazorPages.CreateModel);
                break;
            case IndexTemplate:
                templateType = typeof(Templates.RazorPages.Index);
                break;
            case IndexModelTemplate:
                templateType = typeof(Templates.RazorPages.IndexModel);
                break;
            case DeleteTemplate:
                templateType = typeof(Templates.RazorPages.Delete);
                break;
            case DeleteModelTemplate:
                templateType = typeof(Templates.RazorPages.DeleteModel);
                break;
            case EditTemplate:
                templateType = typeof(Templates.RazorPages.Edit);
                break;
            case EditModelTemplate:
                templateType = typeof(Templates.RazorPages.EditModel);
                break;
            case DetailsTemplate:
                templateType = typeof(Templates.RazorPages.Details);
                break;
            case DetailsModelTemplate:
                templateType = typeof(Templates.RazorPages.DetailsModel);
                break;
        }

        return templateType;
    }

    internal static bool IsValidTemplate(string templateType, string templateFileName)
    {
        if (string.Equals(templateType, "CRUD", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return
            string.Equals(templateFileName, templateType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(templateFileName, $"{templateType}Model", StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetBaseOutputPath(string modelName, string? projectPath)
    {
        string projectBasePath = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        return Path.Combine(projectBasePath, "Pages", $"{modelName}Pages");
    }

    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allT4TemplatePaths, RazorPageModel razorPagesModel)
    {
        var textTemplatingProperties = new List<TextTemplatingProperty>();
        if (allT4TemplatePaths is null || !allT4TemplatePaths.Any())
        {
            return textTemplatingProperties;
        }

        foreach (var templatePath in allT4TemplatePaths)
        {
            var templateName = Path.GetFileNameWithoutExtension(templatePath);
            var templateType = GetTemplateType(templatePath);
            if (!string.IsNullOrEmpty(templatePath) && templateType is not null && !string.IsNullOrEmpty(templateName))
            {
                if (!IsValidTemplate(razorPagesModel.PageType, templateName))
                {
                    break;
                }

                string baseOutputPath = GetBaseOutputPath(
                    razorPagesModel.ModelInfo.ModelTypeName,
                    razorPagesModel.ProjectInfo.ProjectPath);
                string extension = templateName.Contains("Model", StringComparison.OrdinalIgnoreCase) ? Common.Constants.ViewModelExtension : Common.Constants.ViewExtension;
                string formattedTemplateName = templateName.Replace("Model", string.Empty, StringComparison.OrdinalIgnoreCase);
                string outputFileName = Path.Combine(baseOutputPath, $"{formattedTemplateName}{extension}");

                textTemplatingProperties.Add(new()
                {
                    TemplateModel = razorPagesModel,
                    TemplateModelName = "Model",
                    TemplatePath = templatePath,
                    TemplateType = templateType,
                    OutputPath = outputFileName
                });
            }
        }

        return textTemplatingProperties;
    }
}
