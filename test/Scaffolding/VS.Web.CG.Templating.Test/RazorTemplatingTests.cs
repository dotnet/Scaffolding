// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating.Test
{
    //This is more of an integration test.
    public class RazorTemplatingTests
    {
        [Fact(Skip = "Disabling test on CI")]
        public async void RunTemplateAsync_Generates_Text_For_Template_With_A_Model()
        {
            //Arrange
            var templateContent = @"Hello @Model.Name";
            var model = new SimpleModel() { Name = "World" };
            var compilationService = new TestCompilationService();
            var templatingService = new RazorTemplating(compilationService);

            //Act
            var result = await templatingService.RunTemplateAsync(templateContent, model);

            //Assert
            Assert.Null(result.ProcessingException);
            Assert.Equal("Hello World", result.GeneratedText);
        }

        [Fact(Skip = "Disabling test on CI")]
        public async void RunTemplateAsync_Returns_Error_For_Invalid_Template()
        {
            //Arrange
            var templateContent = "@Invalid";
            var compilationService = new TestCompilationService();
            var templatingService = new RazorTemplating(compilationService);

            //Act
            var result = await templatingService.RunTemplateAsync(templateContent, templateModel: "DoesNotMatter");

            //Assert
            Assert.Equal("", result.GeneratedText);
            Assert.NotNull(result.ProcessingException);
            Console.WriteLine("Processing exception: " + result.ProcessingException.Message);
            Assert.Equal("Template Processing Failed:(1,7): error CS0103: The name 'Invalid' does not exist in the current context",
                result.ProcessingException.Message);
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