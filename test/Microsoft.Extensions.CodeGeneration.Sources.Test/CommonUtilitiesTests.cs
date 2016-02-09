// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Microsoft.Extensions.CodeGeneration.Sources.Test;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration
{
    public class CommonUtilitiesTests : TestBase
    {
        ICodeGenAssemblyLoadContext loadContext;

        public CommonUtilitiesTests() : base(@"..\TestApps\ModelTypesLocatorTestClassLibrary")
        {
            loadContext = new DefaultAssemblyLoadContext(
                             new Dictionary<AssemblyName, string>(),
                             new Dictionary<string, string>(),
                             new List<string>());
        }
        [Fact]
        public void CommonUtilities_TestGetAssemblyFromCompilation()
        {

            LibraryExporter exporter = new LibraryExporter(_projectContext);
            LibraryManager manager = new LibraryManager(_projectContext);
            IEnumerable<MetadataReference> references = exporter.GetAllExports().SelectMany(export => export.GetMetadataReferences());
            string code = @"using System;
                            namespace Sample { 
                                public class SampleClass 
                                {
                                } 
                            }";
            
            Compilation compilation = GetCompilation(code, "TestAssembly", references);
            CompilationResult result = CommonUtilities.GetAssemblyFromCompilation(loadContext, compilation);

            Assert.True(result.Success);
            Assert.True(result.Assembly.DefinedTypes.Where(_ => _.Name == "SampleClass").Any());
        }

        [Fact]
        public void CommonUtilities_TestGetAssemblyFromCompilation_Failure()
        {

            LibraryExporter exporter = new LibraryExporter(_projectContext);
            LibraryManager manager = new LibraryManager(_projectContext);

            string code = @"using System;
                            namespace Sample { 
                                public class SampleClass 
                                {
                                } 
                            }";

            Compilation compilation = GetCompilation(code, "TestAssembly", null);
            CompilationResult result = CommonUtilities.GetAssemblyFromCompilation(loadContext, compilation);

            Assert.False(result.Success);
            Assert.True(result.ErrorMessages.Any());
        }

        private Compilation GetCompilation(string content, string assemblyName, IEnumerable<MetadataReference> references, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        {
            return CSharpCompilation.Create(assemblyName,
                            new[] { CSharpSyntaxTree.ParseText(content) },
                            references,
                            new CSharpCompilationOptions(outputKind));
        }
    }
}
