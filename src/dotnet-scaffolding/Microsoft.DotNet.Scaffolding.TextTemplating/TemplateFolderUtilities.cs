// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;

namespace Microsoft.DotNet.Scaffolding.TextTemplating;
internal class TemplateFoldersUtilities : ITemplateFolderService
{
    public IEnumerable<string> GetTemplateFolders(string[] baseFolders)
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
                string templatesFolderName = "Templates";
                var candidateTemplateFolders = Path.Combine(rootFolder, templatesFolderName, baseFolderName);
                if (Directory.Exists(candidateTemplateFolders))
                {
                    templateFolders.Add(candidateTemplateFolders);
                }
            }
        }

        return templateFolders;
    }

    public IEnumerable<string> GetAllT4Templates(string[] baseFolders)
    {
        return GetAllFiles(baseFolders, ".tt");
    }

    public IEnumerable<string> GetAllFiles(string[] baseFolders, string? extension = null)
    {
        List<string> allTemplates = [];
        var allTemplateFolders = GetTemplateFolders(baseFolders);
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
}


