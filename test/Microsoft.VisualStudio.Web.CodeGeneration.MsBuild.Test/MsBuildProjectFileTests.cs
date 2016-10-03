// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;
using Xunit;

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

            var projectReferences = new List<string>()
            {
                "../abc.csproj"
            };

            var assemblyReferences = new List<string>()
            {
                "C:\test.dll"
            };

            var properties = new Dictionary<string,string>();
            var targetFrameworkStr = "netcoreapp1.0;net451";
            _projectFile = new MsBuildProjectFile(path, sourceFiles, projectReferences, assemblyReferences, properties, targetFrameworkStr);
        }

        [Fact]
        public void Test_ProjectReferences()
        {
            var projectReferences = _projectFile.ProjectReferences;
            Assert.Equal(1, projectReferences.Count());

            var references = _projectFile.AssemblyReferences;
            Assert.Equal(1, references.Count());

            var targetFrameworks = _projectFile.TargetFrameworks;
            Assert.Equal(2, targetFrameworks.Count());
        }
    }
}