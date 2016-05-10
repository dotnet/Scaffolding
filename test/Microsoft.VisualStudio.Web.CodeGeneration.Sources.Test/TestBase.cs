// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    public class TestBase
    {
#if NET451
        NuGetFramework framework = FrameworkConstants.CommonFrameworks.Net451;
#else
        NuGetFramework framework = FrameworkConstants.CommonFrameworks.NetStandard15;
#endif
        protected ProjectContext _projectContext;
        protected string _projectPath;
        protected IApplicationInfo _applicationInfo;
        
        public TestBase(string projectPath)
        {
            _projectPath = projectPath;

            _projectContext = new ProjectContextBuilder().WithProjectDirectory(_projectPath).WithTargetFramework(framework).Build();

            
#if RELEASE
            _applicationInfo = new ApplicationInfo("ModelTypesLocatorTestClassLibrary", _projectPath, "Release");
#else
            _applicationInfo = new ApplicationInfo("ModelTypesLocatorTestClassLibrary", _projectPath, "Debug");
#endif
        }
    }
}
