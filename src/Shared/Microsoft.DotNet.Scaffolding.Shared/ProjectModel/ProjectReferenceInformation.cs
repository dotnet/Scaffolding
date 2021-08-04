// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Scaffolding.Shared.ProjectModel
{
    /// <summary>
    /// Information of the project reference including full path, assembly name, etc.
    /// </summary>
    public class ProjectReferenceInformation
    {
        /// <summary>
        /// Full path to the csproj file of the project.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Name of the project reference.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Assembly name for the project reference.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Items included in the project reference for compilation
        /// &lt;Compile Include="" /&gt;
        /// </summary>
        public IEnumerable<string> CompilationItems { get; set; }
    }
}
