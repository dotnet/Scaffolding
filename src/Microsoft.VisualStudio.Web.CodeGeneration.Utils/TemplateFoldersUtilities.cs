// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public static class TemplateFoldersUtilities
    {
        public static List<string> GetTemplateFolders(
            string containingProject,
            string applicationBasePath,
            string[] baseFolders,
            IProjectContext projectContext)
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

            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            var rootFolders = new List<string>();
            var templateFolders = new List<string>();

            rootFolders.Add(applicationBasePath);

            var dependency = projectContext.GetPackage(containingProject);

            if (dependency != null)
            {
                string containingProjectPath = "";

                if (dependency.Type == DependencyType.Project)
                {
                    containingProjectPath = Path.GetDirectoryName(dependency.Path);
                }
                else if (dependency.Type == DependencyType.Package)
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