// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using NuGet.Frameworks;

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
