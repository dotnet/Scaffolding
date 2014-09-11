// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.Framework.CodeGeneration
{
    public class CodeGeneratorsLocator : ICodeGeneratorLocator
    {
        private readonly ICodeGeneratorAssemblyProvider _assemblyProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITypeActivator _typeActivator;

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
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture,
                        "No code generators found with the name '{0}'",
                        codeGeneratorName));
            }

            if (count > 1)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture,
                    "Multiple code generators found matching the name '{0}'",
                    codeGeneratorName));
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