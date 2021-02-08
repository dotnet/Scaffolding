
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Web.CodeGeneration.Msbuild;
using Xunit;
using Xunit.Abstractions;


namespace Microsoft.VisualStudio.Web.CodeGeneration.MSBuild
{
    public class ProjectContextWriterTests : TestBase
    {
        static string testAppPath = Path.Combine("..", "TestApps", "ModelTypesLocatorTestClassLibrary");
        private ITestOutputHelper _outputHelper;

        public ProjectContextWriterTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void TestProjectContextFromMsBuild()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupEmptyCodeGenerationProject(fileProvider, _outputHelper);
                var path = Path.Combine(fileProvider.Root, MsBuildProjectStrings.RootProjectName);

                var projectContext = GetProjectContext(path, true);
                //we know the project name is Test so test some basic properties.
                Assert.True(string.Equals(projectContext.DepsFile, "Test.deps.json", StringComparison.OrdinalIgnoreCase));
                Assert.True(string.Equals(projectContext.RuntimeConfig, "Test.runtimeconfig.json", StringComparison.OrdinalIgnoreCase));
                Assert.False(string.IsNullOrEmpty(projectContext.TargetFramework));
                Assert.False(string.IsNullOrEmpty(projectContext.TargetFrameworkMoniker));
                Assert.True(projectContext.PackageDependencies.Any());
                Assert.True(projectContext.CompilationAssemblies.Any());
                Assert.True(projectContext.PackageDependencies.Where(x => x.Name.Equals("Microsoft.VisualStudio.Web.CodeGeneration.Design")).Any());
            }
        }

        [SkippableTheory]
        [InlineData("C:\\Users\\User\\.nuget\\packages\\", "X.Y.Z", "1.2.3", "C:\\Users\\User\\.nuget\\packages\\x.y.z\\1.2.3")]
        [InlineData("C:\\Users\\User\\.nuget\\", "X.Y.Z", "1.2.3", "C:\\Users\\User\\.nuget\\x.y.z\\1.2.3")]
        [InlineData("C:\\Users\\User\\.nuget\\packages\\", null, null, "")]
        [InlineData("C:\\Users\\User\\.nuget\\packages\\", "X.Y.Z", null, "")]
        [InlineData("C:\\Users\\User\\.nuget\\packages\\", null, "1.2.3", "")]
        [InlineData(null, "X.Y.Z", "1.2.3", "")]
        public void GetPathTestWindows(string nugetPath, string packageName, string version, string expectedPath)
        {
            Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            using (var fileProvider = new TemporaryFileProvider())
            {
                Tuple<string, string> nameAndVersion = new Tuple<string, string>(packageName, version);
                ProjectContextWriter writer = new ProjectContextWriter();
                Assert.Equal(expectedPath, writer.GetPath(nugetPath, nameAndVersion));
            }
        }

        [SkippableTheory]
        [InlineData("C://Users//User//.nuget//packages//", "X.Y.Z", "1.2.3", "C://Users//User//.nuget//packages//x.y.z//1.2.3")]
        [InlineData("C://Users//User//.nuget//", "X.Y.Z", "1.2.3", "C://Users//User//.nuget//x.y.z//1.2.3")]
        [InlineData("C://Users//User//.nuget//packages//", null, null, "")]
        [InlineData("C://Users//User//.nuget//packages//", "X.Y.Z", null, "")]
        [InlineData("C://Users//User//.nuget//packages//", null, "1.2.3", "")]
        [InlineData(null, "X.Y.Z", "1.2.3", "")]
        public void GetPathTestOSX(string nugetPath, string packageName, string version, string expectedPath)
        {
            Skip.If(!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || !RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
            using (var fileProvider = new TemporaryFileProvider())
            {
                Tuple<string, string> nameAndVersion = new Tuple<string, string>(packageName, version);
                ProjectContextWriter writer = new ProjectContextWriter();
                Assert.Equal(expectedPath, writer.GetPath(nugetPath, nameAndVersion));
            }
        }
    }
}
