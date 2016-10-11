// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class TestBase
    {
        protected IProjectContext GetProjectContext(string path, bool isMsBuild)
        {
            if (isMsBuild)
            {
                return new MsBuildProjectContextBuilder()
                    .AsDesignTimeBuild()
                    .WithTargetFramework(FrameworkConstants.CommonFrameworks.NetCoreApp10)
                    .WithConfiguration("Debug")
                    .Build();
            }
            else
            {
                var context = Microsoft.DotNet.ProjectModel.ProjectContext.Create(path, FrameworkConstants.CommonFrameworks.NetStandard16);
                return new Microsoft.Extensions.ProjectModel.DotNetProjectContext(context, "Debug", null);
            }
        }
    }
}
