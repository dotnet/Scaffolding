using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class MsBuildProjectContext
    {
        private readonly Project _project;

        public MsBuildProjectContext(string filePath, string configuration)
        {
            _project = CreateProject(filePath, configuration);
            var result = RunDesignTimeBuild(_project);
            var projectInstance = result.ProjectStateAfterBuild;

            Configuration = configuration;
            ProjectName = Path.GetFileNameWithoutExtension(filePath);
            ProjectFullPath = FindProperty(projectInstance, "ProjectPath");
            RootNamespace = FindProperty(projectInstance, "RootNamespace") ?? ProjectName;
            IsClassLibrary = FindProperty(projectInstance, "OutputType").Equals("Library", StringComparison.OrdinalIgnoreCase);
            Platform = FindProperty(projectInstance, "Platform");
            AssemblyFullPath = FindProperty(projectInstance, "TargetPath");
            Config = AssemblyFullPath + ".config";
            
            // The below are available only if the project is restored. 
            // Should this throw exception if restore is not run yet?
            TargetFramework = NuGetFramework.Parse(FindProperty(projectInstance, "NuGetTargetMoniker"));
            RuntimeConfigJson = FindProperty(projectInstance, "_ProjectRuntimeConfigFilePath");
            DepsJson = FindProperty(projectInstance, "_ProjectDepsFilePath");
            PackagesDirectory = FindProperty(projectInstance, "NuGetPackageRoot");
            TargetDirectory = FindProperty(projectInstance, "TargetDir");
        }

        private BuildResult RunDesignTimeBuild(Project project)
        {
            var projectInstance = project.CreateProjectInstance();
            EnsureTargetDirectoryExists(projectInstance);

            // The Build target currently fails. There is some versioning and target issues that still need to be worked on.
            var buildRequest = new BuildRequestData(projectInstance, new[] { /*"Build",*/ "GenerateBuildDependencyFile" }, null, BuildRequestDataFlags.ProvideProjectStateAfterBuild);
            var buildParams = new BuildParameters(project.ProjectCollection);

            // Add a logger so that if the deps json generation fails,we see the errors on the console.
            buildParams.Loggers = new List<Build.Framework.ILogger>() { new Build.Logging.ConsoleLogger(LoggerVerbosity.Quiet) };
            var result = BuildManager.DefaultBuildManager.Build(buildParams, buildRequest);

            if (result.ProjectStateAfterBuild == null)
            {
                // this is a hack for failed project builds. ProjectStateAfterBuild == null after a failed build
                // But the properties are still available to be read
                result.ProjectStateAfterBuild = projectInstance;
            }
            return result;
        }

        private void EnsureTargetDirectoryExists(ProjectInstance projectInstance)
        {
            var targetDir = FindProperty(projectInstance, "TargetDir");
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
        }

        private static Project CreateProject(string filePath, string configuration)
        {
            var sdkPath = new DotNetSdkResolver().ResolveLatest();
            var msBuildFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "MSBuild.exe"
                : "MSBuild";

            Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", Path.Combine(sdkPath, msBuildFile));

            var globalProperties = new Dictionary<string, string>
            {
                { "Configuration", configuration },
                { "GenerateDependencyFile", "true" },
                { "DesignTimeBuild", "true" },
                { "MSBuildExtensionsPath", sdkPath },
                { "DotNetHostPath", new DotNet.Cli.Utils.Muxer().MuxerPath },
                {"AutoGenerateBindingRedirects", "true" }
            };

            var xmlReader = XmlReader.Create(new FileStream(filePath, FileMode.Open));
            var projectCollection = new ProjectCollection();
            var xml = ProjectRootElement.Create(xmlReader, projectCollection);
            xml.FullPath = filePath;

            var project = new Project(xml, globalProperties, /*toolsVersion*/ null, projectCollection);
            return project;
        }

        private string FindProperty(ProjectInstance project, string propertyName)
            => project.Properties.FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))?.EvaluatedValue;

        public NuGetFramework TargetFramework { get; }
        public bool IsClassLibrary { get; }
        public string Config { get; }
        public string DepsJson { get; }
        public string RuntimeConfigJson { get; }
        public string PackagesDirectory { get; }
        public string AssemblyFullPath { get; }
        public string ProjectName { get; }
        public string Configuration { get; }
        public string Platform { get; }
        public string ProjectFullPath { get; }
        public string RootNamespace { get; }
        public string TargetDirectory { get; }
        public MsBuildProjectFile ProjectFile => new MsBuildProjectFile(_project);
    }

    //internal class BuildLogger : Build.Framework.ILogger
    //{
    //    public string Parameters
    //    {
    //        get;
    //        set;
    //    }

    //    public LoggerVerbosity Verbosity
    //    {
    //        get;
    //        set;
    //    }

    //    public void Initialize(IEventSource eventSource)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void Shutdown()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}