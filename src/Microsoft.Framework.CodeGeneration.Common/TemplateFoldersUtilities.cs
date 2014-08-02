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
        public static IEnumerable<string> GetTemplateFolders(
            [NotNull]string containingProject,
            [NotNull]ILibraryManager libraryManager)
        {
            string templatesFolderName = "Templates";
            var templateFolders = new List<string>();

            var dependency = libraryManager.GetLibraryInformation(containingProject);

            if (dependency != null)
            {
                string baseFolder = "";

                if (string.Equals("Project", dependency.Type, StringComparison.Ordinal))
                {
                    baseFolder = Path.GetDirectoryName(dependency.Path);
                }
                else if (string.Equals("Package", dependency.Type, StringComparison.Ordinal))
                {
                    baseFolder = dependency.Path;
                }
                else
                {
                    Debug.Assert(false, "Unexpected type of library information for template folders");
                }

                var candidateTemplateFolders = Path.Combine(baseFolder, templatesFolderName);
                if (Directory.Exists(candidateTemplateFolders))
                {
                    templateFolders.Add(candidateTemplateFolders);
                }
            }
            return templateFolders;
        }
    }
}