// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.ProjectModel.Resolution;
using NuGet.Frameworks;
using Newtonsoft.Json;

namespace Microsoft.Extensions.ProjectModel
{
    public class CommonProjectContext : IProjectContext
    {
        public string AssemblyFullPath { get; set; }

        public string AssemblyName { get; set; }

        public IEnumerable<ResolvedReference> CompilationAssemblies { get; set; }

        public IEnumerable<string> CompilationItems { get; set; }

        public string Config { get; set; }

        public string Configuration { get; set; }

        public IEnumerable<string> EmbededItems { get; set; }

        public bool IsClassLibrary { get; set; }

        public IEnumerable<DependencyDescription> PackageDependencies { get; set; }

        public string PackagesDirectory { get; set; }

        public string Platform { get; set; }

        public string ProjectFullPath { get; set; }

        public string ProjectName { get; set; }

        public IEnumerable<ProjectReferenceInformation> ProjectReferenceInformation { get; set; }

        public IEnumerable<string> ProjectReferences { get; set; }

        public string RootNamespace { get; set; }

        public string TargetDirectory { get; set; }

        public string TargetFramework { get; set; }
    }
}
