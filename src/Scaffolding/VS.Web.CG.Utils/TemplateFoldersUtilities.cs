// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
// ConsoleLogger
using Microsoft.DotNet.Scaffolding.Shared;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public static class TemplateFoldersUtilities
    {
        static readonly ConsoleLogger _logger = new ConsoleLogger();
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

            var dependency = GetPackage(projectContext, containingProject);

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
                    _logger.LogMessage($"Considering candidate template folder { candidateTemplateFolders }", LogMessageLevel .Trace);
                    if (Directory.Exists(candidateTemplateFolders))
                    {
                        templateFolders.Add(candidateTemplateFolders);
                    }
                }
            }
            return templateFolders;
        }

        public static DependencyDescription GetPackage(IProjectContext context, string name)
        {
            Requires.NotNullOrEmpty(name, nameof(name));
            Requires.NotNull(context, nameof(context));

            return context.PackageDependencies.FirstOrDefault(package => package.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
