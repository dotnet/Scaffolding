using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier
{
    //static class helper for CodeAnalysis 
    public static class CodeAnalysisHelper
    {
        //helps create a CodeAnalysis.Project with project files given a project path.
        public static CodeAnalysis.Project LoadCodeAnalysisProject(
            string projectFilePath,
            IEnumerable<string> files)
        {
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject(Path.GetFileName(projectFilePath), "C#");
            var projectWithFiles = project.WithAllSourceFiles(files);
            project = projectWithFiles ?? project;
            return project;
        }
    }
}
