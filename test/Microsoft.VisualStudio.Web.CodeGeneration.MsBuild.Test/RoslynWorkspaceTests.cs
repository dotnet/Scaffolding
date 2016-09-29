// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild.Test
{
    public class RoslynWorkspaceTests
    {
        [Fact(Skip ="Disable tests that need projectInfo")]
        public void TestRoslynWorkspaceCreation()
        {
            // Arrange
            var path = Path.GetFullPath(@"../TestApps/MsBuildTestApp/MsBuildTestApp/MsBuildTestApp.csproj");

            ScaffoldingBuildProcessor processor = new ScaffoldingBuildProcessor();
            MsBuilder<ScaffoldingBuildProcessor> builder = new MsBuilder<ScaffoldingBuildProcessor>(path, processor);

            builder.RunMsBuild();
            var dependencyProvider = processor.CreateDependencyProvider();
            var context = processor.CreateMsBuildProjectContext();

            var workspace = new RoslynWorkspace(context, dependencyProvider);
            // Act
            var compilation = workspace.CurrentSolution.Projects.First().GetCompilationAsync().Result;
            var type = compilation.GetTypeByMetadataName("Program");

            // Assert
            Assert.Equal("Program", type.Name);

            // Get type from Dependency.
            type = compilation.GetTypeByMetadataName("Microsoft.VisualStudio.Web.CodeGenerators.Mvc.CommonCommandLineModel");

            Assert.NotNull(type);
            Console.WriteLine(type.ContainingAssembly.Name);
        }
    }
}
