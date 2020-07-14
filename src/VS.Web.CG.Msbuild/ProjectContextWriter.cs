// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Msbuild
{
    public class ProjectContextWriter : Build.Utilities.Task
    {
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
        #endregion

        public override bool Execute()
        {
            Debugger.Launch();
            var msBuildContext = new CommonProjectContext()
            {
                AssemblyFullPath = this.AssemblyFullPath,
                AssemblyName = string.IsNullOrEmpty(this.AssemblyName) ? Path.GetFileName(this.AssemblyFullPath) : this.AssemblyName,
                CompilationAssemblies = GetCompilationAssemblies(this.ResolvedReferences),
                CompilationItems = this.CompilationItems.Select(i => i.ItemSpec),
                PackageDependencies = GetPackageDependencies(this.ProjectAssetsFile),
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
                TargetFramework = this.TargetFramework
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

        internal IEnumerable<DependencyDescription> GetPackageDependencies(string projectAssetsFile)
        {
            IList<DependencyDescription> packageDependencies = new List<DependencyDescription>();
            if (!string.IsNullOrEmpty(projectAssetsFile) && File.Exists(projectAssetsFile) && !string.IsNullOrEmpty(TargetFramework))
            {
                //target framework moniker for the current project. We use this to get all targets for said moniker.
                var targetFrameworkMoniker = NuGetFramework.Parse(TargetFramework)?.DotNetFrameworkName;
                string json = File.ReadAllText(projectAssetsFile);
                if (!string.IsNullOrEmpty(json) && !string.IsNullOrEmpty(targetFrameworkMoniker))
                {
                    try
                    {
                        JsonDocument baseDocument = JsonDocument.Parse(json);
                        if (baseDocument != null)
                        {
                            JsonElement root = baseDocument.RootElement;
                            //"targets" gives us all top-level and transitive dependencies. "packageFolders" gives us the path where the dependencies are on disk.
                            if (root.TryGetProperty(TargetsProperty, out var targets) && root.TryGetProperty(PackageFoldersProperty, out var packageFolderPath))
                            {
                                if (targets.TryGetProperty(targetFrameworkMoniker, out var packages))
                                {
                                    var packagesEnumerator = packages.EnumerateObject();
                                    //populate are our own List<DependencyDescription> of all the dependencies we find.
                                    foreach (var package in packagesEnumerator)
                                    {
                                        var fullName = package.Name;
                                        //get default nuget package path.
                                        var path = packageFolderPath.EnumerateObject().Any() ? packageFolderPath.EnumerateObject().First().Name : string.Empty;
                                        if (!string.IsNullOrEmpty(fullName) && !string.IsNullOrEmpty(path) && package.Value.TryGetProperty(TypeProperty, out var type))
                                        {
                                            //fullName is in the format {Package Name}/{Version} for example "System.Text.MoreText/2.1.1" Split into tuple. 
                                            Tuple<string, string> nameAndVersion = GetName(fullName);
                                            if (nameAndVersion != null)
                                            {
                                                var dependencyTypeValue = type.ToString();
                                                var DependencyTypeEnum = DependencyType.Unknown;
                                                if (Enum.TryParse(typeof(DependencyType), dependencyTypeValue, true, out var dependencyType))
                                                {
                                                    DependencyTypeEnum = (DependencyType)dependencyType;
                                                }
                                                
                                                DependencyDescription dependency = new DependencyDescription(nameAndVersion.Item1,
                                                                                                             nameAndVersion.Item2,
                                                                                                             GetPath(path, nameAndVersion),
                                                                                                             targetFrameworkMoniker,
                                                                                                             DependencyTypeEnum,
                                                                                                             true);
                                                if (package.Value.TryGetProperty(DependencyProperty, out var dependencies))
                                                {
                                                    var dependenciesList = dependencies.EnumerateObject();
                                                    //Add all transitive dependencies
                                                    foreach (var dep in dependenciesList)
                                                    {
                                                        if (!string.IsNullOrEmpty(dep.Name))
                                                        {
                                                            Dependency transitiveDependency = new Dependency(dep.Name, dep.Value.ToString());
                                                            dependency.AddDependency(transitiveDependency);
                                                        }
                                                    }
                                                }
                                                packageDependencies.Add(dependency);
                                            } 
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        Debug.Assert(false, "Completely empty json.");
                    }
                }
            }

            return packageDependencies;
        }


        internal string GetPath(string nugetPath, Tuple<string, string> nameAndVersion)
        {
            if (!string.IsNullOrEmpty(nugetPath))
            {
                string path = Path.Combine(nugetPath, nameAndVersion.Item1, nameAndVersion.Item2);
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        private Tuple<string, string> GetName(string fullName)
        {
            Tuple<string, string> nameAndVersion = null;
            if (!string.IsNullOrEmpty(fullName))
            {
                string[] splitName = fullName.Split("/");
                if (splitName.Length  > 1)
                {
                    nameAndVersion = new Tuple<string, string>(splitName[0], splitName[1]);
                }
            }
            return nameAndVersion;
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
