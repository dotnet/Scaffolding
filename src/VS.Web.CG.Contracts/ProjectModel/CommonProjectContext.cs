// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel
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
    }
}
