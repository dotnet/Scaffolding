// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo
{
    public interface IMsBuildProjectContext
    {
        string AssemblyFullPath { get; }
        string Config { get; }
        string Configuration { get; }
        IEnumerable<MsBuildProjectFile> DependencyProjectFiles { get; }
        string DepsJson { get; }
        bool IsClassLibrary { get; }
        string PackagesDirectory { get; }
        string Platform { get; }
        MsBuildProjectFile ProjectFile { get; }
        string ProjectFullPath { get; }
        string ProjectName { get; }
        string RootNamespace { get; }
        string RuntimeConfigJson { get; }
        string TargetDirectory { get; }
        NuGetFramework TargetFramework { get; }
    }
}