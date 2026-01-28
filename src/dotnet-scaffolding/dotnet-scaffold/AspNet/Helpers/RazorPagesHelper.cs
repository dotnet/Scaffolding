// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for Razor Pages scaffolding, including template and output path utilities.
/// </summary>
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

    /// <summary>
    /// Gets the template type for a given template path.
    /// </summary>
    /// <param name="templatePath">The template path.</param>
    /// <returns>The type of the template, or null if the template path is null or empty.</returns>
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
                templateType = typeof(Templates.net10.RazorPages.Create);
                break;
            case CreateModelTemplate:
                templateType = typeof(Templates.net10.RazorPages.CreateModel);
                break;
            case IndexTemplate:
                templateType = typeof(Templates.net10.RazorPages.Index);
                break;
            case IndexModelTemplate:
                templateType = typeof(Templates.net10.RazorPages.IndexModel);
                break;
            case DeleteTemplate:
                templateType = typeof(Templates.net10.RazorPages.Delete);
                break;
            case DeleteModelTemplate:
                templateType = typeof(Templates.net10.RazorPages.DeleteModel);
                break;
            case EditTemplate:
                templateType = typeof(Templates.net10.RazorPages.Edit);
                break;
            case EditModelTemplate:
                templateType = typeof(Templates.net10.RazorPages.EditModel);
                break;
            case DetailsTemplate:
                templateType = typeof(Templates.net10.RazorPages.Details);
                break;
            case DetailsModelTemplate:
                templateType = typeof(Templates.net10.RazorPages.DetailsModel);
                break;
        }

        return templateType;
    }

    /// <summary>
    /// Determines whether the specified template type is valid for the given template file name.
    /// </summary>
    /// <param name="templateType">The template type.</param>
    /// <param name="templateFileName">The template file name.</param>
    /// <returns>true if the template type is valid; otherwise, false.</returns>
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

    /// <summary>
    /// Gets the base output path for generated files based on model name and project path.
    /// </summary>
    /// <param name="modelName">The model name.</param>
    /// <param name="projectPath">The project path.</param>
    /// <returns>The base output path.</returns>
    internal static string GetBaseOutputPath(string modelName, string? projectPath)
    {
        string projectBasePath = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        return Path.Combine(projectBasePath, "Pages", $"{modelName}Pages");
    }

    /// <summary>
    /// Gets the text templating properties for the specified T4 template paths and Razor page model.
    /// </summary>
    /// <param name="allT4TemplatePaths">The collection of all T4 template paths.</param>
    /// <param name="razorPagesModel">The Razor page model.</param>
    /// <returns>A collection of <see cref="TextTemplatingProperty"/> instances that represent the text templating properties.</returns>
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
