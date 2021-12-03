// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Msbuild
{
    public class ProjectContextWriter : Build.Utilities.Task
    {
        private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
        private const string AspNetCoreIdentity = "Microsoft.AspNetCore.Identity";

        private const string TargetsProperty = "targets";
        private const string PackageFoldersProperty = "packageFolders";
        private const string DependencyProperty = "dependencies";
        private const string TypeProperty = "type";
        #region Inputs
        [Build.Framework.Required]
        public string OutputFile { get; set; }

        [Build.Framework.Required]
        public ITaskItem[] ResolvedReferences { get; set; }


        [Build.Framework.Required]
        public ITaskItem[] ProjectReferences { get; set; }

        [Build.Framework.Required]
        public ITaskItem[] CompilationItems { get; set; }

        [Build.Framework.Required]
        public ITaskItem[] EmbeddedItems { get; set; }

        [Build.Framework.Required]
        public string TargetFramework { get; set; }

        [Build.Framework.Required]
        public string TargetFrameworkMoniker { get; set; }

        [Build.Framework.Required]
        public string Name { get; set; }

        [Build.Framework.Required]
        public string OutputType { get; set; }

        [Build.Framework.Required]
        public string Platform { get; set; }

        [Build.Framework.Required]
        public string TargetDirectory { get; set; }

        [Build.Framework.Required]
        public string RootNamespace { get; set; }

        [Build.Framework.Required]
        public string AssemblyFullPath { get; set; }

        public string AssemblyName { get; set; }

        [Build.Framework.Required]
        public string Configuration { get; set; }

        [Build.Framework.Required]
        public string ProjectFullPath { get; set; }

        [Build.Framework.Required]
        public string ProjectRuntimeConfigFileName { get; set; }

        [Build.Framework.Required]
        public string ProjectDepsFileName { get; set; }

        [Build.Framework.Required]
        public string ProjectAssetsFile { get; set; }

        //not required as it might not get a value (fails if required and value not present).
        public string GeneratedImplicitNamespaceImportFile { get; set; }
        #endregion

        public override bool Execute()
        {
            var msBuildContext = new CommonProjectContext()
            {
                AssemblyFullPath = this.AssemblyFullPath,
                AssemblyName = string.IsNullOrEmpty(this.AssemblyName) ? Path.GetFileName(this.AssemblyFullPath) : this.AssemblyName,
                CompilationAssemblies = GetCompilationAssemblies(this.ResolvedReferences),
                CompilationItems = this.CompilationItems.Select(i => i.ItemSpec),
                PackageDependencies = ProjectContextHelper.GetPackageDependencies(this.ProjectAssetsFile, this.TargetFramework, this.TargetFrameworkMoniker),
                Config = this.AssemblyFullPath + ".config",
                Configuration = this.Configuration,
                DepsFile = this.ProjectDepsFileName,
                EmbededItems = this.EmbeddedItems.Select(i => i.ItemSpec),
                IsClassLibrary = "Library".Equals(this.OutputType, StringComparison.OrdinalIgnoreCase),
                Platform = this.Platform,
                ProjectFullPath = this.ProjectFullPath,
                ProjectName = this.Name,
                ProjectReferences = this.ProjectReferences.Select(i => i.ItemSpec),
                RootNamespace = this.RootNamespace,
                RuntimeConfig = this.ProjectRuntimeConfigFileName,
                TargetDirectory = this.TargetDirectory,
                TargetFramework = this.TargetFramework,
                TargetFrameworkMoniker = this.TargetFrameworkMoniker, 
                GeneratedImplicitNamespaceImportFile = this.GeneratedImplicitNamespaceImportFile
            };

            var projectReferences = msBuildContext.ProjectReferences;
            var projReferenceInformation = GetProjectDependency(projectReferences, msBuildContext.ProjectFullPath);
            msBuildContext.ProjectReferenceInformation = projReferenceInformation;
            using(var streamWriter = new StreamWriter(File.Create(OutputFile)))
            {
                var json = JsonSerializer.Serialize(msBuildContext);
                streamWriter.Write(json);
            }

            return true;
        }

        private IEnumerable<ProjectReferenceInformation> GetProjectDependency(
            IEnumerable<string> projectReferences,
            string rootProjectFullpath)
        {
            return ProjectReferenceInformationProvider.GetProjectReferenceInformation(
                rootProjectFullpath,
                projectReferences);
        }

        private IEnumerable<ResolvedReference> GetCompilationAssemblies(ITaskItem[] resolvedReferences)
        {
            Requires.NotNull(resolvedReferences, nameof(resolvedReferences));
            var compilationAssemblies = new List<ResolvedReference>();
            foreach (var item in resolvedReferences)
            {
                var resolvedPath = item.ItemSpec;
                var name = Path.GetFileName(resolvedPath);
                var reference = new ResolvedReference(name, resolvedPath);
                compilationAssemblies.Add(reference);
            }

            return compilationAssemblies;
        }
    }
}
