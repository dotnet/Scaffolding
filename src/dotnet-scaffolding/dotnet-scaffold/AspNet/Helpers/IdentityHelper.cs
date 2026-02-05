// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.Files;
using System.Reflection;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for Identity scaffolding, including template and output path utilities.
/// </summary>
internal static class IdentityHelper
{
    /// <summary>
    /// Use the template paths and IdentityModel to create valid 'TextTemplateProperty' objects.
    /// </summary>
    /// <param name="allFilePaths">All file paths.</param>
    /// <param name="identityModel">The identity model.</param>
    /// <returns>An enumerable of TextTemplatingProperty.</returns>
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allFilePaths, IdentityModel identityModel)
    {
        if (identityModel.ProjectInfo is null || string.IsNullOrEmpty(identityModel.ProjectInfo.ProjectPath))
        {
            return [];
        }

        var textTemplatingProperties = new List<TextTemplatingProperty>();
        foreach (var templatePath in allFilePaths)
        {
            var templateFullName = GetFormattedRelativeIdentityFile(templatePath);
            var typeName = StringUtil.GetTypeNameFromNamespace(templateFullName);
            var templateType = IdentityTemplateTypes.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.FullName) &&
                x.FullName.Contains(templateFullName) &&
                x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            var projectName = Path.GetFileNameWithoutExtension(identityModel.ProjectInfo.ProjectPath);
            if (!string.IsNullOrEmpty(templatePath) && templateType is not null && !string.IsNullOrEmpty(projectName))
            {
                string extension = string.Empty;
                //the 'ManageNavPagesModel.tt' only should have .cs extension (only exception)
                if (templateFullName.Contains("ManageNavPagesModel", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".cs";
                }
                else
                {
                    extension = templateFullName.EndsWith("Model", StringComparison.OrdinalIgnoreCase) ? ".cshtml.cs" : ".cshtml";
                }
                
                string formattedTemplateName = templateFullName.Replace("Model", string.Empty, StringComparison.OrdinalIgnoreCase);
                string templateNameWithNamespace = $"{identityModel.IdentityNamespace}.{formattedTemplateName}";
                string outputFileName = $"{StringUtil.ToPath(templateNameWithNamespace, identityModel.BaseOutputPath, projectName)}{extension}";
                textTemplatingProperties.Add(new()
                {
                    TemplateModel = identityModel,
                    TemplateModelName = "Model",
                    TemplatePath = templatePath,
                    TemplateType = templateType,
                    OutputPath = outputFileName
                });
            }
        }

        return textTemplatingProperties;
    }

    /// <summary>
    /// Gets the formatted relative identity file path from the full file name.
    /// </summary>
    /// <param name="fullFileName">The full file name.</param>
    /// <returns>The formatted relative identity file path.</returns>
    private static string GetFormattedRelativeIdentityFile(string fullFileName)
    {
        string identifier = $"Identity{Path.DirectorySeparatorChar}";
        int index = fullFileName.IndexOf(identifier);
        if (index != -1)
        {
            string pathAfterIdentifier = fullFileName.Substring(index + identifier.Length);
            string pathAsNamespaceWithoutExtension = StringUtil.GetFilePathWithoutExtension(pathAfterIdentifier);
            return pathAsNamespaceWithoutExtension;
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the application user text templating property.
    /// </summary>
    /// <param name="applicationUserTemplate">The application user template.</param>
    /// <param name="identityModel">The identity model.</param>
    /// <returns>A TextTemplatingProperty for the application user.</returns>
    internal static TextTemplatingProperty? GetApplicationUserTextTemplatingProperty(string? applicationUserTemplate, IdentityModel identityModel)
    {
        var projectDirectory = Path.GetDirectoryName(identityModel.ProjectInfo.ProjectPath);
        if (string.IsNullOrEmpty(applicationUserTemplate) || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        string userClassOutputPath = $"{Path.Combine(projectDirectory, "Data", identityModel.UserClassName)}.cs";
        return new TextTemplatingProperty()
        {
            TemplateModel = identityModel,
            TemplateModelName = "Model",
            TemplatePath = applicationUserTemplate,
            TemplateType = typeof(ApplicationUser),
            OutputPath = userClassOutputPath
        };
    }

    private static IList<Type>? _identityTemplateTypes;
    private static IList<Type> IdentityTemplateTypes
    {
        get
        {
            if (_identityTemplateTypes is null)
            {
                var allTypes = Assembly.GetExecutingAssembly().GetTypes();
                _identityTemplateTypes = allTypes.Where(t => !string.IsNullOrEmpty(t.FullName) && t.FullName.Contains("AspNet.Templates.Identity")).ToList();
            }

            return _identityTemplateTypes;
        }
    }
}
