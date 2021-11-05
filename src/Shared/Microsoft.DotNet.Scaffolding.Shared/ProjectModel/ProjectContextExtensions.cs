// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.Web.CodeGeneration.Msbuild;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.DotNet.Scaffolding.Shared.ProjectModel
{
    public static class ProjectContextExtensions
    {
        public static DependencyDescription GetPackage(this IProjectContext context, string name)
        {
            Requires.NotNullOrEmpty(name, nameof(name));
            Requires.NotNull(context, nameof(context));

            return context.PackageDependencies.FirstOrDefault(package => package.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<DependencyDescription> GetReferencingPackages(this IProjectContext context, string name)
        {
            Requires.NotNullOrEmpty(name, nameof(name));
            Requires.NotNull(context, nameof(context));

            return context
                .PackageDependencies
                .Where(package => package
                    .Dependencies
                    .Any(dep => dep.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }
        public static IProjectContext AddPackageDependencies(this IProjectContext projectInformation, string projectAssetsFile)
        {
            //get project assets file
            var packageDependencies = ProjectContextWriter.GetPackageDependencies(projectAssetsFile, projectInformation.TargetFramework, projectInformation.TargetFrameworkMoniker);
            var additionalCompilation = ProjectContextWriter.GetScaffoldingAssemblies(packageDependencies).ToList();
            var compilationList = projectInformation.CompilationAssemblies.ToList();
            compilationList.AddRange(additionalCompilation);
            var newProjectContext = new CommonProjectContext()
            {
                AssemblyFullPath = projectInformation.AssemblyFullPath,
                AssemblyName = projectInformation.AssemblyName,
                CompilationAssemblies = compilationList,
                CompilationItems = projectInformation.CompilationItems,
                PackageDependencies = packageDependencies,
                Config = projectInformation.Config,
                Configuration = projectInformation.Configuration,
                DepsFile = projectInformation.DepsFile,
                EmbededItems = projectInformation.EmbededItems,
                IsClassLibrary = projectInformation.IsClassLibrary,
                Platform = projectInformation.Platform,
                ProjectFullPath = projectInformation.ProjectFullPath,
                ProjectName = projectInformation.ProjectName,
                ProjectReferences = projectInformation.ProjectReferences,
                RootNamespace = projectInformation.RootNamespace,
                RuntimeConfig = projectInformation.RuntimeConfig,
                TargetDirectory = projectInformation.TargetDirectory,
                TargetFramework = projectInformation.TargetFramework,
                TargetFrameworkMoniker = projectInformation.TargetFrameworkMoniker,
                GeneratedImplicitNamespaceImportFile = projectInformation.GeneratedImplicitNamespaceImportFile
            };
            return newProjectContext;
        }

    }
}
