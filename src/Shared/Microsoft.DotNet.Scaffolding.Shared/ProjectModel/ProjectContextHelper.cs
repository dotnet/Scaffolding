// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.DotNet.Scaffolding.Shared
{
    public static class ProjectContextHelper
    {
        private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
        private const string AspNetCoreIdentity = "Microsoft.AspNetCore.Identity";

        private const string TargetsProperty = "targets";
        private const string PackageFoldersProperty = "packageFolders";
        private const string DependencyProperty = "dependencies";
        private const string TypeProperty = "type";

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
                                if (targetFramework.EndsWith("-windows"))
                                {
                                    //tfm in project.assets.json is netX-windows7.0 instead of netX-windows
                                    targetFramework = $"{targetFramework}7.0";
                                }

                                if (targets.TryGetProperty(targetFramework, out var packages))
                                {
                                    packageDependencies = DeserializePackages(packages, packageFolderPath, targetFrameworkMoniker);
                                }
                                else if (targets.TryGetProperty(targetFrameworkMoniker, out var legacyPackages))
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

        internal static string GetPath(string nugetPath, Tuple<string, string> nameAndVersion)
        {
            string path = string.Empty;
            if (!string.IsNullOrEmpty(nugetPath) && !string.IsNullOrEmpty(nameAndVersion.Item1) && !string.IsNullOrEmpty(nameAndVersion.Item2))
            {
                path = Path.Combine(nugetPath, nameAndVersion.Item1, nameAndVersion.Item2);
                path = Directory.Exists(path) ? path : Path.Combine(nugetPath, nameAndVersion.Item1.ToLower(), nameAndVersion.Item2);
            }

            return path;
        }

        internal static Tuple<string, string> GetName(string fullName)
        {
            Tuple<string, string> nameAndVersion = null;
            if (!string.IsNullOrEmpty(fullName))
            {
                string[] splitName = fullName.Split("/");
                if (splitName.Length > 1)
                {
                    nameAndVersion = new Tuple<string, string>(splitName[0], splitName[1]);
                }
            }
            return nameAndVersion;
        }

        internal static IList<DependencyDescription> DeserializePackages(JsonElement packages, JsonElement packageFolderPath, string targetFrameworkMoniker)
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
                    Tuple<string, string> nameAndVersion = GetName(fullName);
                    if (nameAndVersion != null)
                    {
                        var dependencyTypeValue = type.ToString();
                        var DependencyTypeEnum = DependencyType.Unknown;
                        if (Enum.TryParse(typeof(DependencyType), dependencyTypeValue, true, out var dependencyType))
                        {
                            DependencyTypeEnum = (DependencyType)dependencyType;
                        }

                        string packagePath = GetPath(path, nameAndVersion);
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

        /// <summary>
        /// Returns the value given a key (aka a xml tag) from the given xml file
        /// </summary>
        /// <param name="variableKey">variable key or tags in the csproj file</tag></param>
        /// <param name="xmlText">xml file text (mostly used for parsing csproj file)</param>
        /// <returns>empty or value from the parsing the elements of the xml file.</returns>
        internal static string GetXmlKeyValue(string variableKey, string xmlText)
        {
            string variableValue = string.Empty;
            if (!string.IsNullOrEmpty(xmlText) && !string.IsNullOrEmpty(variableKey))
            {
                //use XDocument to get all csproj elements.
                XDocument document = XDocument.Parse(xmlText);
                var docNodes = document.Root?.Elements();
                var allElements = docNodes?.SelectMany(x => x.Elements());
                if (allElements != null && allElements.Any())
                {
                    var varValueElement = allElements.FirstOrDefault(e => e.Name.LocalName.Equals(variableKey, StringComparison.OrdinalIgnoreCase));
                    if (varValueElement != null)
                    {
                        variableValue = varValueElement.Value;
                    }
                }
            }
            return variableValue;
        }
    }
}
