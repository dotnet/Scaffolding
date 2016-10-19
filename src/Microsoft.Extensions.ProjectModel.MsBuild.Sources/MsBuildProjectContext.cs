// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.ProjectModel.Resolution;
using NuGet.Frameworks;

namespace Microsoft.Extensions.ProjectModel
{
    internal class MsBuildProjectContext : IProjectContext
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
            string targetFramework)
        {

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
            TargetFramework = NuGetFramework.Parse(targetFramework);
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

        public IEnumerable<ProjectReferenceInformation> ProjectReferenceInformation { get; set; }

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
