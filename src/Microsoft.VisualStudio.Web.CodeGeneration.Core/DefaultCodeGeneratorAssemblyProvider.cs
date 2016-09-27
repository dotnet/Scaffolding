// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.MsBuild;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class DefaultCodeGeneratorAssemblyProvider : ICodeGeneratorAssemblyProvider
    {
        private static readonly HashSet<string> _codeGenerationFrameworkAssemblies =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Microsoft.VisualStudio.Web.CodeGeneration",
            };
        private static readonly HashSet<string> _exclusions =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Microsoft.VisualStudio.Web.CodeGeneration.Tools",
                "Microsoft.VisualStudio.Web.CodeGeneration"
            };

        private readonly ICodeGenAssemblyLoadContext _assemblyLoadContext;
        private ProjectDependencyProvider _projectDependencyProvider;
         
        public DefaultCodeGeneratorAssemblyProvider(ProjectDependencyProvider projectDependencyProvider, ICodeGenAssemblyLoadContext loadContext)
        {
            if(loadContext == null)
            {
                throw new ArgumentNullException(nameof(loadContext));
            }
            if(projectDependencyProvider == null)
            {
                throw new ArgumentNullException(nameof(projectDependencyProvider));
            }
            _projectDependencyProvider = projectDependencyProvider;
            _assemblyLoadContext = loadContext;

        }

        public IEnumerable<Assembly> CandidateAssemblies
        {
            get
            {

                var list = _codeGenerationFrameworkAssemblies
                    .SelectMany(_projectDependencyProvider.GetReferencingPackages)
                    .Distinct()
                    .Where(IsCandidateLibrary);
                return list.Select(lib => _assemblyLoadContext.LoadFromName(new AssemblyName(lib.Name)));
            }
        }

        private bool IsCandidateLibrary(DependencyDescription library)
        {
            return !_exclusions.Contains(library.Name);
        }
    }
}