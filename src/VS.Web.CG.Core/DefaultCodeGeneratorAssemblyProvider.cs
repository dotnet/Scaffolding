// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

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
                "Microsoft.VisualStudio.Web.CodeGeneration",
                "Microsoft.VisualStudio.Web.CodeGeneration.Design"
            };

        private readonly ICodeGenAssemblyLoadContext _assemblyLoadContext;
        private IProjectContext _projectContext;
         
        public DefaultCodeGeneratorAssemblyProvider(IProjectContext projectContext, ICodeGenAssemblyLoadContext loadContext)
        {
            if(loadContext == null)
            {
                throw new ArgumentNullException(nameof(loadContext));
            }
            if(projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }
            _projectContext = projectContext;
            _assemblyLoadContext = loadContext;

        }

        public IEnumerable<Assembly> CandidateAssemblies
        {
            get
            {

                var list = _codeGenerationFrameworkAssemblies
                    .SelectMany(_projectContext.GetReferencingPackages)
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