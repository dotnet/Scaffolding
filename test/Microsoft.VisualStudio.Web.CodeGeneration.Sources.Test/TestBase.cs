// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.ProjectModel;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class TestBase
    {
        protected IProjectContext GetProjectContext(string path, bool isMsBuild)
        {
            if (isMsBuild)
            {
                return new MsBuildProjectContextBuilder(path, FrameworkConstants.CommonFrameworks.NetCoreApp10)
                    .Build();
            }
            else
            {
                var context = DotNetProjectContextBuilder.Create(path, FrameworkConstants.CommonFrameworks.NetStandard16);
                return new Microsoft.Extensions.ProjectModel.DotNetProjectContext(context, "Debug", null, null);
            }
        }
    }
}
