using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Utils
{
    public class RoslynWorkspaceHelper
    {
        private string _projectPath;
        private Project _project;
        public Project MsBuildProject
        {
            get
            {
                if (_project == null)
                {
                    _project = new Project(_projectPath);
                }
                return _project;
            }
        }

        public RoslynWorkspaceHelper(string projectPath)
        {
            _projectPath = projectPath;
        }

        public Dictionary<string, string> GetMsBuildProperties(IEnumerable<string> properties)
        {
            Dictionary<string, string> msbuildProperties = new Dictionary<string, string>();
            if (properties != null && properties.Any())
            {
                foreach (string property in properties)
                {
                    string value = GetMsBuildProperty(property);
                    if (!string.IsNullOrEmpty(value))
                    {
                        msbuildProperties.Add(property, value);
                    }
                }
            }
            return msbuildProperties;
        }

        public string GetMsBuildProperty(string property)
        {
            if (!string.IsNullOrEmpty(property))
            {
                string value = MsBuildProject?.GetProperty(property)?.EvaluatedValue;
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        internal IEnumerable<ProjectReferenceInformation> GetProjectReferenceInformation(IEnumerable<string> projectReferenceStrings)
        {
            List<ProjectReferenceInformation> projectReferenceInformation = new List<ProjectReferenceInformation>();
            if (projectReferenceStrings != null && projectReferenceStrings.Any())
            {
                foreach (string projectReferenceString in projectReferenceStrings)
                {
                    var currentProject = new Project(Path.GetFullPath(projectReferenceString));
                    if (currentProject != null)
                    {
                        projectReferenceInformation.Add(GetProjectInformation(currentProject));
                    }
                }
            }
            return projectReferenceInformation;
        }

        private ProjectReferenceInformation GetProjectInformation(Project project)
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
