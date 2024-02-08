// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.DotNet.Scaffolding.Shared.Project;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier
{
    //static class helper for CodeAnalysis 
    public static class CodeAnalysisHelper
    {
        //helps create a CodeAnalysis.Project with project files given a project path.
        public static CodeAnalysis.Project LoadCodeAnalysisProject(
            string projectFilePath,
            IEnumerable<string> files)
        {
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject(Path.GetFileName(projectFilePath), "C#");
            var projectWithFiles = project.WithAllSourceFiles(files);
            project = projectWithFiles ?? project;
            return project;
        }
    }
}
