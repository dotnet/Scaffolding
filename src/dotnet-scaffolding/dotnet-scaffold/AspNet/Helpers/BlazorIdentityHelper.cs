// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.Files;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

//TODO : combine with 'IdentityHelper', should be quite easy.
/// <summary>
/// Helper methods for Blazor Identity scaffolding, including template and output path utilities.
/// </summary>
internal static class BlazorIdentityHelper
{
    /// <summary>
    /// Retrieves the text templating properties for the given T4 templates and Blazor identity model.
    /// </summary>
    /// <param name="allT4TemplatePaths">The paths of all T4 templates.</param>
    /// <param name="blazorIdentityModel">The Blazor identity model containing project and identity information.</param>
    /// <returns>An <see cref="IEnumerable{TextTemplatingProperty}"/> collection containing the text templating properties for the specified templates.</returns>
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allT4TemplatePaths, IdentityModel blazorIdentityModel)
    {
        var textTemplatingProperties = new List<TextTemplatingProperty>();
        foreach (var templatePath in allT4TemplatePaths)
        {
            var templateFullName = GetFormattedRelativeIdentityFile(templatePath);
            var typeName = StringUtil.GetTypeNameFromNamespace(templateFullName);
            var templateType = BlazorIdentityTemplateTypes.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.FullName) &&
                x.FullName.Contains(templateFullName) &&
                x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            var projectName = Path.GetFileNameWithoutExtension(blazorIdentityModel.ProjectInfo.ProjectPath);

            if (!string.IsNullOrEmpty(templatePath) && templateType is not null && !string.IsNullOrEmpty(projectName))
            {
                // Files in Pages and Shared folders are Razor components, others are C# files
                string extension = templateFullName.StartsWith("Pages", StringComparison.OrdinalIgnoreCase) ||
                                   templateFullName.StartsWith("Shared", StringComparison.OrdinalIgnoreCase) ? ".razor" : ".cs";
                string templateNameWithNamespace = $"{blazorIdentityModel.IdentityNamespace}.{templateFullName}";
                string outputFileName = $"{StringUtil.ToPath(templateNameWithNamespace, blazorIdentityModel.BaseOutputPath, projectName)}{extension}";
                textTemplatingProperties.Add(new()
                {
                    TemplateModel = blazorIdentityModel,
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
    /// Retrieves the formatted relative identity file path from the full file name.
    /// </summary>
    /// <param name="fullFileName">The full file name to retrieve the relative identity file path from.</param>
    /// <returns>The formatted relative identity file path.</returns>
    private static string GetFormattedRelativeIdentityFile(string fullFileName)
    {
        string identifier = $"BlazorIdentity{Path.DirectorySeparatorChar}";
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
    /// Retrieves the text templating property for the application user template.
    /// </summary>
    /// <param name="applicationUserTemplate">The path of the application user template.</param>
    /// <param name="blazorIdentityModel">The Blazor identity model containing project and identity information.</param>
    /// <returns>A <see cref="TextTemplatingProperty"/> object containing the text templating property for the application user template, or null if the template path or project directory is invalid.</returns>
    internal static TextTemplatingProperty? GetApplicationUserTextTemplatingProperty(string? applicationUserTemplate, IdentityModel blazorIdentityModel)
    {
        var projectDirectory = Path.GetDirectoryName(blazorIdentityModel.ProjectInfo.ProjectPath);
        if (string.IsNullOrEmpty(applicationUserTemplate) || string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        string userClassOutputPath = $"{Path.Combine(projectDirectory, "Data", blazorIdentityModel.UserClassName)}.cs";
        return new TextTemplatingProperty()
        {
            TemplateModel = blazorIdentityModel,
            TemplateModelName = "Model",
            TemplatePath = applicationUserTemplate,
            TemplateType = typeof(ApplicationUser),
            OutputPath = userClassOutputPath
        };
    }

    private static IList<Type>? _blazorIdentityTemplateTypes;
    private static IList<Type> BlazorIdentityTemplateTypes
    {
        get
        {
            if (_blazorIdentityTemplateTypes is null)
            {
                var allTypes = Assembly.GetExecutingAssembly().GetTypes();
                _blazorIdentityTemplateTypes = allTypes.Where(t => !string.IsNullOrEmpty(t.FullName) && t.FullName.Contains("AspNet.Templates.BlazorIdentity")).ToList();
            }

            return _blazorIdentityTemplateTypes;
        }
    }
}
