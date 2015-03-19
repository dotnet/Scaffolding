// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Framework.CodeGeneration.Templating.Compilation
{
    public class RoslynCompilationService : ICompilationService
    {
        public CompilationResult Compile(string content, List<MetadataReference> references)
        {
            var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(content) };

            var assemblyName = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(assemblyName,
                        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                        syntaxTrees: syntaxTrees,
                        references: references);

            var result = CommonUtilities.GetAssemblyFromCompilation(compilation);
            if (result.Success)
            {
                var type = result.Assembly.GetExportedTypes()
                                   .First();

                return CompilationResult.Successful(string.Empty, type);
            }
            else
            {
                return CompilationResult.Failed(content, result.ErrorMessages);
            }
        }
    }
}
