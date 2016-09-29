using System;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class MsBuildProjectContextTest
    {
        [Fact(Skip ="Disable tests that need projectInfo")]
        public void TestCreateContext()
        {
            //var path = Path.GetFullPath(@"../TestApps/MsBuildTestApp/MsBuildTestApp/");
            //var path = @"C:\users\prbhosal\Documents\Visual Studio 15\Projects\WebApplication2\WebApplication2\";
            //while (!System.Diagnostics.Debugger.IsAttached) { }
            //var context = new MsBuildProjectContext(Path.Combine(path, "WebApplication2.csproj"), "debug");
            //Assert.Equal(Path.GetFullPath(Path.Combine(@"../TestApps/MsBuildTestApp/MsBuildTestApp/", "bin", "Debug", "MsBuildTestApp.deps.json")).ToLower()
            //    , context.DepsJson.ToLower());
            //Assert.Equal("MsBuildTestApp", context.ProjectName);
            //Assert.True(File.Exists(context.DepsJson), "DepsJson not created");
        }
    }
}