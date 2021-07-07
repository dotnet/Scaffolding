using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

internal static class ProjectExtensions
{
    public static Project AddDocuments(this Project project, IEnumerable<string> files)
    {
        foreach (string file in files)
        {
            project = project.AddDocument(file, File.ReadAllText(file)).Project;
        }
        return project;
    }

    public static Project? WithAllSourceFiles(this Project project)
    {
        if (!string.IsNullOrEmpty(project.FilePath))
        {
            string? projectDirectory = Directory.GetParent(project.FilePath)?.FullName;
            if (!string.IsNullOrEmpty(projectDirectory))
            {
                var files = GetAllSourceFiles(projectDirectory);
                var newProject = project.AddDocuments(files);
                return newProject;
            }
        }
        return null;
    }

    private static IEnumerable<string> GetAllSourceFiles(string directoryPath)
    {
        var filePaths = Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
        return filePaths;
    }

}
