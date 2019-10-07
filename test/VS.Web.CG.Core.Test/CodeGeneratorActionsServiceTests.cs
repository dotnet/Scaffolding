// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;
using Microsoft.VisualStudio.Web.CodeGeneration.Test.Sources;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.Test
{
    public class CodeGeneratorActionsServiceTests
    {
        [Fact]
        public async Task AddFileFromTemplateAsync_Throws_If_Template_Is_Not_Found()
        {
            var mockFilesLocator = new Mock<IFilesLocator>();
            var mockTemplating = new Mock<ITemplating>();

            var codeGeneratorActionService = new CodeGeneratorActionsService(
                mockTemplating.Object, mockFilesLocator.Object, DefaultFileSystem.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await codeGeneratorActionService.AddFileFromTemplateAsync("Dummy", 
                    "Template",
                    new[] { "TemplateFolder1", "TemplateFolder2" },
                    null));
            Assert.Equal("Template file Template not found within search paths TemplateFolder1;TemplateFolder2", ex.Message);
        }

        [Fact]
        public async Task AddFileFromTemplateAsync_Throws_If_Template_Processing_Has_Exceptions()
        {
            var mockFilesLocator = new Mock<IFilesLocator>();
            var mockTemplating = new Mock<ITemplating>();
            var mockFileSystem = new MockFileSystem();

            var templateName = "TemplateName";
            var templatePath = "C:\template.cshtml";
            var templateContent = "TemplateContent";
            var processingException = new TemplateProcessingException(new[] { "Error1" }, string.Empty);

            mockFilesLocator.Setup(fl => fl.GetFilePath(templateName, It.IsAny<IEnumerable<string>>()))
                .Returns(templatePath);
            mockFileSystem.WriteAllText(templatePath, templateContent);
            mockTemplating.Setup(templating => templating.RunTemplateAsync(templateContent, null))
                .Returns(Task.FromResult(new TemplateResult()
                {
                    ProcessingException = processingException
                }));

            var codeGeneratorActionService = new CodeGeneratorActionsService(
                mockTemplating.Object, mockFilesLocator.Object, mockFileSystem);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await codeGeneratorActionService.AddFileFromTemplateAsync("Dummy",
                    templateName,
                    new[] { "TemplateFolder1", "TemplateFolder2" },
                    null));
            Assert.Equal("There was an error running the template " + templatePath + ": Template Processing Failed:Error1", ex.Message);
        }

        [Fact]
        public async Task AddFileFromTemplateAsync_Writes_If_Template_Processing_Is_Successful()
        {
            var mockFilesLocator = new Mock<IFilesLocator>();
            var mockTemplating = new Mock<ITemplating>();
            var mockFileSystem = new MockFileSystem();

            var templateName = "TemplateName";
            var templatePath = "C:\template.cshtml";
            var templateContent = "TemplateContent";
            var outputPath = @"C:\Output.txt";
            var generatedText = "GeneratedText";

            mockFilesLocator.Setup(fl => fl.GetFilePath(templateName, It.IsAny<IEnumerable<string>>()))
                .Returns(templatePath);
            mockFileSystem.WriteAllText(templatePath, templateContent);
            mockTemplating.Setup(templating => templating.RunTemplateAsync(templateContent, null))
                .Returns(Task.FromResult(new TemplateResult()
                {
                    ProcessingException = null,
                    GeneratedText = generatedText
                }));

            var codeGeneratorActionService = new CodeGeneratorActionsService(
                mockTemplating.Object, mockFilesLocator.Object, mockFileSystem);

            await codeGeneratorActionService.AddFileFromTemplateAsync(outputPath,
                    templateName,
                    new[] { "TemplateFolder1", "TemplateFolder2" },
                    null);

            Assert.True(mockFileSystem.FileExists(outputPath));
            Assert.Equal(generatedText, mockFileSystem.ReadAllText(outputPath));
        }
    }
}