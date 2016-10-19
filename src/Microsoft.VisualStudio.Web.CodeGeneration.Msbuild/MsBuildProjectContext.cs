// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.ProjectModel;
using Microsoft.Extensions.ProjectModel.Resolution;
using NuGet.Frameworks;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Msbuild
{
    public class MsBuildProjectContext
    {
        public MsBuildProjectContext(
            string assemblyFullPath,
            string assemblyName,
            IEnumerable<ResolvedReference> compilationAssemblies,
            IEnumerable<string> compilationItems,
            string config,
            string configuration,
            IEnumerable<string> embededItems,
            bool isClassLibrary,
            IEnumerable<DependencyDescription> packageDependencies,
            string packagesDirectory,
            string platform,
            string projectFullPath,
            string projectName,
            IEnumerable<string> projectReferences,
            string rootNamespace,
            string targetDirectory,
            string targetFramework)
        {
            Requires.NotNullOrEmpty(assemblyFullPath, nameof(assemblyFullPath));
            Requires.NotNullOrEmpty(assemblyName, nameof(assemblyName));
            Requires.NotNull(compilationAssemblies, nameof(compilationAssemblies));
            Requires.NotNull(compilationItems, nameof(compilationItems));
            Requires.NotNullOrEmpty(config, nameof(config));
            Requires.NotNullOrEmpty(configuration, nameof(configuration));
            Requires.NotNull(embededItems, nameof(embededItems));
            Requires.NotNull(packageDependencies, nameof(packageDependencies));
            Requires.NotNullOrEmpty(packagesDirectory, nameof(packagesDirectory));
            Requires.NotNullOrEmpty(platform, nameof(platform));
            Requires.NotNullOrEmpty(projectFullPath, nameof(projectFullPath));
            Requires.NotNullOrEmpty(projectName, nameof(projectName));
            Requires.NotNull(projectReferences, nameof(projectReferences));
            Requires.NotNullOrEmpty(rootNamespace, nameof(rootNamespace));
            Requires.NotNullOrEmpty(targetDirectory, nameof(targetDirectory));
            Requires.NotNullOrEmpty(targetFramework, nameof(targetFramework));

            AssemblyFullPath = assemblyFullPath;
            AssemblyName = assemblyName;
            CompilationAssemblies = compilationAssemblies;
            CompilationItems = compilationItems;
            Config = config;
            Configuration = configuration;
            EmbededItems = embededItems;
            IsClassLibrary = isClassLibrary;
            PackageDependencies = packageDependencies;
            PackagesDirectory = packagesDirectory;
            Platform = platform;
            ProjectFullPath = projectFullPath;
            ProjectName = projectName;
            ProjectReferences = projectReferences;
            RootNamespace = rootNamespace;
            TargetDirectory = targetDirectory;
            TargetFramework = targetFramework;
        }
        public string AssemblyFullPath { get; }

        public string AssemblyName { get; }

        public IEnumerable<ResolvedReference> CompilationAssemblies { get; }

        public IEnumerable<string> CompilationItems { get; }

        public string Config { get; }

        public string Configuration { get; }

        public IEnumerable<string> EmbededItems { get; }

        public bool IsClassLibrary { get; }

        public IEnumerable<DependencyDescription> PackageDependencies { get; }

        public string PackagesDirectory { get; }

        public string Platform { get; }

        public string ProjectFullPath { get; }

        public string ProjectName { get; }

        public IEnumerable<ProjectReferenceInformation> ProjectReferenceInformation { get; set; }

        public IEnumerable<string> ProjectReferences { get; }

        public string RootNamespace { get; }

        public string TargetDirectory { get; }

        public string TargetFramework { get; }
    }
}
