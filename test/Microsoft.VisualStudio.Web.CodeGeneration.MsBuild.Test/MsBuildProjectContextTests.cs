using System;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class MsBuildProjectContextTest 
    {
        [Fact]
        public void TestCreateContext()
        {
            var context = new MsBuildProjectContext(@"../TestApps/MsBuildTestApp/MsBuildTestApp/MsBuildTestApp.csproj", "debug");
            //Assert.Equal(Path.Combine(@"../TestApps/MsBuildTestApp/MsBuildTestApp/", "bin", "debug", "MsBuildTestApp.deps.json")
            //    ,context.DepsJson);
            Assert.Equal("MsBuildTestApp", context.ProjectName);
        }
    }
}