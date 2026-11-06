// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for Razor view scaffolding, including template and output path utilities.
/// </summary>
internal class ViewHelper
{
    /// <summary>
    /// Template file name for the create view.
    /// </summary>
    internal const string CreateTemplate = "Create.tt";

    /// <summary>
    /// Template file name for the delete view.
    /// </summary>
    internal const string DeleteTemplate = "Delete.tt";

    /// <summary>
    /// Template file name for the details view.
    /// </summary>
    internal const string DetailsTemplate = "Details.tt";

    /// <summary>
    /// Template file name for the edit view.
    /// </summary>
    internal const string EditTemplate = "Edit.tt";

    /// <summary>
    /// Template file name for the index view.
    /// </summary>
    internal const string IndexTemplate = "Index.tt";

    /// <summary>
    /// Retrieves the text templating properties for the specified T4 template paths and view model.
    /// </summary>
    /// <param name="allT4TemplatePaths">The collection of all T4 template paths.</param>
    /// <param name="viewModel">The view model containing project and model information.</param>
    /// <returns>An enumerable collection of <see cref="TextTemplatingProperty"/> instances.</returns>
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allT4TemplatePaths, ViewModel viewModel)
    {
        var textTemplatingProperties = new List<TextTemplatingProperty>();
        if (allT4TemplatePaths is not null && allT4TemplatePaths.Any()) 
        {
            foreach (var templatePath in allT4TemplatePaths)
            {
                var templateName = Path.GetFileNameWithoutExtension(templatePath);
                var templateType = GetTemplateType(templatePath, viewModel.ProjectInfo.LowestSupportedTargetFramework);
                if (!string.IsNullOrEmpty(templatePath) && templateType is not null)
                {
                    if (!IsValidTemplate(viewModel.PageType, templateName))
                    {
                        continue;
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

    /// <summary>
    /// Gets the template type for the specified template path.
    /// </summary>
    /// <param name="templatePath">The template path.</param>
    /// <param name="targetFramework">The target framework of the project.</param>
    /// <returns>The <see cref="Type"/> of the template, or null if the templatePath is null or empty.</returns>
    internal static Type? GetTemplateType(string? templatePath, TargetFramework? targetFramework)
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
                    CreateTemplate => typeof(Templates.net9.Views.Create),
                    IndexTemplate => typeof(Templates.net9.Views.Index),
                    DeleteTemplate => typeof(Templates.net9.Views.Delete),
                    EditTemplate => typeof(Templates.net9.Views.Edit),
                    DetailsTemplate => typeof(Templates.net9.Views.Details),
                    _ => null
                };
            case TargetFramework.Net10:
                return fileName switch
                {
                    CreateTemplate => typeof(Templates.net10.Views.Create),
                    IndexTemplate => typeof(Templates.net10.Views.Index),
                    DeleteTemplate => typeof(Templates.net10.Views.Delete),
                    EditTemplate => typeof(Templates.net10.Views.Edit),
                    DetailsTemplate => typeof(Templates.net10.Views.Details),
                    _ => null
                };
            case TargetFramework.Net11:
            default:
                return fileName switch
                {
                    CreateTemplate => typeof(Templates.net11.Views.Create),
                    IndexTemplate => typeof(Templates.net11.Views.Index),
                    DeleteTemplate => typeof(Templates.net11.Views.Delete),
                    EditTemplate => typeof(Templates.net11.Views.Edit),
                    DetailsTemplate => typeof(Templates.net11.Views.Details),
                    _ => null
                };
        }
    }

    /// <summary>
    /// Determines whether the specified template file name is valid for the given template type.
    /// </summary>
    /// <param name="templateType">The type of the template.</param>
    /// <param name="templateFileName">The template file name.</param>
    /// <returns><c>true</c> if the template is valid; otherwise, <c>false</c>.</returns>
    private static bool IsValidTemplate(string templateType, string templateFileName)
    {
        if (string.Equals("CRUD", templateType, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(templateType, templateFileName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the base output path for the specified model name and project path.
    /// </summary>
    /// <param name="modelName">The model name.</param>
    /// <param name="projectPath">The project path.</param>
    /// <returns>The base output path as a string.</returns>
    private static string GetBaseOutputPath(string modelName, string? projectPath)
    {
        string projectBasePath = Path.GetDirectoryName(projectPath) ?? Directory.GetCurrentDirectory();
        return Path.Combine(projectBasePath, "Views", modelName);
    }
}
