using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild.Test
{
    public class RoslynWorkspaceTests
    {
        //[Fact]
        public void TestRoslynWorkspaceCreation()
        {
            var context = new MsBuildProjectContext(@"../TestApps/MsBuildTestApp/MsBuildTestApp/MsBuildTestApp.csproj", "debug");
            var workspace = context.CreateRoslynWorkspace();

            var compilation = workspace.CurrentSolution.Projects.First().GetCompilationAsync().Result;
            var type = compilation.GetTypeByMetadataName("Program");
            Assert.Equal("Program", type.Name);
        }
    }
}
