// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.Extensions.ProjectModel;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class TestBase
    {
        protected IProjectContext GetProjectContext(string path, bool isMsBuild)
        {
            if (isMsBuild)
            {
                /**
                 * To Make this work, the test project needs to take a dependency on
                 * 'Microsoft.VisualStudio.Web.CodeGeneration.Tools' package. That way when NuGet restores
                 * the project, it will include the targets in the project automatically.
                 */
                var codeGenerationTargetLocation = "Dummy";
                // TODO: Need to include the build task and target
                return new MsBuildProjectContextBuilder(path, codeGenerationTargetLocation)
                    .Build();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
