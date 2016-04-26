// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.CodeGeneration.DotNet;
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
            var templateContent = @"Hello @Model.Name";
            var model = new SimpleModel() { Name = "World" };
            var compilationService = GetCompilationService();
            var templatingService = new RazorTemplating(compilationService);

            //Act
            var result = await templatingService.RunTemplateAsync(templateContent, model);

            //Assert
            Assert.Null(result.ProcessingException);
            Assert.Equal("Hello World", result.GeneratedText);
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
            Console.WriteLine("Processing exception: " + result.ProcessingException.Message);
            Assert.Equal("Template Processing Failed:(1,7): error CS0103: The name 'Invalid' does not exist in the current context",
                result.ProcessingException.Message);
        }

        private ICompilationService GetCompilationService()
        {
            ProjectContext context = CreateProjectContext(null);
            var applicationInfo = new ApplicationInfo("", context.ProjectDirectory);
            ICodeGenAssemblyLoadContext loader = new DefaultAssemblyLoadContext();
            IApplicationInfo _applicationInfo;

#if RELEASE 
            _applicationInfo = new ApplicationInfo("ModelTypesLocatorTestClassLibrary", Directory.GetCurrentDirectory(), "Release");
#else
            _applicationInfo = new ApplicationInfo("ModelTypesLocatorTestClassLibrary", Directory.GetCurrentDirectory(), "Debug");
#endif

            ILibraryExporter libExporter = new LibraryExporter(context, _applicationInfo);

            return new RoslynCompilationService(applicationInfo, loader, libExporter);
        }

        private static ProjectContext CreateProjectContext(string projectPath)
        {
#if NET451
            projectPath = projectPath ?? Path.Combine("..", "..", "..", "..");
            var framework = NuGet.Frameworks.FrameworkConstants.CommonFrameworks.Net451.GetShortFolderName();
#else
            projectPath = projectPath ?? Directory.GetCurrentDirectory();
            var framework = NuGet.Frameworks.FrameworkConstants.CommonFrameworks.NetCoreApp10.GetShortFolderName();
#endif
            if (!projectPath.EndsWith(Microsoft.DotNet.ProjectModel.Project.FileName))
            {
                projectPath = Path.Combine(projectPath, Microsoft.DotNet.ProjectModel.Project.FileName);
            }

            if (!File.Exists(projectPath))
            {
                throw new InvalidOperationException($"{projectPath} does not exist.");
            }
            return ProjectContext.CreateContextForEachFramework(projectPath).FirstOrDefault(c => c.TargetFramework.GetShortFolderName() == framework);
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