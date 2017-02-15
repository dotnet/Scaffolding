// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Msbuild
{
    public class ProjectReferenceInformationProvider
    {
        public static IEnumerable<ProjectReferenceInformation> GetProjectReferenceInformation(
            string rootProjectPath,
            IEnumerable<string> projectReferences)
        {
            var projectReferenceInformation = new List<ProjectReferenceInformation>();
            if (projectReferences == null)
            {
                return projectReferenceInformation;
            }

            var referencePaths = new Queue<string>(
                projectReferences.Select(path => EnsurePathRooted(path, Path.GetDirectoryName(rootProjectPath))));
            var addedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            addedProjects.Add(rootProjectPath);

            var rootProjDir = Path.GetDirectoryName(rootProjectPath);
            while (referencePaths.Count > 0)
            {
                var currentProjectPath = referencePaths.Dequeue();

                if (addedProjects.Contains(currentProjectPath))
                {
                    //Already added, skip.
                    continue;
                }

                addedProjects.Add(currentProjectPath);
                // If the project is already loaded, it just returns the loaded project.
                // If it is not already loaded, it will load using global properties, toolsets etc. and return.
                var currentProject = ProjectCollection.GlobalProjectCollection.LoadProject(currentProjectPath);
                if (currentProject == null)
                {
                    continue;
                }

                var referenceInfo = GetProjectInformation(currentProject);
                projectReferenceInformation.Add(referenceInfo);

                var currentProjectReferences = GetProjectReferences(currentProject);
                foreach (var prjRef in currentProjectReferences)
                {
                    referencePaths.Enqueue(prjRef);
                }
            }

            return projectReferenceInformation;
        }

        private static string EnsurePathRooted(string targetPath, string basePath)
        {
            if (string.IsNullOrEmpty(targetPath))
            {
                throw new ArgumentNullException(nameof(targetPath));
            }
            return Path.IsPathRooted(targetPath)
                ? Path.GetFullPath(targetPath)
                : Path.GetFullPath(Path.Combine(basePath, targetPath));
        }

        private static IEnumerable<string> GetProjectReferences(Project project)
        {
            return project.GetItems("ProjectReference")
                .Select(i => i.EvaluatedInclude)
                .Select(path => EnsurePathRooted(path, project.DirectoryPath));
        }

        private static ProjectReferenceInformation GetProjectInformation(Project project)
        {
            var compileItems = project.GetItems("Compile").Select(i => i.EvaluatedInclude);
            var fullPath = project.FullPath;
            var name = project.GetProperty("ProjectName")
                ?.EvaluatedValue
                ?? Path.GetFileNameWithoutExtension(fullPath);

            var assemblyPath = project.GetProperty("TargetPath")
                ?.EvaluatedValue;

            var assemblyName = string.IsNullOrEmpty(assemblyPath)
                ? name
                : Path.GetFileNameWithoutExtension(assemblyPath);

            return new ProjectReferenceInformation()
            {
                ProjectName = name,
                AssemblyName = assemblyName,
                CompilationItems = compileItems,
                FullPath = fullPath
            };
        }
    }
}
