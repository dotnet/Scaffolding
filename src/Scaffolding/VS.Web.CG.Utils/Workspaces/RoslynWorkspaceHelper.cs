using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils.Workspaces
{
    internal static class RoslynWorkspaceHelper
    {
        internal static IEnumerable<ProjectReferenceInformation> GetProjectReferenceInformation(IEnumerable<string> projectReferenceStrings)
        {
            List<ProjectReferenceInformation> projectReferenceInformation = new List<ProjectReferenceInformation>();
            if (projectReferenceStrings != null && projectReferenceStrings.Any())
            {   
                //projectReferenceInformation = ProjectReferenceInformationProvider.GetProjectReferenceInformation("C:\\Users\\decho\\Test Scripts\\test4\\test4.csproj", projectReferenceStrings).ToList();
                foreach (string projectReferenceString in projectReferenceStrings)
                {
                    var currentProject = GetMsBuildProject(Path.GetFullPath(projectReferenceString));
                    projectReferenceInformation.Add(GetProjectInformation(currentProject));
                }
            }
            return projectReferenceInformation;
        }

        private static Project GetMsBuildProject(string projectPath)
        {
            return new Project(projectPath);
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
