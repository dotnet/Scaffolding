using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class MsBuildProjectFileTests
    {
        private Project _project;
        private MsBuildProjectFile _projectFile;

        public MsBuildProjectFileTests()
        {
            _project = CreateProject(@"./TestResources/WebApplication2.csproj", "Debug");
            _projectFile = new MsBuildProjectFile(_project);
        }

        [Fact]
        public void Test_ProjectReferences()
        {
            var projectReferences = _projectFile.ProjectReferences;
            Assert.Equal(1, projectReferences.Count);

            var references = _projectFile.AssemblyReferences;
            Assert.Equal(1, references.Count);
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
                { "MSBuildExtensionsPath", sdkPath }
            };

            var xmlReader = XmlReader.Create(new FileStream(filePath, FileMode.Open));
            var projectCollection = new ProjectCollection();
            var xml = ProjectRootElement.Create(xmlReader, projectCollection);
            xml.FullPath = filePath;

            var project = new Project(xml, globalProperties, /*toolsVersion*/ null, projectCollection);
            return project;
        }
    }
}