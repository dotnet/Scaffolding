// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;

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

        private readonly ILibraryManager _libraryManager;
        private readonly ICodeGenAssemblyLoadContext _assemblyLoadContext;
        private readonly ILibraryExporter _libraryExporter;

        public DefaultCodeGeneratorAssemblyProvider(ILibraryManager libraryManager, ICodeGenAssemblyLoadContext loadContext, ILibraryExporter libraryExporter)
        {
            if (libraryManager == null)
            {
                throw new ArgumentNullException(nameof(libraryManager));
            }
            if(loadContext == null)
            {
                throw new ArgumentNullException(nameof(loadContext));
            }
            if(libraryExporter == null)
            {
                throw new ArgumentNullException(nameof(libraryExporter));
            }
            _libraryManager = libraryManager;
            _assemblyLoadContext = loadContext;
            _libraryExporter = libraryExporter;

        }

        public IEnumerable<Assembly> CandidateAssemblies
        {
            get
            {

                var list = _codeGenerationFrameworkAssemblies
                    .SelectMany(_libraryManager.GetReferencingLibraries)
                    .Distinct()
                    .Where(IsCandidateLibrary);
                return list.Select(lib => _assemblyLoadContext.LoadFromName(new AssemblyName(lib.Identity.Name)));
            }
        }

        private bool IsCandidateLibrary(LibraryDescription library)
        {
            return !_exclusions.Contains(library.Identity.Name);
        }
    }
}