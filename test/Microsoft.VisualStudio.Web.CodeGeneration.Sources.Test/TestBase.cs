// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class TestBase
    {
        protected TestFixture _fixture;
        protected IMsBuildProjectContext ProjectContext { get; }
        protected IProjectDependencyProvider ProjectDependencyProvider { get; }
        protected IApplicationInfo ApplicationInfo { get; }

        public TestBase(TestFixture testFixture)
        {
            this._fixture = testFixture;

//            _fixture.RunBuild();
//            ProjectContext = _fixture.ProjectInfo.ProjectContext;
//            string configuration = "Debug";
//#if RELEASE
//            configuration = "Release";
//#endif
//            ApplicationInfo = new ApplicationInfo(_fixture.ProjectInfo.ProjectContext.ProjectName,
//                ProjectContext.ProjectFullPath,
//                configuration);


        }
    }
}
