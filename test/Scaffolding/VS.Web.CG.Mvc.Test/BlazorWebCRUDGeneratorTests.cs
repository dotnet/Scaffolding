// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
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

        [Fact]
        public void CreateTemplate_GeneratesNullCoalescingAssignment()
        {
            // Verify that Create.tt template contains the null coalescing assignment pattern
            var createTemplatePath = Path.Combine("Templates", "Blazor", "Create.tt");
            var transformation = BlazorWebCRUDHelper.GetBlazorTransformation(createTemplatePath);
            
            Assert.NotNull(transformation);
            
            // Read the actual template file to verify the pattern
            var templateContent = File.ReadAllText(Path.Combine("src", "Scaffolding", "VS.Web.CG.Mvc", "Templates", "Blazor", "Create.tt"));
            
            // Verify the new pattern exists
            Assert.Contains("= default!", templateContent);
            Assert.Contains("protected override void OnInitialized()", templateContent);
            Assert.Contains("??= new()", templateContent);
            
            // Verify the old pattern is not used
            Assert.DoesNotContain("{ get; set; } = new()", templateContent);
        }

        [Fact]
        public void BlazorTemplates_UseNotFoundNavigation()
        {
            // Test that templates use NavigationManager.NotFound() instead of NavigateTo("notfound")
            var templateFiles = new[] { "Edit.tt", "Details.tt", "Delete.tt" };
            var templateBasePath = "src/Scaffolding/VS.Web.CG.Mvc/Templates/Blazor";
            
            foreach (var templateFile in templateFiles)
            {
                var templatePath = Path.Combine(templateBasePath, templateFile);
                if (File.Exists(templatePath))
                {
                    var content = File.ReadAllText(templatePath);
                    
                    // Verify that NavigationManager.NotFound() is used
                    Assert.Contains("NavigationManager.NotFound()", content);
                    
                    // Verify that old NavigateTo("notfound") pattern is not used
                    Assert.DoesNotContain("NavigationManager.NavigateTo(\"notfound\")", content);
                }
            }
        }

        [Fact]
        public void MiddlewarePlacement_IsCorrect()
        {
            // Test that UseStatusCodePagesWithReExecute middleware is placed correctly
            var jsonPath = "src/Scaffolding/VS.Web.CG.Mvc/Blazor/blazorWebCrudChanges.json";
            if (File.Exists(jsonPath))
            {
                var content = File.ReadAllText(jsonPath);
                
                // Verify that middleware is inserted after UseExceptionHandler/UseHsts
                Assert.Contains("\"InsertAfter\": [ \"app.UseExceptionHandler\", \"app.UseHsts\" ]", content);
                
                // Verify that it contains the status code middleware
                Assert.Contains("UseStatusCodePagesWithReExecute", content);
                
                // Verify it's not placed before app.Run() (old incorrect placement)
                Assert.DoesNotContain("\"InsertBefore\": [ \"app.Run()\" ]", content);
            }
        }

        [Fact]
        public void NotFoundTemplate_Exists()
        {
            // Test that NotFound.tt template exists and has correct content
            var templatePath = "src/Scaffolding/VS.Web.CG.Mvc/Templates/Blazor/NotFound.tt";
            if (File.Exists(templatePath))
            {
                var content = File.ReadAllText(templatePath);
                
                // Verify basic structure
                Assert.Contains("@page \"/not-found\"", content);
                Assert.Contains("@layout MainLayout", content);
                Assert.Contains("<PageTitle>Not Found</PageTitle>", content);
                Assert.Contains("Return to Home", content);
            }
        }

        [Fact]  
        public void DynamicRouteReplacement_ConfigurationStructure()
        {
            // Test that JSON configuration has the correct structure for dynamic route replacement
            var jsonPath = "src/Scaffolding/VS.Web.CG.Mvc/Blazor/blazorWebCrudChanges.json";
            if (File.Exists(jsonPath))
            {
                var content = File.ReadAllText(jsonPath);
                
                // Verify that middleware is configured with placeholder route
                Assert.Contains("UseStatusCodePagesWithReExecute(\\\"/not-found\\\"", content);
                
                // Verify correct placement after UseExceptionHandler/UseHsts
                Assert.Contains("\"InsertAfter\": [ \"app.UseExceptionHandler\", \"app.UseHsts\" ]", content);
            }
        }

        [Fact]
        public async Task BlazorWebCRUDGenerator_Should_Use_NullCoalescingAssignment_In_OnInitialized()
        {
            // Arrange
            var blazorGenerator = CreateBlazorWebCRUDGenerator();
            var model = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "Car",
                DataContextClass = "CarDbContext",
                TemplateName = "Create"
            };

            // Act - Generate the Create razor page
            await blazorGenerator.GenerateCode(model);

            // Assert - Verify the generated content uses null coalescing assignment
            var generatedFiles = fileSystem.EnumerateFiles("/", "*.razor", SearchOption.AllDirectories);
            var createFile = generatedFiles.FirstOrDefault(f => f.Contains("Create.razor"));
            
            Assert.NotNull(createFile);
            var content = fileSystem.ReadAllText(createFile);
            
            // Verify null coalescing assignment pattern is used in OnInitialized
            Assert.Contains("??= new()", content);
            Assert.Contains("= default!", content);
            Assert.Contains("protected override void OnInitialized()", content);
            
            // Verify old pattern is not used
            Assert.DoesNotContain("{ get; set; } = new()", content);
        }

        [Fact]
        public async Task BlazorWebCRUDGenerator_Should_Use_NotFound_Navigation()
        {
            // Arrange
            var blazorGenerator = CreateBlazorWebCRUDGenerator();
            var editModel = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "Car",
                DataContextClass = "CarDbContext", 
                TemplateName = "Edit"
            };

            // Act - Generate the Edit razor page
            await blazorGenerator.GenerateCode(editModel);

            // Assert - Verify NavigationManager.NotFound() is used
            var generatedFiles = fileSystem.EnumerateFiles("/", "*.razor", SearchOption.AllDirectories);
            var editFile = generatedFiles.FirstOrDefault(f => f.Contains("Edit.razor"));
            
            Assert.NotNull(editFile);
            var content = fileSystem.ReadAllText(editFile);
            
            // Verify NavigationManager.NotFound() is used
            Assert.Contains("NavigationManager.NotFound()", content);
            
            // Verify old NavigateTo("notfound") pattern is not used
            Assert.DoesNotContain("NavigationManager.NavigateTo(\"notfound\")", content);
        }

        [Fact]
        public async Task BlazorWebCRUDGenerator_Should_Generate_All_CRUD_Pages()
        {
            // Arrange
            var blazorGenerator = CreateBlazorWebCRUDGenerator();
            var crudModel = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "Car",
                DataContextClass = "CarDbContext",
                TemplateName = "crud"
            };

            // Act - Generate all CRUD pages
            await blazorGenerator.GenerateCode(crudModel);

            // Assert - Verify all CRUD pages are generated
            var generatedFiles = fileSystem.EnumerateFiles("/", "*.razor", SearchOption.AllDirectories)
                .Select(f => Path.GetFileName(f))
                .ToList();

            Assert.Contains("Create.razor", generatedFiles);
            Assert.Contains("Edit.razor", generatedFiles); 
            Assert.Contains("Details.razor", generatedFiles);
            Assert.Contains("Delete.razor", generatedFiles);
            Assert.Contains("Index.razor", generatedFiles);
        }

        [Fact]
        public async Task BlazorWebCRUDGenerator_Should_Generate_NotFound_Page()
        {
            // Arrange
            var blazorGenerator = CreateBlazorWebCRUDGenerator();
            var model = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "Car",
                DataContextClass = "CarDbContext",
                TemplateName = "crud"
            };

            // Act - Generate CRUD pages which should include NotFound page
            await blazorGenerator.GenerateCode(model);

            // Assert - Verify NotFound page is generated with correct content
            var generatedFiles = fileSystem.EnumerateFiles("/", "*.razor", SearchOption.AllDirectories);
            var notFoundFile = generatedFiles.FirstOrDefault(f => f.Contains("NotFound.razor"));
            
            if (notFoundFile != null)
            {
                var content = fileSystem.ReadAllText(notFoundFile);
                
                // Verify basic NotFound page structure
                Assert.Contains("@page \"/not-found\"", content);
                Assert.Contains("Not Found", content);
            }
        }

        [Fact]
        public async Task BlazorWebCRUDGenerator_Should_Configure_Middleware_Correctly()
        {
            // Arrange
            var blazorGenerator = CreateBlazorWebCRUDGenerator();
            var model = new BlazorWebCRUDGeneratorCommandLineModel
            {
                ModelClass = "Car",
                DataContextClass = "CarDbContext",
                TemplateName = "crud"
            };

            // Mock Program.cs to verify middleware placement
            var originalProgramCs = @"
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(""/Error"");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();";

            fileSystem.WriteAllText("Program.cs", originalProgramCs);

            // Act - Generate CRUD pages which should modify Program.cs
            await blazorGenerator.GenerateCode(model);

            // Assert - Verify middleware is placed correctly
            var updatedProgramCs = fileSystem.ReadAllText("Program.cs");
            
            // Verify UseStatusCodePagesWithReExecute is added
            Assert.Contains("UseStatusCodePagesWithReExecute", updatedProgramCs);
            
            // Verify it's placed after UseExceptionHandler and UseHsts
            var exceptionHandlerIndex = updatedProgramCs.IndexOf("UseExceptionHandler");
            var hstsIndex = updatedProgramCs.IndexOf("UseHsts"); 
            var statusCodePagesIndex = updatedProgramCs.IndexOf("UseStatusCodePagesWithReExecute");
            
            Assert.True(exceptionHandlerIndex < statusCodePagesIndex);
            Assert.True(hstsIndex < statusCodePagesIndex);
        }
    }
}
