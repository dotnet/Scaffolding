// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.MsBuild;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;
using NuGet.Frameworks;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class EFTestFixture
    {
        NuGetFramework framework = FrameworkConstants.CommonFrameworks.NetStandard16;
        protected string _projectPath;

        public EFTestFixture()
        {
            _projectPath = Path.GetFullPath(@"../MsBuildTestApps/MsBuildTestAppSolution/ModelTypesTestLibrary/ModelTypesTestLibrary.csproj");
            RunBuild();
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

            Workspace = new RoslynWorkspace(ProjectInfo.ProjectContext, ProjectInfo.ProjectDependencyProvider);
        }

        public ProjectInfoContainer ProjectInfo { get; private set; }

        public RoslynWorkspace Workspace { get; private set; }

    }

    [CollectionDefinition("CodeGeneration.EF")]
    public class CodeGenerationSourcesCollection : ICollectionFixture<EFTestFixture>
    {

    }
}
