using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier
{
    internal static class ProjectExtensions
    {
        public static CodeAnalysis.Project AddDocuments(this CodeAnalysis.Project project, IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                project = project.AddDocument(file, File.ReadAllText(file)).Project;
            }
            return project;
        }

        public static CodeAnalysis.Project WithAllSourceFiles(this CodeAnalysis.Project project)
        {
            if (!string.IsNullOrEmpty(project.FilePath))
            {
                string projectDirectory = Directory.GetParent(project.FilePath)?.FullName;
                if (!string.IsNullOrEmpty(projectDirectory))
                {
                    var files = GetAllSourceFiles(projectDirectory);
                    var newProject = project.AddDocuments(files);
                    return newProject;
                }
            }
            return null;
        }

        private static IEnumerable<string> GetAllSourceFiles(string directoryPath)
        {
            var filePaths =
                Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories).Union(
                Directory.EnumerateFiles(directoryPath, "*.cshtml", SearchOption.AllDirectories));
            return filePaths;
        }

    }
}
