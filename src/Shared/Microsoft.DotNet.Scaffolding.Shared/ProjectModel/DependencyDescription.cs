// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Scaffolding.Shared.ProjectModel
{
    /// <summary>
    /// Represents a NuGet package dependency of the project.
    /// </summary>
    public class DependencyDescription
    {
        private readonly List<Dependency> _dependencies;

        /// <summary/>
        /// <param name="name">Name of the dependency.</param>
        /// <param name="version">Version of the dependency.</param>
        /// <param name="path">Full path to the dependency.</param>
        /// <param name="targetFramework">TFM of the project to which this dependency belongs.</param>
        /// <param name="type">Type of the dependency. <see cref="DependencyType"/></param>
        /// <param name="resolved">Indicates whether this dependency is resolved on disk or not.</param>
        public DependencyDescription(
            string name, 
            string version, 
            string path, 
            string targetFramework, 
            DependencyType type, 
            bool resolved)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrEmpty(targetFramework))
            {
                throw new ArgumentNullException(nameof(targetFramework));
            }

            Name = name;
            Version = version;
            TargetFramework = targetFramework;
            Resolved = resolved;
            Path = path;
            Type = type;

            _dependencies = new List<Dependency>();
        }

        /// <summary>
        /// TFM of the project to which this dependency belongs.
        /// </summary>
        public string TargetFramework { get; }

        /// <summary>
        /// Name of the dependency.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Full path to the dependency.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Version of the dependency.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Type of the dependency. <see cref="DependencyType"/>
        /// </summary>
        public DependencyType Type { get; }

        /// <summary>
        /// Specifies whether this dependency has been resolved.
        /// </summary>
        public bool Resolved { get; }

        /// <summary>
        /// Dependencies of the this dependency.
        /// </summary>
        public IEnumerable<Dependency> Dependencies => _dependencies;

        /// <summary>
        /// Adds a dependency to current dependencies.
        /// </summary>
        /// <param name="dependency"></param>
        public void AddDependency(Dependency dependency)
        {
            _dependencies.Add(dependency);
        }
    }
}
