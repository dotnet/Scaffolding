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
    public class MsBuildProjectContext : IProjectContext
    {

        public MsBuildProjectContext(
            string assemblyFullPath,
            string assemblyName,
            IEnumerable<ResolvedReference> compilationAssemblies,
            IEnumerable<string> compilationItems,
            string config,
            string configuration,
            string depsJson,
            IEnumerable<string> embededItems,
            bool isClassLibrary,
            IEnumerable<DependencyDescription> packageDependencies,
            string packageLockFile,
            string packagesDirectory,
            string platform,
            string projectFullPath,
            string projectName,
            IEnumerable<string> projectReferences,
            string rootNamespace,
            string runtimeConfigJson,
            string targetDirectory,
            NuGetFramework targetFramework)
        {
            Requires.NotNullOrEmpty(assemblyFullPath, nameof(assemblyFullPath));
            Requires.NotNullOrEmpty(assemblyName, nameof(assemblyName));
            Requires.NotNull(compilationAssemblies, nameof(compilationAssemblies));
            Requires.NotNull(compilationItems, nameof(compilationItems));
            Requires.NotNullOrEmpty(config, nameof(config));
            Requires.NotNullOrEmpty(configuration, nameof(configuration));
            Requires.NotNullOrEmpty(depsJson, nameof(depsJson));
            Requires.NotNull(embededItems, nameof(embededItems));
            Requires.NotNull(packageDependencies, nameof(packageDependencies));
            Requires.NotNullOrEmpty(packageLockFile, nameof(packageLockFile));
            Requires.NotNullOrEmpty(packagesDirectory, nameof(packagesDirectory));
            Requires.NotNullOrEmpty(platform, nameof(platform));
            Requires.NotNullOrEmpty(projectFullPath, nameof(projectFullPath));
            Requires.NotNullOrEmpty(projectName, nameof(projectName));
            Requires.NotNull(projectReferences, nameof(projectReferences));
            Requires.NotNullOrEmpty(rootNamespace, nameof(rootNamespace));
            Requires.NotNullOrEmpty(runtimeConfigJson, nameof(runtimeConfigJson));
            Requires.NotNullOrEmpty(targetDirectory, nameof(targetDirectory));
            Requires.NotNull(targetFramework, nameof(targetFramework));

            AssemblyFullPath = assemblyFullPath;
            AssemblyName = assemblyName;
            CompilationAssemblies = compilationAssemblies;
            CompilationItems = compilationItems;
            Config = config;
            Configuration = configuration;
            DepsJson = depsJson;
            EmbededItems = embededItems;
            IsClassLibrary = isClassLibrary;
            PackageDependencies = packageDependencies;
            PackageLockFile = packageLockFile;
            PackagesDirectory = packagesDirectory;
            Platform = platform;
            ProjectFullPath = projectFullPath;
            ProjectName = projectName;
            ProjectReferences = projectReferences;
            RootNamespace = rootNamespace;
            RuntimeConfigJson = runtimeConfigJson;
            TargetDirectory = targetDirectory;
            TargetFramework = targetFramework;
        }
        public string AssemblyFullPath { get; }

        public string AssemblyName { get; }

        public IEnumerable<ResolvedReference> CompilationAssemblies { get; }

        public IEnumerable<string> CompilationItems { get; }

        public string Config { get; }

        public string Configuration { get; }

        public string DepsJson { get; }

        public IEnumerable<string> EmbededItems { get; }

        public bool IsClassLibrary { get; }

        public IEnumerable<DependencyDescription> PackageDependencies { get; }

        public string PackageLockFile { get; }

        public string PackagesDirectory { get; }

        public string Platform { get; }

        public string ProjectFullPath { get; }

        public string ProjectName { get; }

        public IEnumerable<string> ProjectReferences { get; }

        public string RootNamespace { get; }

        public string RuntimeConfigJson { get; }

        public string TargetDirectory { get; }

        public NuGetFramework TargetFramework { get; }

        public string FindProperty(string propertyName)
        {
            throw new NotImplementedException();
        }
    }
}
