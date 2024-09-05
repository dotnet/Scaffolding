// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Extensions;

public static class RoslynExtensions
{
    public static Project? GetProject(this Solution? solution, string? projectPath)
    {
        return solution?.Projects?.FirstOrDefault(x => string.Equals(projectPath, x.FilePath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Given CodeAnalysis.Project and fileName, return CodeAnalysis.Document by reading the latest file from disk.
    /// </summary>
    /// <param name="project"></param>
    /// <param name="fileName"></param>
    /// <param name="fileSystem"></param>
    /// <returns></returns>
    public static Document? GetDocumentFromName(this Project project, string? fileName)
    {
        if (project != null && !string.IsNullOrEmpty(fileName))
        {
            var projectDirectory = Path.GetDirectoryName(project.FilePath);
            string extension = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(extension))
            {
                extension = $"*{extension}";
            }

            if (string.IsNullOrEmpty(projectDirectory))
            {
                return null;
            }

            var allFiles = Directory.EnumerateFiles(projectDirectory, extension, SearchOption.AllDirectories);
            var filePath = allFiles?.FirstOrDefault(x => x.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            var fileText = File.ReadAllText(filePath);
            if (!string.IsNullOrEmpty(fileText))
            {
                return project.AddDocument(filePath, fileText);
            }
        }

        return null;
    }

    public static Document? GetDocument(this Project project, string? documentName)
    {
        var fileName = Path.GetFileName(documentName);
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        //often Document.Name is the file path of the document and not the name.
        //check for all possible cases. 
        return project.Documents.FirstOrDefault(x =>
            x.Name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Equals(documentName, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(x.FilePath) &&
            x.FilePath.Equals(documentName, StringComparison.OrdinalIgnoreCase)));
    }

    public static TextDocument? GetAdditionalDocument(this Project project, string? documentName)
    {
        var fileName = Path.GetFileName(documentName);
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        //often TextDocument.Name is the file path of the document and not the name.
        //check for all possible cases. 
        return project.AdditionalDocuments.FirstOrDefault(x =>
            x.Name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Equals(documentName, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(x.FilePath) &&
            x.FilePath.Equals(documentName, StringComparison.OrdinalIgnoreCase)));
    }
}
