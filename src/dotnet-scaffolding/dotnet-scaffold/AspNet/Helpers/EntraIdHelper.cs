// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for Entra ID scaffolding, including template and output path utilities.
/// </summary>
internal static class EntraIdHelper
{
    /// <summary>
    /// Retrieves the text templating properties for the specified T4 templates and Entra ID model.
    /// </summary>
    /// <param name="allT4TemplatePaths">The collection of all T4 template paths.</param>
    /// <param name="entraIdModel">The Entra ID model containing configuration and data.</param>
    /// <returns>An enumerable collection of <see cref="TextTemplatingProperty"/> instances.</returns>
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allT4TemplatePaths, EntraIdModel entraIdModel)
    {
        var textTemplatingProperties = new List<TextTemplatingProperty>();
        foreach (var templatePath in allT4TemplatePaths)
        {
            var templateFullName = GetFormattedRelativeIdentityFile(templatePath);
            var typeName = StringUtil.GetTypeNameFromNamespace(templateFullName);
            var templateType = BlazorEntraIdTemplateTypes.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.FullName) &&
                x.FullName.Contains(templateFullName) &&
                x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            var projectName = Path.GetFileNameWithoutExtension(entraIdModel.ProjectInfo?.ProjectPath);

            if (!string.IsNullOrEmpty(templatePath) && templateType is not null && !string.IsNullOrEmpty(projectName))
            {
                string extension = templateFullName.StartsWith("loginor", StringComparison.OrdinalIgnoreCase) ? ".razor" : ".cs";
                string templateNameWithNamespace = String.Equals(extension, ".razor") ? $"{entraIdModel.BaseOutputPath}\\Components\\Layout" : $"{entraIdModel.BaseOutputPath}";
                string outputFileName = Path.Combine(templateNameWithNamespace ?? "", templateFullName + extension);

                textTemplatingProperties.Add(new()
                {
                    TemplateModel = entraIdModel,
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
    /// Formats the specified file name to a relative identity file path.
    /// </summary>
    /// <param name="fullFileName">The full file name to format.</param>
    /// <returns>A formatted relative identity file path.</returns>
    private static string GetFormattedRelativeIdentityFile(string fullFileName)
    {
        string identifier = $"BlazorEntraId{Path.DirectorySeparatorChar}";
        int index = fullFileName.IndexOf(identifier);
        if (index != -1)
        {
            string pathAfterIdentifier = fullFileName.Substring(index + identifier.Length);
            string pathAsNamespaceWithoutExtension = StringUtil.GetFilePathWithoutExtension(pathAfterIdentifier);
            return pathAsNamespaceWithoutExtension;
        }

        return string.Empty;
    }

    private static IList<Type>? _blazorEntraIdTemplateTypes;
    private static IList<Type> BlazorEntraIdTemplateTypes
    {
        get
        {
            if (_blazorEntraIdTemplateTypes is null)
            {
                var allTypes = Assembly.GetExecutingAssembly().GetTypes();
                _blazorEntraIdTemplateTypes = allTypes.Where(t => !string.IsNullOrEmpty(t.FullName) && t.FullName.Contains("AspNet.Templates.BlazorEntraId")).ToList();
            }

            return _blazorEntraIdTemplateTypes;
        }
    }
}
