using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier
{
    internal static class ProjectExtensions
    {
        public static CodeAnalysis.Project WithAllSourceFiles(this CodeAnalysis.Project project, IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                project = project.AddDocument(file, File.ReadAllText(file)).Project;
            }

            return project;
        }
    }
}
