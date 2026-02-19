// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Model;

namespace Microsoft.DotNet.Scaffolding.TextTemplating;
internal class TemplateFoldersUtilities : ITemplateFolderService
{
    public IEnumerable<string> GetAllT4TemplatesForTargetFramework(string[] baseFolders, string? projectPath)
    {
        string targetFrameworkTemplateFolder = GetTargetFrameworkTemplateFolder(projectPath);
        return GetAllFiles(targetFrameworkTemplateFolder, baseFolders, ".tt");
    }

    public IEnumerable<string> GetAllFilesForTargetFramework(string[] baseFolders, string? projectPath)
    {
        string targetFrameworkTemplateFolder = "net11.0"; // TODO call GetTargetFrameworkTemplateFolder(projectPath); when the other target frameworks are supported
        return GetAllFiles(targetFrameworkTemplateFolder, baseFolders);
    }

    public IEnumerable<string> GetAllFiles(string targetFrameworkTemplateFolder, string[] baseFolders, string? extension = null)
    {
        List<string> allTemplates = [];
        var allTemplateFolders = GetTemplateFoldersWithFramework(targetFrameworkTemplateFolder, baseFolders);
        var searchPattern = string.IsNullOrEmpty(extension) ? string.Empty : $"*{Path.GetExtension(extension)}";
        if (allTemplateFolders != null && allTemplateFolders.Any())
        {
            foreach (var templateFolder in allTemplateFolders)
            {
                allTemplates.AddRange(Directory.EnumerateFiles(templateFolder, searchPattern, SearchOption.AllDirectories));
            }
        }

        return allTemplates;
    }

    public IEnumerable<string> GetTemplateFoldersWithFramework(string frameworkTemplateFolder, string[] baseFolders)
    {
        ArgumentNullException.ThrowIfNull(baseFolders);
        var rootFolders = new List<string>();
        var templateFolders = new List<string>();
        var basePath = FindFolderWithToolsFolder(Assembly.GetExecutingAssembly().Location);
        if (Directory.Exists(basePath))
        {
            rootFolders.Add(basePath);
        }

        foreach (var rootFolder in rootFolders)
        {
            foreach (var baseFolderName in baseFolders)
            {
                string templatesFolderName = Path.Combine("AspNet", "Templates");
                var candidateTemplateFolders = Path.Combine(rootFolder, templatesFolderName, frameworkTemplateFolder, baseFolderName);
                if (Directory.Exists(candidateTemplateFolders))
                {
                    templateFolders.Add(candidateTemplateFolders);
                }
            }
        }

        return templateFolders;
    }

    private string? FindFolderWithToolsFolder(string startPath)
    {
        DirectoryInfo? directory = new DirectoryInfo(startPath);

        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "tools")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private string GetTargetFrameworkTemplateFolder(string? projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return "net11.0";
        }

        TargetFramework? targetFramework = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        switch (targetFramework)
        {
            case TargetFramework.Net8:
                return "net8.0";
            case TargetFramework.Net9:
                return "net9.0";
            case TargetFramework.Net10:
                return "net10.0";
        }

        return "net11.0";
    }
}


