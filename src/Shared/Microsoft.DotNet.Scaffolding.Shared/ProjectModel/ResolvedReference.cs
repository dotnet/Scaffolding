// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Shared.ProjectModel
{
    /// <summary>
    /// Information about a resolved reference of the project.
    /// The reference could be from a NuGet package, assembly ref etc.
    /// </summary>
    public class ResolvedReference
    {
        /// <summary/>
        /// <param name="name">Name of the referenced assembly.</param>
        /// <param name="resolvedPath">Full path of the referenced assembly.</param>
        public ResolvedReference(string name, string resolvedPath)
        {
            Name = name;
            ResolvedPath = resolvedPath;
        }

        /// <summary>
        /// Full path of the referenced assembly.
        /// </summary>
        public string ResolvedPath { get; }

        /// <summary>
        /// Name of the referenced assembly.
        /// </summary>
        public string Name { get; }
    }
}
