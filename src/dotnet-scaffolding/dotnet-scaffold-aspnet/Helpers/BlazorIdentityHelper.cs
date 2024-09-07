// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.Files;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal static class BlazorIdentityHelper
{
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allT4TemplatePaths, BlazorIdentityModel blazorIdentityModel)
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
                string extension = templateFullName.StartsWith("identity", StringComparison.OrdinalIgnoreCase) ? ".cs" : ".razor";
                string templateNameWithNamespace = $"{blazorIdentityModel.BlazorIdentityNamespace}.{templateFullName}";
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

    internal static TextTemplatingProperty? GetApplicationUserTextTemplatingProperty(string? applicationUserTemplate, BlazorIdentityModel blazorIdentityModel)
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
