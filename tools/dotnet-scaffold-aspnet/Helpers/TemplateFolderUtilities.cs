// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers
{
    public static class TemplateFoldersUtilities
    {
        public static List<string> GetTemplateFolders(string[] baseFolders)
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

        public static List<string> GetAllT4Templates(string[] baseFolders)
        {
            List<string> allTemplates = [];
            var allTemplateFolders = GetTemplateFolders(baseFolders);
            if (allTemplateFolders != null && allTemplateFolders.Count > 0)
            {
                foreach(var templateFolder in allTemplateFolders)
                {
                    allTemplates.AddRange(Directory.EnumerateFiles(templateFolder, "*.tt", SearchOption.AllDirectories));
                }
            }

            return allTemplates;
        }

        static string? FindFolderWithToolsFolder(string startPath)
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
}
