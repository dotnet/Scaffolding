// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration
{
    public class DefaultCodeGeneratorAssemblyProvider : ICodeGeneratorAssemblyProvider
    {
        private static readonly HashSet<string> _codeGenerationFrameworkAssemblies =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Microsoft.Framework.CodeGeneration",
            };

        private ILibraryManager _libraryManager;

        public DefaultCodeGeneratorAssemblyProvider([NotNull]ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public IEnumerable<Assembly> CandidateAssemblies
        {
            get
            {
                return _codeGenerationFrameworkAssemblies
                    .SelectMany(_libraryManager.GetReferencingLibraries)
                    .Distinct()
                    .Where(IsCandidateLibrary)
                    .Select(lib => Assembly.Load(new AssemblyName(lib.Name)));
            }
        }

        private bool IsCandidateLibrary(ILibraryInformation library)
        {
            return !_codeGenerationFrameworkAssemblies.Contains(library.Name);
        }
    }
}