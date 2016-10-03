// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Frameworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild.Test
{
    public class ScaffoldingBuildProcessorTests
    {
        [Fact(Skip = "Disable tests that need projectInfo")]
        public void TestBuildProcessor()
        {
            string filePath = Path.GetFullPath(@"../MsBuildTestApps/MsBuildTestAppSolution/ModelTypesTestLibrary/ModelTypesTestLibrary.csproj");
            MsBuilder<ScaffoldingBuildProcessor> builder = new MsBuilder<ScaffoldingBuildProcessor>(filePath, new ScaffoldingBuildProcessor());
            
            builder.RunMsBuild(FrameworkConstants.CommonFrameworks.NetCoreApp10);
            Assert.NotNull(builder.BuildProcessor.Packages);
            Assert.NotNull(builder.BuildProcessor.ResolvedReferences);

        }
    }
}
