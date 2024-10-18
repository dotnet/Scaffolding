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

    /// <summary>
    /// given a file name with extension,
    /// return the first file path in the project that matches the file name with extension.
    /// </summary>
    public static string? GetFilePath(this Project project, string? fileNameWithExtension)
    {
        if (string.IsNullOrEmpty(fileNameWithExtension))
        {
            return null;
        }

        var allFiles = project.GetFilesOfExtension(fileNameWithExtension);
        return allFiles?.FirstOrDefault(x => x.EndsWith(fileNameWithExtension.Replace("\\", Path.DirectorySeparatorChar.ToString()), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// given an extension,
    /// return a list of all file paths in the project that have the given extension.
    /// </summary>
    /// <param name="extension">should include the '.' character</param>
    /// <returns></returns>
    public static List<string>? GetFilesOfExtension(this Project project, string extension)
    {
        var fileExtension = Path.GetExtension(extension);
        if (string.IsNullOrEmpty(fileExtension))
        {
            return null;
        }

        var projectDirectory = Path.GetDirectoryName(project.FilePath);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            return null;
        }

        return Directory.EnumerateFiles(projectDirectory, $"*{fileExtension}", SearchOption.AllDirectories).ToList();
    }

    public static Document? GetDocument(this Project project, string? documentName)
    {
        if (string.IsNullOrEmpty(documentName))
        {
            return null;
        }

        //often Document.Name is the file path of the document and not the name.
        //check for all possible cases. 
        return project.Documents.FirstOrDefault(x =>
            x.Name.EndsWith(documentName.Replace("\\", Path.DirectorySeparatorChar.ToString()), StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(x.FilePath) && x.FilePath.EndsWith(documentName.Replace("\\", Path.DirectorySeparatorChar.ToString()), StringComparison.OrdinalIgnoreCase)));
    }

    public static TextDocument? GetAdditionalDocument(this Project project, string? documentName)
    {
        if (string.IsNullOrEmpty(documentName))
        {
            return null;
        }

        //often TextDocument.Name is the file path of the document and not the name.
        //check for all possible cases. 
        return project.AdditionalDocuments.FirstOrDefault(x =>
            !string.IsNullOrEmpty(x.FilePath) &&
            x.FilePath.EndsWith(documentName.Replace("\\", Path.DirectorySeparatorChar.ToString()), StringComparison.OrdinalIgnoreCase));
    }
}
