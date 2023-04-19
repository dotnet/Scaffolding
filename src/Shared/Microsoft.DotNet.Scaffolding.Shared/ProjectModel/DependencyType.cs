// Copyright (c) .NET Foundation. All rights reserved.

namespace Microsoft.DotNet.Scaffolding.Shared.ProjectModel
{
    /// <summary>
    /// Types of dependencies.
    /// </summary>
    public enum DependencyType
    {
        /// <summary>
        /// Represents the Target secion in the project.assets.json.
        /// (The dependencies of this type of dependency are the direct dependencies
        /// of the project.)
        /// </summary>
        Target,

        /// <summary>
        /// NuGet package dependency.
        /// </summary>
        Package,

        /// <summary>
        /// Assembly reference.
        /// </summary>
        Assembly,

        /// <summary>
        /// Project Reference.
        /// </summary>
        Project,

        /// <summary>
        /// Analyzer Assembly reference.
        /// </summary>
        AnalyzerAssembly,

        /// <summary>
        /// Unknown reference type.
        /// </summary>
        Unknown
    }
}
