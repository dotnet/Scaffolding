// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Framework.CodeGeneration
{
    //Todo: Perhaps this should be internal, it's public right now for being able to access
    //it in CodeGeneration project.
    public class CodeGeneratorsLocator : ICodeGeneratorLocator
    {
        private ICodeGeneratorAssemblyProvider _assemblyProvider;
        private IServiceProvider _serviceProvider;
        private ITypeActivator _typeActivator;

        public CodeGeneratorsLocator(
            [NotNull]ITypeActivator typeActivator,
            [NotNull]IServiceProvider serviceProvider,
            [NotNull]ICodeGeneratorAssemblyProvider assemblyProvider)
        {
            _typeActivator = typeActivator;
            _serviceProvider = serviceProvider;
            _assemblyProvider = assemblyProvider;
        }

        public CodeGeneratorDescriptor GetCodeGenerator([NotNull]string codeGeneratorName)
        {
            var candidates = CodeGenerators
                .Where(gen => string.Equals(gen.Name, codeGeneratorName, StringComparison.OrdinalIgnoreCase));

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

        public IEnumerable<CodeGeneratorDescriptor> CodeGenerators
        {
            get
            {
                var descriptors = new List<CodeGeneratorDescriptor>();

                foreach (var assembly in _assemblyProvider.CandidateAssemblies)
                {
                    descriptors.AddRange(assembly
                        .DefinedTypes
                        .Where(IsCodeGenerator)
                        .Select(typeInfo => DescriptorFromTypeInfo(typeInfo)));
                }

                return descriptors;
            }
        }

        private CodeGeneratorDescriptor DescriptorFromTypeInfo([NotNull]TypeInfo typeInfo)
        {
            return new CodeGeneratorDescriptor(typeInfo, _typeActivator, _serviceProvider);
        }

        private bool IsCodeGenerator([NotNull]TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass ||
                typeInfo.IsAbstract ||
                typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            return typeInfo.Name.EndsWith("CodeGenerator", StringComparison.OrdinalIgnoreCase) ||
                typeof(ICodeGenerator).GetTypeInfo().IsAssignableFrom(typeInfo);
        }
    }
}