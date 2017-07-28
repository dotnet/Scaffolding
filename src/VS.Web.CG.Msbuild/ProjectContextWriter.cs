// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Msbuild
{
    public class ProjectContextWriter : Build.Utilities.Task
    {
        #region Inputs
        [Build.Framework.Required]
        public string OutputFile { get; set; }

        [Build.Framework.Required]
        public ITaskItem[] ResolvedReferences { get; set; }

        [Build.Framework.Required]
        public ITaskItem[] PackageDependencies { get; set; }

        [Build.Framework.Required]
        public ITaskItem[] ProjectReferences { get; set; }

        [Build.Framework.Required]
        public ITaskItem[] CompilationItems { get; set; }

        [Build.Framework.Required]
        public ITaskItem[] EmbeddedItems { get; set; }

        [Build.Framework.Required]
        public string TargetFramework { get; set; }

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
        #endregion

        public override bool Execute()
        {
            var msBuildContext = new CommonProjectContext()
            {
                AssemblyFullPath = this.AssemblyFullPath,
                AssemblyName = string.IsNullOrEmpty(this.AssemblyName) ? Path.GetFileName(this.AssemblyFullPath) : this.AssemblyName,
                CompilationAssemblies = GetCompilationAssemblies(this.ResolvedReferences),
                CompilationItems = this.CompilationItems.Select(i => i.ItemSpec),
                Config = this.AssemblyFullPath + ".config",
                Configuration = this.Configuration,
                DepsFile = this.ProjectDepsFileName,
                EmbededItems = this.EmbeddedItems.Select(i => i.ItemSpec),
                IsClassLibrary = "Library".Equals(this.OutputType, StringComparison.OrdinalIgnoreCase),
                PackageDependencies = this.GetPackageDependencies(PackageDependencies),
                Platform = this.Platform,
                ProjectFullPath = this.ProjectFullPath,
                ProjectName = this.Name,
                ProjectReferences = this.ProjectReferences.Select(i => i.ItemSpec),
                RootNamespace = this.RootNamespace,
                RuntimeConfig = this.ProjectRuntimeConfigFileName,
                TargetDirectory = this.TargetDirectory,
                TargetFramework = this.TargetFramework
            };

            var projectReferences = msBuildContext.ProjectReferences;
            var projReferenceInformation = GetProjectDependency(projectReferences, msBuildContext.ProjectFullPath);
            msBuildContext.ProjectReferenceInformation = projReferenceInformation;
            using(var streamWriter = new StreamWriter(File.Create(OutputFile)))
            {
                var json = JsonConvert.SerializeObject(msBuildContext);
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

        private IEnumerable<DependencyDescription> GetPackageDependencies(ITaskItem[] packageDependecyItems)
        {
            Requires.NotNull(packageDependecyItems, nameof(packageDependecyItems));
            var packages = packageDependecyItems.Select(item => new { key = item.ItemSpec, value = GetPackageDependency(item) })
                .Where(package => package != null && package.value != null);
            var packageMap = new Dictionary<string, DependencyDescription>(StringComparer.OrdinalIgnoreCase);
            foreach(var package in packages)
            {
                packageMap.Add(package.key, package.value);
            }

            PopulateDependencies(packageMap, packageDependecyItems);

            return packageMap.Values;
        }

        private void PopulateDependencies(Dictionary<string, DependencyDescription> packageMap, ITaskItem[] packageDependecyItems)
        {
            Requires.NotNull(packageMap, nameof(packageMap));
            Requires.NotNull(packageDependecyItems, nameof(packageDependecyItems));
            foreach (var item in packageDependecyItems)
            {
                var depSpecs = item.GetMetadata("Dependencies")
                    ?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                DependencyDescription current = null;
                if (depSpecs == null || !packageMap.TryGetValue(item.ItemSpec, out current))
                {
                    return;
                }

                foreach (var depSpec in depSpecs)
                {
                    var spec = item.ItemSpec.Split('/').FirstOrDefault() +"/"+ depSpec;
                    DependencyDescription d = null;
                    if (packageMap.TryGetValue(spec, out d))
                    {
                        current.AddDependency(new Dependency(d.Name, d.Version));
                    }
                }
            }
        }

        private DependencyDescription GetPackageDependency(ITaskItem item)
        {
            Requires.NotNull(item, nameof(item));

            var type = item.GetMetadata("Type");
            var name = ("Target".Equals(type, StringComparison.OrdinalIgnoreCase))
                ? item.ItemSpec
                : item.GetMetadata("Name");

            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            var version = item.GetMetadata("Version");
            var path = item.GetMetadata("Path");
            var resolved = item.GetMetadata("Resolved");

            var isResolved = false;
            bool.TryParse(resolved, out isResolved);

            var framework = item.ItemSpec.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).First();
            DependencyType dt;
            dt = Enum.TryParse(type, out dt)
                ? dt
                : DependencyType.Unknown;

            return new DependencyDescription(name, version, path, framework, dt, isResolved);
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
