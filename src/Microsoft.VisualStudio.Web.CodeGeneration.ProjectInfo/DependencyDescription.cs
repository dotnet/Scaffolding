// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public class DependencyDescription
    {
        private HashSet<Dependency> _dependencies;
        public DependencyDescription(string name, string path, string itemSpec, string version, string type, string resolved)
        {
            Requires.NotNullOrEmpty(name, nameof(name));
            Requires.NotNullOrEmpty(itemSpec, nameof(itemSpec));

            Name = name;
            Path = path;
            ItemSpec = itemSpec;
            Version = version;

            bool res = false;
            Resolved = bool.TryParse(resolved, out res) ? res : false;

            DependencyType d;
            Type = Enum.TryParse(type, out d) ? d :DependencyType.Unknown;

            _dependencies = new HashSet<Dependency>();
        }

        public string TargetFramework
        {
            get
            {
                return ItemSpec.Split('/').FirstOrDefault();
            }
        }
        public string Name { get; }
        public string Path { get; }
        public string ItemSpec { get; }
        public string Version { get; }
        public DependencyType Type { get; }
        public bool Resolved { get; }


        public IEnumerable<Dependency> Dependencies
        {
            get
            {
                return _dependencies;
            }
        }

        public void AddDependency(Dependency dependency)
        {
            Requires.NotNull(dependency, nameof(dependency));
            if (!_dependencies.Contains(dependency))
            {
                _dependencies.Add(dependency);
            }
        }
    }
}
