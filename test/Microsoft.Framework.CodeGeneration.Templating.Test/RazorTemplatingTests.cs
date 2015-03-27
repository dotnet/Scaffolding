// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.CodeGeneration.Templating.Compilation;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Compilation;
using Microsoft.Framework.Runtime.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.Framework.CodeGeneration.Templating.Test
{
    //This is more of an integration test.
    public class RazorTemplatingTests
    {
        [Fact]
        public async void RunTemplateAsync_Generates_Text_For_Template_With_A_Model()
        {
            //Arrange
            var templateContent = "Hello @Model.Name";
            var model = new SimpleModel () { Name = "World" };
            var compilationService = GetCompilationService();
            var templatingService = new RazorTemplating(compilationService);

            //Act
            var result = await templatingService.RunTemplateAsync(templateContent, model);

            //Assert
            Assert.Equal("Hello World", result.GeneratedText);
            Assert.Null(result.ProcessingException);
        }

        [Fact]
        public async void RunTemplateAsync_Returns_Error_For_Invalid_Template()
        {
            //Arrange
            var templateContent = "@Invalid";
            var compilationService = GetCompilationService();
            var templatingService = new RazorTemplating(compilationService);

            //Act
            var result = await templatingService.RunTemplateAsync(templateContent, templateModel:"DoesNotMatter");

            //Assert
            Assert.Equal("", result.GeneratedText);
            Assert.NotNull(result.ProcessingException);
            Assert.Equal("Template Processing Failed:(1,7): error CS0103: The name 'Invalid' does not exist in the current context",
                result.ProcessingException.Message);
        }

        private ICompilationService GetCompilationService()
        {
            var originalProvider = CallContextServiceLocator.Locator.ServiceProvider;
            var appEnvironment = (IApplicationEnvironment)originalProvider.GetService(typeof(IApplicationEnvironment));
            var loaderAccessor = (IAssemblyLoadContextAccessor)originalProvider.GetService(typeof(IAssemblyLoadContextAccessor));
            var libManager = (ILibraryManager)originalProvider.GetService(typeof(ILibraryManager));

            var emptyLibExport = new LibraryExport();
            var mockLibManager = new Mock<ILibraryManager>();
            mockLibManager
                .Setup(lm => lm.GetAllExports("Microsoft.Framework.CodeGeneration"))
                .Returns(emptyLibExport);

            var input = "Microsoft.Framework.CodeGeneration.Templating.Test";
            mockLibManager
                .Setup(lm => lm.GetAllExports(input))
                .Returns(libManager.GetAllExports(input));

            return new RoslynCompilationService(appEnvironment, loaderAccessor, mockLibManager.Object);
        }

        private class LibraryExport : ILibraryExport
        {
            public IList<IMetadataReference> MetadataReferences { get; }
            public IList<ISourceReference> SourceReferences { get; }

            public LibraryExport()
            {
                MetadataReferences = new List<IMetadataReference>();
                SourceReferences = new List<ISourceReference>();
            }
        }
    }

    //If i make this a private class inside the above class,
    //dynamic runtime is failing to bind the Name property at runtime.
    //A bit strange.
    public class SimpleModel
    {
        public string Name { get; set; }
    }
}