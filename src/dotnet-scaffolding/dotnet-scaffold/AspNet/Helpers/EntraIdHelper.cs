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
    /// <param name="blazorWasmClientProjectPath">The optional Blazor WebAssembly client project path.</param>
    /// <returns>An enumerable collection of <see cref="TextTemplatingProperty"/> instances.</returns>
    internal static IEnumerable<TextTemplatingProperty> GetTextTemplatingProperties(IEnumerable<string> allT4TemplatePaths, EntraIdModel entraIdModel, string? blazorWasmClientProjectPath = null)
    {
        var textTemplatingProperties = new List<TextTemplatingProperty>();
        var templateTypes = GetBlazorEntraIdTemplateTypes(entraIdModel.ProjectInfo?.LowestSupportedTargetFramework);
        var serverLayoutPath = Path.Combine(entraIdModel.BaseOutputPath ?? string.Empty, "Components", "Layout");
        var serverNavMenuPath = Path.Combine(serverLayoutPath, "NavMenu.razor");
        var clientProjectDirectory = string.IsNullOrEmpty(blazorWasmClientProjectPath) ? null : Path.GetDirectoryName(blazorWasmClientProjectPath);
        var clientLayoutPath = string.IsNullOrEmpty(clientProjectDirectory) ? null : Path.Combine(clientProjectDirectory, "Layout");
        var clientNavMenuPath = string.IsNullOrEmpty(clientLayoutPath) ? null : Path.Combine(clientLayoutPath, "NavMenu.razor");

        foreach (var templatePath in allT4TemplatePaths)
        {
            var templateFullName = GetFormattedRelativeIdentityFile(templatePath);
            var typeName = StringUtil.GetTypeNameFromNamespace(templateFullName);
            var templateType = templateTypes.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.FullName) &&
                x.FullName.Contains(templateFullName) &&
                x.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            var projectName = Path.GetFileNameWithoutExtension(entraIdModel.ProjectInfo?.ProjectPath);

            if (!string.IsNullOrEmpty(templatePath) && templateType is not null && !string.IsNullOrEmpty(projectName))
            {
                string extension = templateFullName.StartsWith("loginor", StringComparison.OrdinalIgnoreCase) ? ".razor" : ".cs";
                IEnumerable<string> outputDirectories = String.Equals(extension, ".razor", StringComparison.Ordinal)
                    ? GetLayoutOutputPaths(serverLayoutPath, serverNavMenuPath, clientLayoutPath, clientNavMenuPath)
                    : [entraIdModel.BaseOutputPath ?? string.Empty];

                foreach (string outputDirectory in outputDirectories)
                {
                    textTemplatingProperties.Add(new()
                    {
                        TemplateModel = entraIdModel,
                        TemplateModelName = "Model",
                        TemplatePath = templatePath,
                        TemplateType = templateType,
                        OutputPath = Path.Combine(outputDirectory, templateFullName + extension)
                    });
                }
            }
        }

        return textTemplatingProperties;
    }

    private static IReadOnlyList<string> GetLayoutOutputPaths(string serverLayoutPath, string serverNavMenuPath, string? clientLayoutPath, string? clientNavMenuPath)
    {
        var layoutOutputPaths = new List<string>();

        if (File.Exists(serverNavMenuPath))
        {
            layoutOutputPaths.Add(serverLayoutPath);
        }

        if (!string.IsNullOrEmpty(clientLayoutPath) &&
            !string.IsNullOrEmpty(clientNavMenuPath) &&
            File.Exists(clientNavMenuPath))
        {
            layoutOutputPaths.Add(clientLayoutPath);
        }

        if (layoutOutputPaths.Count > 0)
        {
            return layoutOutputPaths;
        }

        string expectedPaths = string.IsNullOrEmpty(clientNavMenuPath) ? $"'{serverNavMenuPath}'" : $"'{serverNavMenuPath}' or '{clientNavMenuPath}'";
        throw new InvalidOperationException($"Could not locate the Blazor navigation component. Expected {expectedPaths}.");
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
    ];

    private static readonly IList<Type> _blazorEntraIdTemplateTypesNet11 =
    [
        typeof(Templates.net11.BlazorEntraId.LoginOrLogout),
    ];
}
