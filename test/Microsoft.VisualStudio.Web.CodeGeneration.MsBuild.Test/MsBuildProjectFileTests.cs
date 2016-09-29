using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Xunit;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public class MsBuildProjectFileTests
    {
        private MsBuildProjectFile _projectFile;

        public MsBuildProjectFileTests()
        {
            var path = Path.GetFullPath(@"./TestResources/WebApplication2.csproj");
            var sourceFiles = new List<string>()
            {
                "Program.cs"
            };
            IEnumerable<string> projectReferences = new List<string>()
            {
                "../abc.csproj"
            };
            IEnumerable<string> assemblyReferences = new List<string>()
            {
                "C:\test.dll"
            };
            IDictionary<string, string> properties = new Dictionary<string,string>();
            _projectFile = new MsBuildProjectFile(path, sourceFiles, projectReferences, assemblyReferences, properties);
        }

        [Fact]
        public void Test_ProjectReferences()
        {
            var projectReferences = _projectFile.ProjectReferences;
            Assert.Equal(1, projectReferences.Count());

            var references = _projectFile.AssemblyReferences;
            Assert.Equal(1, references.Count());
        }
    }
}