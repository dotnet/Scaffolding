// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration
{
    //Todo: Perhaps this should be internal, it's public right for being able to access
    //it in CodeGeneration project.
    public class CodeGeneratorFactoriesLocator
    {
        private static readonly HashSet<string> _codeGenerationFrameworkAssemblies =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Microsoft.Framework.CodeGeneration",
            };

        private ILibraryManager _libraryManager;
        private IServiceProvider _serviceProvider;
        private ITypeActivator _typeActivator;

        public CodeGeneratorFactoriesLocator(
            [NotNull]ITypeActivator typeActivator,
            [NotNull]IServiceProvider serviceProvider,
            [NotNull]ILibraryManager libraryManager)
        {
            _typeActivator = typeActivator;
            _serviceProvider = serviceProvider;
            _libraryManager = libraryManager;
        }

        //Perhaps this method could be optimized not to reflect all code generators
        //once we find a match?
        public CodeGeneratorFactory GetCodeGeneratorFactory([NotNull]string codeGeneratorName)
        {
            var candidates = CodeGeneratorFactories
                .Where(f => string.Equals(f.CodeGeneratorMetadata.Name, codeGeneratorName, StringComparison.OrdinalIgnoreCase));

            var count = candidates.Count();

            if (count == 0)
            {
                throw new Exception("No code generators found with the name " + codeGeneratorName);
            }

            if (count > 1)
            {
                throw new Exception("Multiple code generators found matching the name " + codeGeneratorName);
            }

            return candidates.First();
        }

        public IEnumerable<CodeGeneratorFactory> CodeGeneratorFactories
        {
            get
            {
                var factories = new List<CodeGeneratorFactory>();

                var libs = _codeGenerationFrameworkAssemblies
                    .SelectMany(_libraryManager.GetReferencingLibraries)
                    .Distinct()
                    .Where(IsCandidateLibrary);

                foreach (var lib in libs)
                {
                    var assembly = Assembly.Load(new AssemblyName(lib.Name));

                    if (assembly != null)
                    {
                        factories.AddRange(assembly
                            .DefinedTypes
                            .Where(IsCodeGeneratorFactory)
                            .Select(typeInfo => FactoryFromTypeInfo(typeInfo)));
                    }
                }

                return factories;
            }
        }

        private CodeGeneratorFactory FactoryFromTypeInfo([NotNull]TypeInfo typeInfo)
        {
            return (CodeGeneratorFactory)_typeActivator.CreateInstance(_serviceProvider, typeInfo.AsType());
        }

        private bool IsCodeGeneratorFactory([NotNull]TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass ||
                typeInfo.IsAbstract ||
                typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            if (typeInfo.Name.Equals("CodeGeneratorFactory", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return typeInfo.Name.EndsWith("CodeGeneratorFactory", StringComparison.OrdinalIgnoreCase) ||
                typeof(CodeGeneratorFactory).GetTypeInfo().IsAssignableFrom(typeInfo);
        }

        private bool IsCandidateLibrary(ILibraryInformation library)
        {
            return !_codeGenerationFrameworkAssemblies.Contains(library.Name);
        }
    }
}