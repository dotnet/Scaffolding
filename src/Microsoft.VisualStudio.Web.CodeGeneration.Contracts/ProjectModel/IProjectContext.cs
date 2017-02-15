// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel
{
    public interface IProjectContext
    {
        string ProjectName { get; }
        string Configuration { get; }
        string Platform { get; }
        string ProjectFullPath { get; }
        string RootNamespace { get; }
        bool IsClassLibrary { get; }
        string TargetFramework { get; }
        string Config { get; }
        string PackagesDirectory { get; }
        string TargetDirectory { get; }
        string AssemblyName { get; }
        string AssemblyFullPath { get; }
        string DepsFile { get; }
        string RuntimeConfig { get; }
        IEnumerable<string> CompilationItems { get; }
        IEnumerable<string> EmbededItems { get; }
        IEnumerable<DependencyDescription> PackageDependencies { get;}
        IEnumerable<ResolvedReference> CompilationAssemblies { get; }
        IEnumerable<string> ProjectReferences { get; }
        IEnumerable<ProjectReferenceInformation> ProjectReferenceInformation { get; }
    }
}