// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.Files;
using System.Reflection;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal static class IdentityHelper
{
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allFilePaths, IdentityModel identityModel)
    {
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
                //the 'ManageNavPagesModel.tt' only should have .cs extension.
                if (templateFullName.Equals("ManageNavPagesModel", StringComparison.OrdinalIgnoreCase))
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

    internal static IEnumerable<TextTemplatingProperty> GetAdditionalTextTemplatingProperties(IEnumerable<string> allFilePaths, IdentityModel identityModel)
    {
        return [];
    }

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
