// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        /// Root namespace for the project reference.
        /// </summary>
        public string RootNamespace { get; set; }

        /// <summary>
        /// Whether the project reference imports the Razor SDK.
        /// </summary>
        public bool IsRazorProject { get; set; }

        /// <summary>
        /// Razor component items included in the project reference.
        /// </summary>
        public List<string> RazorComponentItems { get; set; }

        /// <summary>
        /// Items included in the project reference for compilation
        /// &lt;Compile Include="" /&gt;
        /// </summary>
        public List<string> CompilationItems { get; set; }
    }
}
