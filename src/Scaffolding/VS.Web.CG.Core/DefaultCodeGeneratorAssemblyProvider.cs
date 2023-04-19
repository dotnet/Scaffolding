// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
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
            if (loadContext == null)
            {
                throw new ArgumentNullException(nameof(loadContext));
            }
            if (projectContext == null)
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