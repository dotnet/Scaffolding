// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration
{
    internal static class TemplateFoldersUtilities
    {
        public static List<string> GetTemplateFolders(
            [NotNull]string containingProject,
            [NotNull]string[] baseFolders,
            [NotNull]string applicationBasePath,
            [NotNull]ILibraryManager libraryManager)
        {
            string templatesFolderName = "Templates";
            var templateFolders = new List<string>();

            var rootFolders = new List<string>();
            rootFolders.Add(applicationBasePath);

            var dependency = libraryManager.GetLibraryInformation(containingProject);

            if (dependency != null)
            {
                string rootFolder = "";

                if (string.Equals("Project", dependency.Type, StringComparison.Ordinal))
                {
                    rootFolder = Path.GetDirectoryName(dependency.Path);
                }
                else if (string.Equals("Package", dependency.Type, StringComparison.Ordinal))
                {
                    rootFolder = dependency.Path;
                }
                else
                {
                    Debug.Assert(false, "Unexpected type of library information for template folders");
                }
                rootFolders.Add(rootFolder);
            }

            foreach (var rootFolder in rootFolders)
            {
                foreach (var baseFolderName in baseFolders)
                {
                    var candidateTemplateFolders = Path.Combine(rootFolder, templatesFolderName, baseFolderName);
                    if (Directory.Exists(candidateTemplateFolders))
                    {
                        templateFolders.Add(candidateTemplateFolders);
                    }
                }
            }
            return templateFolders;
        }
    }
}