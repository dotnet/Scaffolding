// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Microsoft.Extensions.CodeGeneration.Sources.Test;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration
{
    public class CommonUtilitiesTests : TestBase
    {
#if NET451
        static string testAppPath = Path.Combine("..", "..", "..", "..", "..", "TestApps", "ModelTypesLocatorTestClassLibrary");
#else
        static string testAppPath = Path.Combine("..", "TestApps", "ModelTypesLocatorTestClassLibrary");
#endif
        ICodeGenAssemblyLoadContext loadContext;

        public CommonUtilitiesTests()
            : base(Path.Combine(testAppPath))
        {
            loadContext = new DefaultAssemblyLoadContext();
        }
        
        [Fact]
        public void CommonUtilities_TestGetAssemblyFromCompilation()
        {
            LibraryExporter exporter = new LibraryExporter(_projectContext, _applicationInfo);

            LibraryManager manager = new LibraryManager(_projectContext);
            IEnumerable<MetadataReference> references = exporter.GetAllExports().SelectMany(export => export.GetMetadataReferences());
            string code = @"using System;
                            namespace Sample { 
                                public class SampleClass 
                                {
                                } 
                            }";
            
            Compilation compilation = GetCompilation(code, Path.GetRandomFileName(), references);
            CompilationResult result = CommonUtilities.GetAssemblyFromCompilation(loadContext, compilation);

            Assert.True(result.Success);
            Assert.True(result.Assembly.DefinedTypes.Where(_ => _.Name == "SampleClass").Any());
        }

        [Fact]
        public void CommonUtilities_TestGetAssemblyFromCompilation_Failure()
        {

            string code = @"using System;
                            namespace Sample { 
                                public class SampleClass 
                                {
                                } 
                            }";

            Compilation compilation = GetCompilation(code, Path.GetRandomFileName(), null);
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
