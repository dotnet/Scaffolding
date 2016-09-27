using NuGet.Frameworks;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public class MsBuildProjectContext
    {
        public MsBuildProjectContext(string projectName,
            string configuration,
            string fullpath,
            MsBuildProjectFile projectFile,
            NuGetFramework targetFramework,
            //string assemblyFullPath,
            string platform,
            string configFile,
            string depsJson,
            string rootNameSpace,
            IEnumerable<MsBuildProjectFile> dependencyProjects)
        {
            Requires.NotNull(targetFramework);
            Requires.NotNull(projectFile);
            Requires.NotNullOrEmpty(configuration);
            Requires.NotNullOrEmpty(fullpath);

            ProjectName = projectName;
            TargetFramework = targetFramework;
            ProjectFile = projectFile;
            Platform = platform;
            Configuration = configuration;
            Config = configFile;
            ProjectFullPath = fullpath;
        }

        public NuGetFramework TargetFramework { get; private set; }
        public bool IsClassLibrary { get; private set; }
        public string Config { get; private set; }
        public string DepsJson { get; private set; }
        public string RuntimeConfigJson { get; private set; }
        public string PackagesDirectory { get; private set; }
        public string AssemblyFullPath { get; private set; }
        public string ProjectName { get; private set; }
        public string Configuration { get; private set; }
        public string Platform { get; private set; }
        public string ProjectFullPath { get; private set; }
        public string RootNamespace { get; private set; }
        public string TargetDirectory { get; private set; }
        public MsBuildProjectFile ProjectFile { get; private set; }
        public IEnumerable<MsBuildProjectFile> DependencyProjectFiles { get; private set; }

    }
}