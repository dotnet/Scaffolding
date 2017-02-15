// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel
{
    public class DependencyDescription
    {
        private readonly List<Dependency> _dependencies;

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

        public string TargetFramework { get; }
        public string Name { get; }
        public string Path { get; }
        public string Version { get; }
        public DependencyType Type { get; }
        public bool Resolved { get; }
        public IEnumerable<Dependency> Dependencies => _dependencies;

        public void AddDependency(Dependency dependency)
        {
            _dependencies.Add(dependency);
        }
    }
}
