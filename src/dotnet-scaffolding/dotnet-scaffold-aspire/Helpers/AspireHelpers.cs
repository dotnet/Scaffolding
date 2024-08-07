// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.Roslyn.Extensions;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;

internal static class AspireHelpers
{
    /// <summary>
    /// Returns a dictionary that holds autogenerated project paths that are created during build-time for Aspire host projects.
    /// The string key is the full project path (.csproj) and the string value is the full project name (with namespace)
    /// </summary>
    internal static async Task<Dictionary<string, string>> GetAutoGeneratedProjectNamesAsync(string projectPath, ILogger logger)
    {    
        Dictionary<string, string> autoGeneratedProjectNames = [];
        var codeService = new CodeService(logger, projectPath);
        var solution = (await codeService.GetWorkspaceAsync())?.CurrentSolution;
        var roslynProject = solution?.GetProject(projectPath);
        var allDocuments = roslynProject?.Documents.ToList();
        //roslyn loads the _AppHost.ProjectMetadata.g.cs
        //use this to find the directory for all other *.ProjectMetadata.g.cs files
        var docWithProjectMetadata = allDocuments?.FirstOrDefault(x => x.Name.Contains("ProjectMetadata.g.cs", StringComparison.OrdinalIgnoreCase));
        if (docWithProjectMetadata is null)
        {
            return autoGeneratedProjectNames;
        }

        List<Document> projectMetadataDocuments = [docWithProjectMetadata];
        var projectMetadataDirectory = Path.GetDirectoryName(docWithProjectMetadata.FilePath);
        // Find all other *.ProjectMetadata.g.cs files in the obj directory
        if (!string.IsNullOrEmpty(projectMetadataDirectory) &&
            Directory.Exists(projectMetadataDirectory) && 
            roslynProject is not null)
        {
            var allMatchingFiles = Directory.GetFiles(projectMetadataDirectory, "*ProjectMetadata.g.cs", SearchOption.AllDirectories);
            foreach (var file in allMatchingFiles)
            {
                projectMetadataDocuments.Add(roslynProject.AddDocument(Path.GetFileName(file), SourceText.From(File.ReadAllText(file))));
            }
        }

        var allSyntaxRoots = await Task.WhenAll(projectMetadataDocuments.Select(doc => doc.GetSyntaxRootAsync()));
        // Get all classes with the "Projects" namespace
        var classesInNamespace = allSyntaxRoots
            .SelectMany(root => root?.DescendantNodes().OfType<ClassDeclarationSyntax>() ?? Enumerable.Empty<ClassDeclarationSyntax>())
            .Where(cls => cls.IsInNamespace("Projects"))
            .ToList();

        foreach (var classSyntax in classesInNamespace)
        {
            string? projectPathValue = classSyntax.GetStringPropertyValue("ProjectPath");
            // Get the full class name including the namespace
            var className = classSyntax.Identifier.Text;
            if (!string.IsNullOrEmpty(projectPathValue))
            {
                autoGeneratedProjectNames.TryAdd(projectPathValue, $"Projects.{className}");
            }
        }

        return autoGeneratedProjectNames;
    }
}
