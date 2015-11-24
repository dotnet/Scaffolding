// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.CodeGeneration
{
    public class CodeGeneratorsLocator : ICodeGeneratorLocator
    {
        private readonly ICodeGeneratorAssemblyProvider _assemblyProvider;
        private readonly IServiceProvider _serviceProvider;

        public CodeGeneratorsLocator(
            IServiceProvider serviceProvider,
            ICodeGeneratorAssemblyProvider assemblyProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (assemblyProvider == null)
            {
                throw new ArgumentNullException(nameof(assemblyProvider));
            }

            _serviceProvider = serviceProvider;
            _assemblyProvider = assemblyProvider;
        }

        public CodeGeneratorDescriptor GetCodeGenerator(string codeGeneratorName)
        {
            if (codeGeneratorName == null)
            {
                throw new ArgumentNullException(nameof(codeGeneratorName));
            }

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

        private CodeGeneratorDescriptor DescriptorFromTypeInfo(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return new CodeGeneratorDescriptor(typeInfo, _serviceProvider);
        }

        private bool IsCodeGenerator(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

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