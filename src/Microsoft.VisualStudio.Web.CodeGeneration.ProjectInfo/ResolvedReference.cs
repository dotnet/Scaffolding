// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public class ResolvedReference
    {
        public ResolvedReference(string name, string itemSpec, string nugetPackageSource, string resolvedPath, string version)
        {
            ItemSpec = itemSpec;
            Name = name;
            ResolvedPath = resolvedPath;
            Version = version;

            IsNugetPackage = !string.IsNullOrEmpty(nugetPackageSource) && "Package".Equals(nugetPackageSource, StringComparison.OrdinalIgnoreCase);
        }

        public string ItemSpec { get; }
        public bool IsNugetPackage { get; }
        public string ResolvedPath { get; }
        public string Name { get; }
        public string Version { get; }
        public bool IsResolved
        {
            get
            {
                return !string.IsNullOrEmpty(ResolvedPath) && File.Exists(ResolvedPath);
            }
        }
    }
}
