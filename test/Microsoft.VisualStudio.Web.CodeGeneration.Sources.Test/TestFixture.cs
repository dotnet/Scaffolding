// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.MsBuild;
using NuGet.Frameworks;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class TestFixture
    {
        NuGetFramework framework = FrameworkConstants.CommonFrameworks.NetStandard16;
        protected string _projectPath;
        private IApplicationInfo _applicationInfo;

        public TestFixture()
        {
            _projectPath = Path.GetFullPath(@"../MsBuildTestApps/MsBuildTestAppSolution/ModelTypesTestLibrary/ModelTypesTestLibrary.csproj");
        }

        public void RunBuild()
        {
            ScaffoldingBuildProcessor processor = new ScaffoldingBuildProcessor();
            MsBuilder<ScaffoldingBuildProcessor> builder = new MsBuilder<ScaffoldingBuildProcessor>(_projectPath, processor);
            builder.RunMsBuild(FrameworkConstants.CommonFrameworks.NetCoreApp10);
            ProjectInfo = new CodeGeneration.ProjectInfo.ProjectInfoContainer()
            {
                ProjectContext = processor.CreateMsBuildProjectContext(),
                ProjectDependencyProvider = processor.CreateDependencyProvider()
            };
        }

        public ProjectInfo.ProjectInfoContainer ProjectInfo { get; private set; }

    }

    [CollectionDefinition("CodeGeneration.Utils")]
    public class CodeGenerationSourcesCollection: ICollectionFixture<TestFixture>
    {

    }
}
