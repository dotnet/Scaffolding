// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Project = Microsoft.Build.Evaluation.Project;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils
{
    public static class RoslynWorkspaceHelper
    {
        internal static IEnumerable<ProjectReferenceInformation> GetProjectReferenceInformation(IEnumerable<string> projectReferenceStrings)
        {
            List<ProjectReferenceInformation> projectReferenceInformation = new List<ProjectReferenceInformation>();
            if (projectReferenceStrings != null && projectReferenceStrings.Any())
            {
                foreach (string projectReferenceString in projectReferenceStrings)
                    try
                    {
                        var currentProject = new Project(Path.GetFullPath(projectReferenceString));
                        if (currentProject != null)
                        {
                            projectReferenceInformation.Add(GetProjectInformation(currentProject));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Could not load information for project {projectReferenceString}", ex);
                    }
            }
            return projectReferenceInformation;
        }

        internal static ProjectReferenceInformation GetProjectInformation(Project project)
        {
            var compileItems = project.GetItems("Compile").Select(i => i.EvaluatedInclude);
            var isRazorProject = string.Equals(
                project.GetPropertyValue("UsingMicrosoftNETSdkRazor"),
                "true",
                StringComparison.OrdinalIgnoreCase);
            var razorComponentItems = project.GetItems("RazorComponent")
                .Select(item => item.EvaluatedInclude);
            if (!string.Equals(
                project.GetPropertyValue("EnableDefaultRazorComponentItems"),
                "false",
                StringComparison.OrdinalIgnoreCase))
            {
                razorComponentItems = razorComponentItems.Concat(
                    project.GetItems("Content")
                        .Select(item => item.EvaluatedInclude)
                        .Where(path => string.Equals(Path.GetExtension(path), ".razor", StringComparison.OrdinalIgnoreCase)));
            }
            var fullPath = project.FullPath;
            var name = project.GetProperty("ProjectName")
                ?.EvaluatedValue
                ?? Path.GetFileNameWithoutExtension(fullPath);

            var assemblyPath = project.GetProperty("TargetPath")
                ?.EvaluatedValue;

            var assemblyName = string.IsNullOrEmpty(assemblyPath)
                ? name
                : Path.GetFileNameWithoutExtension(assemblyPath);
            var rootNamespace = project.GetPropertyValue("RootNamespace");

            return new ProjectReferenceInformation()
            {
                ProjectName = name,
                AssemblyName = assemblyName,
                RootNamespace = string.IsNullOrEmpty(rootNamespace) ? name : rootNamespace,
                IsRazorProject = isRazorProject,
                RazorComponentItems = razorComponentItems.ToList(),
                CompilationItems = compileItems.ToList(),
                FullPath = fullPath
            };
        }

        public static async Task<Type> FindExistingType(Workspace workspace, ICodeGenAssemblyLoadContext loader, string projectAssemblyName, string type)
        {
            var compilation = await workspace.CurrentSolution.Projects
                .Where(p => p.AssemblyName == projectAssemblyName)
                .First()
                .GetCompilationAsync();

            if (compilation == null)
            {
                return null;
            }
            var compilationResult = CommonUtilities.GetAssemblyFromCompilation(loader, compilation);
            var allTypes = compilationResult.Assembly.GetTypes();
            //get all types and return the one with the same name. There should be no duplicates so only one should match.
            return allTypes.FirstOrDefault(
                r => r.Name.Equals(type, StringComparison.OrdinalIgnoreCase) ||
                     r.FullName.Equals(type, StringComparison.OrdinalIgnoreCase));
        }
    }
}
