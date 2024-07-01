// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.Test.Sources;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Blazor;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class BlazorWebCRUDGeneratorTests
    {
        private SourceText ProgramCs = SourceText.From(MsBuildProjectStrings.ProgramCsBlazor);
        private SourceText CarCs = SourceText.From(MsBuildProjectStrings.CarTxt);
        private IModelTypesLocator modelTypesLocator;
        private AdhocWorkspace workspace;
        private IFileSystem fileSystem;

        [Fact]
        public void ValidateAndGetOutputPathTests()
        {
            Mock<IApplicationInfo> _mockApp = new Mock<IApplicationInfo>();
            bool isWindows = OperatingSystem.IsWindows();
            var applicationBasePath = "AppPath";
            _mockApp.Setup(app => app.ApplicationBasePath)
                .Returns(applicationBasePath);
            var blazorGenerator = CreateBlazorWebCRUDGenerator();

            var relativeFolderPath = isWindows ? @"SpecialPages\BlazorComponents" : @"SpecialPages/BlazorComponents";
            if (isWindows)
            {
                Assert.Equal(@"AppPath\Components\Pages\testModelPages\Create.razor", blazorGenerator.ValidateAndGetOutputPath("testModel", "Create"));
                Assert.Equal(@"AppPath\SpecialPages\BlazorComponents\testModelPages\Create.razor", blazorGenerator.ValidateAndGetOutputPath("testModel", "Create", relativeFolderPath));
            }
            else
            {
                Assert.Equal(@"AppPath/Components/Pages/testModelPages/Create.razor", blazorGenerator.ValidateAndGetOutputPath("testModel", "Create"));
                Assert.Equal(@"AppPath/SpecialPages/BlazorComponents/testModelPages/Create.razor", blazorGenerator.ValidateAndGetOutputPath("testModel", "Create", relativeFolderPath));
            }
        }

        private BlazorWebCRUDGenerator CreateBlazorWebCRUDGenerator()
        {
            Mock<IServiceProvider> serviceProvider = new Mock<IServiceProvider>();
            Mock<IApplicationInfo> appInfo = new Mock<IApplicationInfo>();
            Mock<ILogger> logger = new Mock<ILogger>();
            fileSystem = new MockFileSystem();
            IFilesLocator filesLocator = new FilesLocator(fileSystem);
            Mock<IProjectContext> projectContext = new Mock<IProjectContext>();
            Mock<IEntityFrameworkService> entityFrameworkService = new Mock<IEntityFrameworkService>();
            Mock<ICodeGeneratorActionsService> codeGeneratorActionsService = new Mock<ICodeGeneratorActionsService>();
            workspace = new AdhocWorkspace();

            var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(),
                VersionStamp.Default,
                "TestAssembly",
                "TestAssembly",
                LanguageNames.CSharp);

            var project = workspace.AddProject(projectInfo);
            appInfo.Setup(app => app.ApplicationBasePath)
                .Returns(@"AppPath");

            projectContext.Setup(ctx => ctx.AssemblyName)
                .Returns("TestAssembly");

            var id = DocumentId.CreateNewId(project.Id);
            var id2 = DocumentId.CreateNewId(project.Id);
            var id3 = DocumentId.CreateNewId(project.Id);
            var programCsLoader = TextLoader.From(TextAndVersion.Create(ProgramCs, VersionStamp.Create())); ;
            var carCsLoader = TextLoader.From(TextAndVersion.Create(CarCs, VersionStamp.Create())); ;
            workspace.AddDocument(DocumentInfo.Create(id2, "Program.cs", loader: programCsLoader, filePath: "Program.cs"));
            workspace.AddDocument(DocumentInfo.Create(id3, "Car.cs", loader: carCsLoader, filePath: "Car.cs"));
            fileSystem.WriteAllText("Program.cs", MsBuildProjectStrings.ProgramCsBlazor);
            modelTypesLocator = new ModelTypesLocator(workspace);
            return new BlazorWebCRUDGenerator(
                appInfo.Object,
                serviceProvider.Object,
                modelTypesLocator,
                logger.Object,
                fileSystem,
                filesLocator,
                projectContext.Object,
                entityFrameworkService.Object,
                workspace);
        }
    }
}
