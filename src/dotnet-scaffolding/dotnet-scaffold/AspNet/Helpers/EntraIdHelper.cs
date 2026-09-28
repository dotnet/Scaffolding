// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Model;
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
    /// <param name="blazorWasmClientProjectPath">The optional client project path for WebAssembly and Auto apps.</param>
    /// <returns>An enumerable collection of <see cref="TextTemplatingProperty"/> instances.</returns>
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(
        IEnumerable<string> allT4TemplatePaths,
        EntraIdModel entraIdModel,
        string? blazorWasmClientProjectPath = null)
    {
        var templateTypes = GetBlazorEntraIdTemplateTypes(entraIdModel.ProjectInfo?.LowestSupportedTargetFramework);
        var templateByName = allT4TemplatePaths
            .Select(templatePath => new
            {
                TemplatePath = templatePath,
                TemplateName = GetFormattedRelativeIdentityFile(templatePath)
            })
            .Where(x => !string.IsNullOrEmpty(x.TemplateName))
            .ToDictionary(x => x.TemplateName, x => x.TemplatePath, StringComparer.OrdinalIgnoreCase);

        if (templateTypes.Count == 0 || templateByName.Count == 0)
        {
            return [];
        }

        string projectOutputPath = !string.IsNullOrEmpty(blazorWasmClientProjectPath)
            ? Path.GetDirectoryName(blazorWasmClientProjectPath) ?? string.Empty
            : entraIdModel.BaseOutputPath ?? string.Empty;
        string componentsOutputPath = !string.IsNullOrEmpty(blazorWasmClientProjectPath)
            ? projectOutputPath
            : Path.Combine(projectOutputPath, "Components");
        string redirectToLoginOutputPath = !string.IsNullOrEmpty(blazorWasmClientProjectPath)
            ? Path.Combine(componentsOutputPath, "Pages", "RedirectToLogin.razor")
            : Path.Combine(componentsOutputPath, "RedirectToLogin.razor");

        var entries = new List<(string Name, string OutputPath)>
        {
            ("LoginOrLogout", Path.Combine(componentsOutputPath, "Layout", "LoginOrLogout.razor")),
            ("RedirectToLogin", redirectToLoginOutputPath),
        };

        var textTemplatingProperties = new List<TextTemplatingProperty>();
        foreach (var entry in entries)
        {
            if (!templateByName.TryGetValue(entry.Name, out var templatePath))
            {
                continue;
            }

            var templateType = templateTypes.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.FullName) &&
                x.FullName.Contains(entry.Name, StringComparison.OrdinalIgnoreCase) &&
                x.Name.Equals(entry.Name, StringComparison.OrdinalIgnoreCase));

            if (templateType is null)
            {
                continue;
            }

            textTemplatingProperties.Add(new()
            {
                TemplateModel = entraIdModel,
                TemplateModelName = "Model",
                TemplatePath = templatePath,
                TemplateType = templateType,
                OutputPath = entry.OutputPath
            });
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

    private static IList<Type> GetBlazorEntraIdTemplateTypes(TargetFramework? targetFramework)
    {
        return targetFramework switch
        {
            TargetFramework.Net10 => _blazorEntraIdTemplateTypesNet10,
            TargetFramework.Net11 or _ => _blazorEntraIdTemplateTypesNet11,
        };
    }

    private static readonly IList<Type> _blazorEntraIdTemplateTypesNet10 =
    [
        typeof(Templates.net10.BlazorEntraId.LoginOrLogout),
        typeof(Templates.net10.BlazorEntraId.RedirectToLogin),
    ];

    private static readonly IList<Type> _blazorEntraIdTemplateTypesNet11 =
    [
        typeof(Templates.net11.BlazorEntraId.LoginOrLogout),
        typeof(Templates.net11.BlazorEntraId.RedirectToLogin),
    ];
}
