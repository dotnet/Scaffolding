// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.ProjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.CodeGeneration.Sources.Test
{
    public class TestBase
    {
        protected ProjectContext _projectContext;
        protected string _projectPath;
        public TestBase(string projectPath)
        {
            _projectPath = projectPath;
            //TODO : how to decide which framework to use? 
            _projectContext = ProjectContext.CreateContextForEachFramework(_projectPath).First();
        }
    }
}
