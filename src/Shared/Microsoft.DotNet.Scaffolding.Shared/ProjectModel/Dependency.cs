// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.DotNet.Scaffolding.Shared.ProjectModel
{
    /// <summary>
    /// Represents a dependency.
    /// </summary>
    public class Dependency
    {
        /// <summary/>
        /// <param name="name">Name of the dependency.</param>
        /// <param name="version">Version of the dependency.</param>
        public Dependency(string name, string version)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Version = version;
        }

        /// <summary>
        /// Name of the dependency.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Version of the dependency.
        /// </summary>
        public string Version { get; }
    }
}
