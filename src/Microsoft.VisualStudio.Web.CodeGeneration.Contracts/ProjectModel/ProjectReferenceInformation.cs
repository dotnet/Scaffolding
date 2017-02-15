// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel
{
    public class ProjectReferenceInformation
    {
        public string FullPath { get; set; }
        public string ProjectName { get; set; }
        public string AssemblyName { get; set; }
        public IEnumerable<string> CompilationItems { get; set; }
    }
}
