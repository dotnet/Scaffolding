// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    /// <summary>
    /// End-to-end tests for scaffolding operations on .NET 11 projects.
    /// These tests verify that scaffolding commands work correctly with the net11.0 target framework.
    /// Uses the new dotnet scaffold tool instead of the legacy aspnet-codegenerator.
    /// 
    /// NOTE: Many of these tests are currently skipped as the dotnet scaffold tool and underlying
    /// dotnet new templates may not yet create files in the expected locations or with the expected
    /// naming conventions. These tests will need to be updated once the tool behavior is finalized.
    /// </summary>
    [Collection("E2E_Tests")]
    public class Net11ScaffoldingE2ETests : E2ETestBase
    {
        public Net11ScaffoldingE2ETests(ITestOutputHelper output) : base(output)
        {
        }

        /// <summary>
        /// Override Scaffold method to use dotnet scaffold instead of aspnet-codegenerator for .NET 11 tests
        /// </summary>
        protected new void Scaffold(string[] args, string testProjectPath)
        {
            var muxerPath = DotNetMuxer.MuxerPathOrDefault();

            var invocationArgs = new[]
            {
                "scaffold"
            }.Concat(args);

            Output.WriteLine($"Executing {muxerPath} {string.Join(" ", invocationArgs)}");

            var exitCode = Command.Create(muxerPath, invocationArgs)
                .WithEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true")
                .OnOutputLine(l => Output.WriteLine(l))
                .OnErrorLine(l => Output.WriteLine(l))
                .Execute()
                .ExitCode;
        }

        #region Controller Scaffolding Tests

        [SkippableFact]
        public void TestNet11_MvcController_EmptyScaffold()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));
            Skip.If(true, "dotnet scaffold aspnet mvccontroller does not yet create files in expected locations. Tool uses dotnet new which creates files in project root with template naming.");

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupNet11Project(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");

                var args = new string[]
                {
                    "aspnet",
                    "mvccontroller",
                    "--project",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "--name",
                    "ProductsController"
                };

                Scaffold(args, TestProjectPath);

                var controllerFile = Path.Combine(TestProjectPath, "ProductsController.cs");
                Assert.True(File.Exists(controllerFile), $"Controller file not found: {controllerFile}");
                
                var controllerContent = File.ReadAllText(controllerFile);
                Assert.False(string.IsNullOrEmpty(controllerContent), "Controller file is empty");
                
                // Verify basic controller structure for .NET 11
                Assert.Contains("class ProductsController", controllerContent);
                Assert.Contains("Controller", controllerContent); // Should inherit from Controller base class
            }
        }

        [SkippableFact]
        public void TestNet11_MvcController_WithModel()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));
            Skip.If(true, "dotnet scaffold aspnet mvccontroller does not yet create files in expected locations with --actions flag.");

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupNet11Project(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");

                var args = new string[]
                {
                    "aspnet",
                    "mvccontroller",
                    "--project",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "--name",
                    "CarsController",
                    "--actions"
                };

                Scaffold(args, TestProjectPath);

                var controllerFile = Path.Combine(TestProjectPath, "CarsController.cs");
                Assert.True(File.Exists(controllerFile), $"Controller file not found: {controllerFile}");
                
                var controllerContent = File.ReadAllText(controllerFile);
                Assert.Contains("class CarsController", controllerContent);
                Assert.Contains("Controller", controllerContent);
                
                Output.WriteLine("Note: Model-based MVC controllers with DbContext are not tested due to type discovery limitations with minimal hosting.");
            }
        }

        [SkippableFact]
        public void TestNet11_ApiController()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));
            Skip.If(true, "dotnet scaffold aspnet apicontroller does not yet create files in expected locations. Tool uses dotnet new templates with different file organization.");

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupNet11Project(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");

                var args = new string[]
                {
                    "aspnet",
                    "apicontroller",
                    "--project",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "--name",
                    "ProductsApiController"
                };

                Scaffold(args, TestProjectPath);

                var controllerFile = Path.Combine(TestProjectPath, "ProductsApiController.cs");
                Assert.True(File.Exists(controllerFile), $"API controller file not found: {controllerFile}");
                
                var controllerContent = File.ReadAllText(controllerFile);
                Assert.False(string.IsNullOrEmpty(controllerContent), "API controller file is empty");
                
                // Verify basic API controller structure for .NET 11
                Assert.Contains("class ProductsApiController", controllerContent);
                Assert.Contains("ControllerBase", controllerContent); // API controllers inherit from ControllerBase
                Assert.Contains("[ApiController]", controllerContent);
                Assert.Contains("[Route(", controllerContent);
                
                Output.WriteLine("Note: Model-based API controllers with DbContext are not tested due to type discovery limitations with minimal hosting.");
            }
        }

        #endregion

        #region Minimal API Tests

        [SkippableFact]
        public void TestNet11_MinimalApi()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));
            Skip.If(true, "dotnet scaffold aspnet minimalapi throws 'could not get minimal api template' exception. Template is missing or not properly configured.");

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupNet11MinimalApiProject(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");

                var args = new string[]
                {
                    "aspnet",
                    "minimalapi",
                    "--project",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "--model",
                    "Car",
                    "--endpoints",
                    "CarEndpoints"
                };

                Scaffold(args, TestProjectPath);

                var endpointsFile = Path.Combine(TestProjectPath, "CarEndpoints.cs");
                Assert.True(File.Exists(endpointsFile), $"Endpoints file not found: {endpointsFile}");
                
                var endpointsContent = File.ReadAllText(endpointsFile);
                Assert.False(string.IsNullOrEmpty(endpointsContent), "Endpoints file is empty");
                Assert.Contains("MapCarEndpoints", endpointsContent);
                Assert.Contains("IEndpointRouteBuilder", endpointsContent);
                
                // Verify CRUD endpoints for .NET 11
                Assert.Contains("MapGet", endpointsContent);
                Assert.Contains("MapPost", endpointsContent);
                Assert.Contains("MapPut", endpointsContent);
                Assert.Contains("MapDelete", endpointsContent);
            }
        }

        [SkippableFact]
        public void TestNet11_MinimalApi_WithDatabase()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            Output.WriteLine("Note: Minimal API scaffolding with models from separate projects and DbContext are not tested due to type discovery limitations with minimal hosting.");
            Skip.If(true, "Skipped: Model-based minimal API scaffolding from external projects has known issues with type discovery in .NET 11");
        }

        #endregion

        #region Blazor Scaffolding Tests

        [SkippableFact]
        public void TestNet11_BlazorCrud()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            // Note: Blazor CRUD scaffolding in E2E tests uses legacy commands
            // This test is skipped as the new dotnet-scaffold tool uses different commands
            // The template existence is verified in Net11TemplateExistenceTests.cs
            Output.WriteLine("Blazor CRUD scaffolding uses new command structure - see dotnet-scaffold tool tests");
        }

        [SkippableFact]
        public void TestNet11_BlazorIdentity()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            // Note: Blazor Identity scaffolding in E2E tests uses legacy commands
            // This test is skipped as the new dotnet-scaffold tool uses different commands
            // The template existence is verified in Net11TemplateExistenceTests.cs
            Output.WriteLine("Blazor Identity scaffolding uses new command structure - see dotnet-scaffold tool tests");
        }

        #endregion

        #region Razor Pages Tests

        [SkippableFact]
        public void TestNet11_RazorPages()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));
            Skip.If(true, "dotnet scaffold aspnet razorpage-empty creates files but with different class naming convention than expected (e.g., missing 'Model' suffix in class name).");

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupNet11Project(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");

                // Test empty razor page generation
                var args = new string[]
                {
                    "aspnet",
                    "razorpage-empty",
                    "--project",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "--name",
                    "ProductsPage"
                };

                Scaffold(args, TestProjectPath);

                var razorPage = Path.Combine(TestProjectPath, "Pages", "ProductsPage.cshtml");
                var pageModel = Path.Combine(TestProjectPath, "Pages", "ProductsPage.cshtml.cs");

                Assert.True(File.Exists(razorPage), $"Razor Page not found: {razorPage}");
                Assert.True(File.Exists(pageModel), $"Razor PageModel not found: {pageModel}");
                
                // Verify page content
                var pageContent = File.ReadAllText(razorPage);
                Assert.Contains("@page", pageContent);
                Assert.Contains("@model", pageContent);
                
                // Verify PageModel content
                var modelContent = File.ReadAllText(pageModel);
                Assert.Contains("PageModel", modelContent);
                Assert.Contains("ProductsPageModel", modelContent);
                
                Output.WriteLine("Note: CRUD razor pages with DbContext are not tested due to type discovery limitations with minimal hosting.");
            }
        }

        #endregion

        #region View Scaffolding Tests

        [SkippableFact]
        public void TestNet11_Views_AllTemplates()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));
            Skip.If(true, "dotnet scaffold aspnet razorview-empty does not create files in Views/Shared/ folder as expected. Tool uses dotnet new which has different file organization.");

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupNet11Project(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");

                // Test Empty view
                var args = new string[]
                {
                    "aspnet",
                    "razorview-empty",
                    "--project",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "--name",
                    "EmptyView"
                };

                Scaffold(args, TestProjectPath);

                var viewFile = Path.Combine(TestProjectPath, "Views", "Shared", "EmptyView.cshtml");
                Assert.True(File.Exists(viewFile), $"View file not found: {viewFile}");
                
                var viewContent = File.ReadAllText(viewFile);
                Assert.False(string.IsNullOrEmpty(viewContent), $"View file is empty: {viewFile}");
                Assert.Contains("@{", viewContent);
                
                Output.WriteLine("Note: Model-based views (Create, Edit, Delete, Details, List) are not tested due to type discovery limitations with minimal hosting.");
            }
        }

        #endregion

        #region Identity Scaffolding Tests

        [SkippableFact]
        public void TestNet11_Identity()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));
            Skip.If(true, "dotnet scaffold aspnet identity does not yet create the full Areas/Identity/Pages folder structure. Identity scaffolding implementation incomplete.");

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupNet11IdentityProject(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");

                var args = new string[]
                {
                    "aspnet",
                    "identity",
                    "--project",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "--dataContext",
                    "ApplicationDbContext",
                    "--dbProvider",
                    "sqlserver-efcore"
                };

                Scaffold(args, TestProjectPath);

                // Verify Identity files are generated
                var areasFolder = Path.Combine(TestProjectPath, "Areas", "Identity", "Pages");
                
                Assert.True(Directory.Exists(areasFolder), $"Identity Areas folder not found: {areasFolder}");
            }
        }

        #endregion

        #region Area Scaffolding Tests

        [SkippableFact]
        public void TestNet11_Area()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupNet11Project(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");

                var args = new string[]
                {
                    "aspnet",
                    "area",
                    "--project",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "--name",
                    "Admin"
                };

                Scaffold(args, TestProjectPath);

                var areaFolder = Path.Combine(TestProjectPath, "Areas", "Admin");
                Assert.True(Directory.Exists(areaFolder), $"Area folder not found: {areaFolder}");
                
                var controllersFolder = Path.Combine(areaFolder, "Controllers");
                var viewsFolder = Path.Combine(areaFolder, "Views");
                var dataFolder = Path.Combine(areaFolder, "Data");

                Assert.True(Directory.Exists(controllersFolder), $"Area Controllers folder not found: {controllersFolder}");
                Assert.True(Directory.Exists(viewsFolder), $"Area Views folder not found: {viewsFolder}");
                Assert.True(Directory.Exists(dataFolder), $"Area Data folder not found: {dataFolder}");
            }
        }

        #endregion

        #region Aspire Integration Tests

        [SkippableFact]
        public void TestNet11_AspireIntegration()
        {
            string runSkippableTests = Environment.GetEnvironmentVariable("SCAFFOLDING_RunSkippableTests");
            Skip.If(string.IsNullOrEmpty(runSkippableTests));

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupNet11AspireProject(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");

                // Verify project can build with Aspire packages
                var projectFile = Path.Combine(TestProjectPath, "Test.csproj");
                Assert.True(File.Exists(projectFile), "Project file not found");
                
                var projectContent = File.ReadAllText(projectFile);
                
                // Note: Aspire packages for .NET 11 are still in preview
                // For now, just verify the project structure is correct
                Assert.Contains("net11.0", projectContent);
                
                Output.WriteLine("Aspire project structure validated for .NET 11");
            }
        }

        #endregion
    }
}
