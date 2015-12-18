// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.CodeGeneration
{
    internal static class TemplateFoldersUtilities
    {
        public static List<string> GetTemplateFolders(
            string containingProject,
            string applicationBasePath,
            string[] baseFolders,
            ILibraryManager libraryManager)
        {
            if (containingProject == null)
            {
                throw new ArgumentNullException(nameof(containingProject));
            }

            if (applicationBasePath == null)
            {
                throw new ArgumentNullException(nameof(applicationBasePath));
            }

            if (baseFolders == null)
            {
                throw new ArgumentNullException(nameof(baseFolders));
            }

            if (libraryManager == null)
            {
                throw new ArgumentNullException(nameof(libraryManager));
            }

            var rootFolders = new List<string>();
            var templateFolders = new List<string>();

            rootFolders.Add(applicationBasePath);

            var dependency = libraryManager.GetLibrary(containingProject);

            if (dependency != null)
            {
                string containingProjectPath = "";

                if (string.Equals("Project", dependency.Type, StringComparison.Ordinal))
                {
                    containingProjectPath = Path.GetDirectoryName(dependency.Path);
                }
                else if (string.Equals("Package", dependency.Type, StringComparison.Ordinal))
                {
                    containingProjectPath = dependency.Path;
                }
                else
                {
                    Debug.Assert(false, Resource.UnexpectedTypeLibraryForTemplates);
                }

                if (Directory.Exists(containingProjectPath))
                {
                    rootFolders.Add(containingProjectPath);
                }
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
    }
}