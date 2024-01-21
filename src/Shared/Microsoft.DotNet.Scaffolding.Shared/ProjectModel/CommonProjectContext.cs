// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.Scaffolding.Shared.ProjectModel
{
    /// <inheritdoc/>
    public class CommonProjectContext : IProjectContext
    {
        /// <inheritdoc/>
        public string AssemblyFullPath { get; set; }

        /// <inheritdoc/>
        public string AssemblyName { get; set; }

        /// <inheritdoc/>
        public IEnumerable<ResolvedReference> CompilationAssemblies { get; set; }

        /// <inheritdoc/>
        public IEnumerable<string> CompilationItems { get; set; }
        public IEnumerable<string> ProjectCapabilities { get; set; }
        /// <inheritdoc/>
        public string Config { get; set; }

        /// <inheritdoc/>
        public string Configuration { get; set; }

        /// <inheritdoc/>
        public string DepsFile { get; set; }

        /// <inheritdoc/>
        public IEnumerable<string> EmbededItems { get; set; }

        /// <inheritdoc/>
        public bool IsClassLibrary { get; set; }

        /// <inheritdoc/>
        public IEnumerable<DependencyDescription> PackageDependencies { get; set; }

        /// <inheritdoc/>
        public string PackagesDirectory { get; set; }

        /// <inheritdoc/>
        public string Platform { get; set; }

        /// <inheritdoc/>
        public string ProjectFullPath { get; set; }

        /// <inheritdoc/>
        public string ProjectName { get; set; }

        /// <inheritdoc/>
        public IEnumerable<ProjectReferenceInformation> ProjectReferenceInformation { get; set; }

        /// <inheritdoc/>
        public IEnumerable<string> ProjectReferences { get; set; }

        /// <inheritdoc/>
        public string RootNamespace { get; set; }

        /// <inheritdoc/>
        public string RuntimeConfig { get; set; }

        /// <inheritdoc/>
        public string TargetDirectory { get; set; }

        /// <inheritdoc/>
        public string TargetFramework { get; set; }

        public string TargetFrameworkMoniker { get; set; }

        /// <inheritdoc/>
        public string GeneratedImplicitNamespaceImportFile { get; set; }

        public string Nullable { get; set; }
    }
}
