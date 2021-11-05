// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Build.Framework;
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
                PackageDependencies = GetPackageDependencies(this.ProjectAssetsFile, this.TargetFramework, this.TargetFrameworkMoniker),
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

        internal static IEnumerable<DependencyDescription> GetPackageDependencies(string projectAssetsFile, string tfm, string tfmMoniker)
        {
            IList<DependencyDescription> packageDependencies = new List<DependencyDescription>();
            if (!string.IsNullOrEmpty(projectAssetsFile) && File.Exists(projectAssetsFile) && !string.IsNullOrEmpty(tfm))
            {
                //target framework moniker for the current project. We use this to get all targets for said moniker.
                var targetFramework = tfm;
                var targetFrameworkMoniker = tfmMoniker;
                if (string.IsNullOrEmpty(targetFrameworkMoniker))
                {
                    //if targetFrameworkMoniker is null, targetFramework is the TargetFrameworkMoniker (issue w/ IProjectContext sent from VS)
                    targetFrameworkMoniker = tfm;
                    targetFramework = ProjectModelHelper.GetShortTfm(tfm);
                }
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
                                if (targets.TryGetProperty(targetFramework, out var packages))
                                {
                                    packageDependencies = DeserializePackages(packages, packageFolderPath, targetFrameworkMoniker);
                                }
                                else if(targets.TryGetProperty(targetFrameworkMoniker, out var legacyPackages))
                                {
                                    packageDependencies = DeserializePackages(legacyPackages, packageFolderPath, targetFrameworkMoniker);
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

        internal static IEnumerable<ResolvedReference> GetScaffoldingAssemblies(IEnumerable<DependencyDescription> dependencies)
        {
            var compilationAssemblies = new List<ResolvedReference>();
            foreach (var item in dependencies)
            {
                //only add Microsoft.EntityFrameworkCore.* or Microsoft.AspNetCore.Identity.* assemblies. Any others might be duplicates which cause in-memory compilation errors and those are the assemblies we care about.
                if (item.Name.Contains(EntityFrameworkCore, StringComparison.OrdinalIgnoreCase) || item.Name.Contains(AspNetCoreIdentity, StringComparison.OrdinalIgnoreCase))
                {
                    var name = $"{item.Name}.dll";
                    //costly search but we're only doing it a handful of times.
                    var file = Directory.GetFiles(item.Path, name, SearchOption.AllDirectories).FirstOrDefault();
                    if (file != null)
                    {
                        var resolvedPath = file.ToString();
                        var reference = new ResolvedReference(name, resolvedPath);
                        compilationAssemblies.Add(reference);
                    }
                }
            }
            return compilationAssemblies;
        }

        private static IList<DependencyDescription> DeserializePackages(JsonElement packages, JsonElement packageFolderPath, string targetFrameworkMoniker)
        {
            IList<DependencyDescription> packageDependencies = new List<DependencyDescription>();
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
                    Tuple<string, string> nameAndVersion = ProjectContextWriter.GetName(fullName);
                    if (nameAndVersion != null)
                    {
                        var dependencyTypeValue = type.ToString();
                        var DependencyTypeEnum = DependencyType.Unknown;
                        if (Enum.TryParse(typeof(DependencyType), dependencyTypeValue, true, out var dependencyType))
                        {
                            DependencyTypeEnum = (DependencyType)dependencyType;
                        }

                        string packagePath = ProjectContextWriter.GetPath(path, nameAndVersion);
                        DependencyDescription dependency = new DependencyDescription(nameAndVersion.Item1,
                                                                                        nameAndVersion.Item2,
                                                                                        Directory.Exists(packagePath) ? packagePath : string.Empty,
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

            return packageDependencies;
        }

        internal static string GetPath(string nugetPath, Tuple<string, string> nameAndVersion)
        {
            string path = string.Empty;
            if (!string.IsNullOrEmpty(nugetPath) && !string.IsNullOrEmpty(nameAndVersion.Item1) && !string.IsNullOrEmpty(nameAndVersion.Item2))
            {
                path = Path.Combine(nugetPath, nameAndVersion.Item1, nameAndVersion.Item2);
                path =  Directory.Exists(path) ? path : Path.Combine(nugetPath, nameAndVersion.Item1.ToLower(), nameAndVersion.Item2);
            }

            return path;
        }

        private static Tuple<string, string> GetName(string fullName)
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
