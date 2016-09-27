using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class MsBuilder<T> where T : IMsBuildProcessor
    {
        private readonly Project _project;
        private T _buildProcessor;

        public MsBuilder(string filePath, T buildProcessor)
        {
            if (buildProcessor == null)
            {
                throw new ArgumentNullException(nameof(buildProcessor));
            }
            _buildProcessor = buildProcessor;
            _buildProcessor.Init();
            _project = CreateProject(filePath, _buildProcessor.Configuration);
        }

        public void RunMsBuild()
        {
            var result = RunMsBuild(_project);
            _buildProcessor.ProcessBuildResult(_project, result.ProjectStateAfterBuild, result.ResultsByTarget);
        }

        private T BuildProcessor
        {
            get
            {
                return _buildProcessor;
            }
        }

        private BuildResult RunMsBuild(Project project)
        {
            var projectInstance = project.CreateProjectInstance();
            //EnsureTargetDirectoryExists(projectInstance);

            var targets = BuildProcessor.Targets;

            var buildRequest = new BuildRequestData(projectInstance, targets, null, BuildRequestDataFlags.ProvideProjectStateAfterBuild);
            var buildParams = new BuildParameters(project.ProjectCollection);

            // Add loggers
            buildParams.Loggers = BuildProcessor.Loggers;
            var result = BuildManager.DefaultBuildManager.Build(buildParams, buildRequest);

            if (result.ProjectStateAfterBuild == null)
            {
                // this is a hack for failed project builds. ProjectStateAfterBuild == null after a failed build
                // But the properties are still available to be read
                result.ProjectStateAfterBuild = projectInstance;
            }

            return result;
        }

        private Project CreateProject(string filePath, string configuration)
        {
            var sdkPath = new DotNetSdkResolver().ResolveLatest();
            var msBuildFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "MSBuild.exe"
                : "MSBuild";

            Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", Path.Combine(sdkPath, msBuildFile));
            var globalProperties = BuildProcessor.Properties;

            FillInDefaultProperties(globalProperties);

            return ProjectUtilities.CreateProject(filePath, configuration, globalProperties);

            //var xmlReader = XmlReader.Create(new FileStream(filePath, FileMode.Open));
            //var projectCollection = new ProjectCollection();
            //var xml = ProjectRootElement.Create(xmlReader, projectCollection);
            //xml.FullPath = filePath;

            //var project = new Project(xml, globalProperties, /*toolsVersion*/ null, projectCollection);
            //return project;
        }

        private static void FillInDefaultProperties(Dictionary<string, string> globalProperties)
        {
            // This is currently needed because of /dotnet/cli/issues/4207
            if (!globalProperties.ContainsKey("DotnetHostPath"))
            {
                globalProperties["DotnetHostPath"] = new DotNet.Cli.Utils.Muxer().MuxerPath;
            }

            if (!globalProperties.ContainsKey("MSBuildExtensionsPath"))
            {
                globalProperties["MsBuildExtensionsPath"] = new DotNetSdkResolver().ResolveLatest();
            }
        }
    }
}
