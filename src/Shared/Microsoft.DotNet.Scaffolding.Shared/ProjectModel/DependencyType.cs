// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
