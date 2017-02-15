// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class CommonUtilitiesTests : TestBase
    {
#if NET451
        static string testAppPath = Path.Combine("..", "..", "..", "..", "..", "TestApps", "ModelTypesLocatorTestClassLibrary");
#else
        static string testAppPath = Path.Combine("..", "TestApps", "ModelTypesLocatorTestClassLibrary");
#endif
        ICodeGenAssemblyLoadContext loadContext;
        private ITestOutputHelper _outputHelper;

        public CommonUtilitiesTests(ITestOutputHelper outputHelper)
        {
            loadContext = new DefaultAssemblyLoadContext();
            _outputHelper = outputHelper;
        }
        
        [Fact (Skip="Rewrite this for msbuild")]
        public void CommonUtilities_TestGetAssemblyFromCompilation()
        {
            // TODO Use Msbuild Project here.
            var projectContext = GetProjectContext(testAppPath, false);
            IEnumerable<MetadataReference> references = projectContext.CompilationAssemblies.SelectMany(c => c.GetMetadataReference());
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

        [Fact(Skip=MsBuildProjectStrings.SkipReason)]
        public void CommonUtilities_TestGetAssemblyFromCompilation_MsBuild()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {

                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, _outputHelper);
                var path = Path.Combine(fileProvider.Root, "Root", MsBuildProjectStrings.RootProjectName);

                var projectContext = GetProjectContext(path, true);
                
                var references = projectContext.CompilationAssemblies.SelectMany(c => c.GetMetadataReference());
                var code = @"using System;
                            namespace Sample { 
                                public class SampleClass 
                                {
                                } 
                            }";

                var compilation = GetCompilation(code, Path.GetRandomFileName(), references);
                var result = CommonUtilities.GetAssemblyFromCompilation(loadContext, compilation);

                Assert.True(result.Success);
                Assert.True(result.Assembly.DefinedTypes.Where(_ => _.Name == "SampleClass").Any());
            }
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
