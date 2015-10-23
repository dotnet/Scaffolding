// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Dnx.Compilation;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.CodeGeneration.Templating.Compilation;
using Moq;
using Xunit;

namespace Microsoft.Extensions.CodeGeneration.Templating.Test
{
    //This is more of an integration test.
    public class RazorTemplatingTests
    {
        [Fact]
        public async void RunTemplateAsync_Generates_Text_For_Template_With_A_Model()
        {
            //Arrange
            var templateContent = "Hello @Model.Name";
            var model = new SimpleModel() { Name = "World" };
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
            var result = await templatingService.RunTemplateAsync(templateContent, templateModel: "DoesNotMatter");

            //Assert
            Assert.Equal("", result.GeneratedText);
            Assert.NotNull(result.ProcessingException);
            Assert.Equal("Template Processing Failed:(1,7): error CS0103: The name 'Invalid' does not exist in the current context",
                result.ProcessingException.Message);
        }

        private ICompilationService GetCompilationService()
        {
            var appEnvironment = PlatformServices.Default.Application;
            var loaderAccessor = PlatformServices.Default.AssemblyLoadContextAccessor;
            var libExporter = CompilationServices.Default.LibraryExporter;

            var emptyLibExport = new LibraryExport(metadataReferences: null);
            var mockLibExporter = new Mock<ILibraryExporter>();

            var input = "Microsoft.Extensions.CodeGeneration.Templating.Test";
            mockLibExporter
                .Setup(lm => lm.GetAllExports(input))
                .Returns(libExporter.GetAllExports(input));

            return new RoslynCompilationService(appEnvironment, loaderAccessor, mockLibExporter.Object);
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