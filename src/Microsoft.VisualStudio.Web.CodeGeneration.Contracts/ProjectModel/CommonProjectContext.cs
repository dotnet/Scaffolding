using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel
{
    public class CommonProjectContext : IProjectContext
    {
        public string AssemblyFullPath { get; set; }

        public string AssemblyName { get; set; }

        public IEnumerable<ResolvedReference> CompilationAssemblies { get; set; }

        public IEnumerable<string> CompilationItems { get; set; }

        public string Config { get; set; }

        public string Configuration { get; set; }

        public string DepsFile { get; set; }

        public IEnumerable<string> EmbededItems { get; set; }

        public bool IsClassLibrary { get; set; }

        public IEnumerable<DependencyDescription> PackageDependencies { get; set; }

        public string PackagesDirectory { get; set; }

        public string Platform { get; set; }

        public string ProjectFullPath { get; set; }

        public string ProjectName { get; set; }

        public IEnumerable<ProjectReferenceInformation> ProjectReferenceInformation { get; set; }

        public IEnumerable<string> ProjectReferences { get; set; }

        public string RootNamespace { get; set; }

        public string RuntimeConfig { get; set; }

        public string TargetDirectory { get; set; }

        public string TargetFramework { get; set; }
    }
}
