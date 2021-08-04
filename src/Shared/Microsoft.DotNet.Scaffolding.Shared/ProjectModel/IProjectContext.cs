// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Scaffolding.Shared.ProjectModel
{
    /// <summary>
    /// Represents the project information on which scaffolding is being done.
    /// </summary>
    public interface IProjectContext
    {
        /// <summary>
        /// Name of the project.
        /// </summary>
        string ProjectName { get; }

        /// <summary>
        /// Project Configuration.
        /// </summary>
        string Configuration { get; }

        /// <summary>
        /// Platform targeted by the project.
        /// </summary>
        string Platform { get; }

        /// <summary>
        /// Full path to the csproj file of the project.
        /// </summary>
        string ProjectFullPath { get; }

        /// <summary>
        /// Default namespace for the project.
        /// </summary>
        string RootNamespace { get; }

        /// <summary>
        /// Specifies whether the output of the project is a
        /// class library.
        /// </summary>
        bool IsClassLibrary { get; }

        /// <summary>
        /// TargetFramework for the project.
        /// If the project has multiple frameworks, all of the
        /// information in the ProjectContext is specific to this
        /// TargetFramework.
        /// </summary>
        string TargetFramework { get; }

        /// <summary>
        /// TargetFrameworkMoniker for the project.
        /// If the project has multiple frameworks, all of the
        /// information in the ProjectContext is specific to this
        /// TargetFramework.
        /// </summary>
        string TargetFrameworkMoniker { get; }

        /// <summary>
        /// Full path to config file for the assembly.
        /// Usually <see cref="AssemblyFullPath"/> + ".config"
        /// </summary>
        string Config { get; }

        /// <summary>
        /// NuGet package root for the project.
        /// </summary>
        string PackagesDirectory { get; }

        /// <summary>
        /// Full path of the Output directory.
        /// </summary>
        string TargetDirectory { get; }

        /// <summary>
        /// File name of the project output.
        /// </summary>
        string AssemblyName { get; }

        /// <summary>
        /// The full path of the project output.
        /// </summary>
        string AssemblyFullPath { get; }
        
        /// <summary>
        /// Full path to deps.json file of the built project.
        /// </summary>
        string DepsFile { get; }

        /// <summary>
        /// Full path to runtimeconfig.json file for the project.
        /// </summary>
        string RuntimeConfig { get; }

        /// <summary>
        /// Items included for compilation in the project.
        /// &lt;Compile Include="" /&gt;
        /// </summary>
        IEnumerable<string> CompilationItems { get; }

        /// <summary>
        /// Items inlcuded as embedded resources.
        /// &lt;EmbeddedResource Include="" /&gt;
        /// </summary>
        IEnumerable<string> EmbededItems { get; }

        /// <summary>
        /// NuGet dependencies of the project.
        /// </summary>
        IEnumerable<DependencyDescription> PackageDependencies { get;}

        /// <summary>
        /// Assemblies required for compilation of the project.
        /// </summary>
        IEnumerable<ResolvedReference> CompilationAssemblies { get; }

        /// <summary>
        /// Paths to project references (direct and indirect) of the project.
        /// </summary>
        IEnumerable<string> ProjectReferences { get; }

        /// <summary>
        /// Collection of information regarding the project references.
        /// </summary>
        IEnumerable<ProjectReferenceInformation> ProjectReferenceInformation { get; }

        /// <summary>
        /// .cs file in obj folder generated at compile time with all default namespace imports in .NET 6+.
        /// </summary>
        string GeneratedImplicitNamespaceImportFile { get; }
    }
}
