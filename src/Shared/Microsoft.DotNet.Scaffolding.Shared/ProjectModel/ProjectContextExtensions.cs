// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var packageDependencies = ProjectContextHelper.GetPackageDependencies(projectAssetsFile, projectInformation.TargetFramework, projectInformation.TargetFrameworkMoniker);
            var additionalCompilation = ProjectContextHelper.GetScaffoldingAssemblies(packageDependencies).ToList();
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
                GeneratedImplicitNamespaceImportFile = projectInformation.GeneratedImplicitNamespaceImportFile,
                Nullable = projectInformation.Nullable,
                ProjectReferenceInformation = projectInformation.ProjectReferenceInformation
            };
            return newProjectContext;
        }

        /// <summary>
        /// Given an IProjectContext, check if IProjectContext.Nullable is set, if it is, no changes necessary.
        /// If not already there, set using ProjectContextHelper.GetXmlKeyValue (parsing the csproj xml file).
        /// </summary>
        /// <param name="context">IProjectContext which has csproj path, and the Nullable variable to set.</param>
        /// <returns>modified IProjectContext with the Nullable property set or the same IProjectContext as passed.</returns>
        public static IProjectContext CheckNullableVariable(this IProjectContext context)
        {
            //if nullable is not empty, return current IProjectContext as is.
            if (context != null && string.IsNullOrEmpty(context.Nullable))
            {
                string csprojText = System.IO.File.ReadAllText(context.ProjectFullPath);
                string nullableVarValue = ProjectContextHelper.GetXmlKeyValue("nullable", csprojText);
                if (!string.IsNullOrEmpty(nullableVarValue))
                {
                    if (context is CommonProjectContext newProjectContext)
                    {
                        newProjectContext.Nullable = nullableVarValue;
                        return newProjectContext;
                    }
                }
            }
            return context;
        }

        /// <summary>
        /// a very simple check for WebApplication.AddRazorComponents()
        /// workaround for when 'ProjectCapability' msbuild item not initialized.
        /// </summary>
        public static bool IsBlazorWebProject(this IProjectContext context)
        {
            var programCsFile = context.CompilationItems.FirstOrDefault(x => x.EndsWith("Program.cs"));
            if (!string.IsNullOrEmpty(programCsFile))
            {
                var programCsFilePath = Path.IsPathRooted(programCsFile)
                ? programCsFile
                : Path.Combine(Path.GetDirectoryName(context.ProjectFullPath), programCsFile);

                string programCsText = File.ReadAllText(programCsFilePath);
                return programCsText.Contains("AddRazorComponents");
            }
           
            return false;
        }
    }
}
