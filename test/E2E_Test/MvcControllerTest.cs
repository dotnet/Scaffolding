// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    [Collection("E2E_Tests")]
    public class MvcControllerTest : E2ETestBase
    {
        private static string[] EMPTY_CONTROLLER_ARGS = new string[] { "-c", Configuration, "controller", "--controllerName", "EmptyController" };
        private static string[] EMPTY_CONTROLLER_WITH_RELATIVE_PATH = new string[] { "-c", Configuration, "controller", "--controllerName", "EmptyController", "--relativeFolderPath", "Controllers" };
        private static string[] READ_WRITE_CONTROLLER = new string[] { "-c", Configuration, "controller",  "--controllerName", "ActionsController", "--readWriteActions" };


        public MvcControllerTest(ITestOutputHelper output)
            :base(output)
        {

        }

        public static IEnumerable<object[]> TestData
        {
            get
            {
                return new[]
                {
                    new object[] { "EmptyController.txt", "EmptyController.cs", EMPTY_CONTROLLER_ARGS },
                    new object[] { "EmptyController_withrelativepath.txt", Path.Combine("Controllers", "EmptyController.cs"), EMPTY_CONTROLLER_WITH_RELATIVE_PATH },
                    new object[] { "ReadWriteController.txt", "ActionsController.cs", READ_WRITE_CONTROLLER }
                };
            }
        }

        [Theory, MemberData(nameof(TestData))]
        public void TestControllerGenerators(string baselineFile, string generatedFilePath, string[] args)
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var invocationArgs = new [] {"-p", Path.Combine(TestProjectPath, "Test.csproj")}.Concat(args).ToArray();
                Scaffold(invocationArgs, TestProjectPath);

                generatedFilePath = Path.Combine(TestProjectPath, generatedFilePath);
                VerifyFileAndContent(generatedFilePath, baselineFile);
            }
        }

        [Fact]
        public void TestControllerWithContext()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var args = new string[]
                {
                    "-p",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "-c",
                    Configuration,
                    "controller",
                    "--controllerName",
                    "CarsController",
                    "--model",
                    "Library1.Models.Car",
                    "--dataContext",
                    "WebApplication1.Models.CarContext",
                    "--noViews",
                    "--bootstrapVersion",
                    "3"
                };

                Scaffold(args, TestProjectPath);
                var generatedFilePath = Path.Combine(TestProjectPath, "CarsController.cs");
                VerifyFileAndContent(generatedFilePath, "CarsController.txt");
            }
        }

        [Fact]
        public void TestApiController()
        {
            var controllerName = "TestApiController";

            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var args = new string[]
                {
                    "-p",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "-c",
                    Configuration,
                    "controller",
                    "--controllerName",
                    controllerName,
                    "--model",
                    "Library1.Models.Car",
                    "--dataContext",
                    "WebApplication1.Models.CarContext",
                    "--restWithNoViews",
                    "--bootstrapVersion",
                    "3"
                };

                Scaffold(args, TestProjectPath);
                var generatedFilePath = Path.Combine(TestProjectPath, $"{controllerName}.cs");
                VerifyFileAndContent(generatedFilePath, $"{controllerName}.txt");
            }
        }

        [Fact]
        public void TestControllerWithContext_WithViews()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var args = new string[]
                {
                    "-p",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "-c",
                    Configuration,
                    "controller",
                    "--controllerName",
                    "CarsWithViewController",
                    "--model",
                    "Library1.Models.Car",
                    "--dataContext",
                    "WebApplication1.Models.CarContext",
                    "--referenceScriptLibraries",
                    "--relativeFolderPath",
                    Path.Combine("Areas", "Test", "Controllers"),
                    "--bootstrapVersion",
                    "3"
                };

                Scaffold(args, TestProjectPath);

                var generatedFilePath = Path.Combine(TestProjectPath, "Areas", "Test", "Controllers", "CarsWithViewController.cs");
                var viewFolder = Path.Combine(TestProjectPath, "Areas", "Test", "Views", "CarsWithView");

                VerifyFileAndContent(generatedFilePath, "CarsWithViewController.txt");
                VerifyFileAndContent(Path.Combine(viewFolder, "Create.cshtml"), Path.Combine("CarViews", "Create.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Delete.cshtml"), Path.Combine("CarViews", "Delete.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Details.cshtml"), Path.Combine("CarViews", "Details.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Edit.cshtml"), Path.Combine("CarViews", "Edit.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Index.cshtml"), Path.Combine("CarViews", "Index.cshtml"));
                VerifyFileAndContent(Path.Combine(TestProjectPath, "Views", "Shared", "_ValidationScriptsPartial.cshtml"), Path.Combine("SharedViews", "_ValidationScriptsPartial.cshtml"));
            }
        }

        [Fact]
        public void TestControllerWithContext_WithForeignKey()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output, false);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                Directory.SetCurrentDirectory(TestProjectPath);
                var args = new string[]
                {
                    "-p",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "-c",
                    Configuration,
                    "controller",
                    "--controllerName",
                    "ProductsController",
                    "--model",
                    "WebApplication1.Models.Product",
                    "--dataContext",
                    "WebApplication1.Models.ProductContext",
                    "--bootstrapVersion",
                    "3"
                };

                Scaffold(args, TestProjectPath);

                var generatedFilePath = Path.Combine(TestProjectPath, "ProductsController.cs");
                var viewFolder = Path.Combine(TestProjectPath, "Views", "Products");

                VerifyFileAndContent(generatedFilePath, "ProductsController.txt");
                VerifyFileAndContent(Path.Combine(viewFolder, "Create.cshtml"), Path.Combine("ProductViews", "Create.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Delete.cshtml"), Path.Combine("ProductViews", "Delete.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Details.cshtml"), Path.Combine("ProductViews", "Details.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Edit.cshtml"), Path.Combine("ProductViews", "Edit.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Index.cshtml"), Path.Combine("ProductViews", "Index.cshtml"));
            }
        }

        [Fact]
        public void TestControllerWithoutEf()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjectsWithoutEF(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                Directory.SetCurrentDirectory(TestProjectPath);
                System.Console.WriteLine(fileProvider.Root);
                var args = new string[]
                {
                    "-p",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "-c",
                    Configuration,
                    "controller",
                    "--controllerName",
                    "ActionsController",
                    "--readWriteActions",
                    "--bootstrapVersion",
                    "3"
                };

                Scaffold(args, TestProjectPath);

                var generatedFilePath = Path.Combine(TestProjectPath, "ActionsController.cs");

                VerifyFileAndContent(generatedFilePath, "ReadWriteController.txt");
            }
        }

        [Fact]
        public void TestEFWithDbContextInDependency()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjectsWithDbContextInDependency(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var args = new string[]
                {
                    "-p",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "-c",
                    Configuration,
                    "controller",
                    "--controllerName",
                    "CarsWithViewController",
                    "--model",
                    "Library1.Models.Car",
                    "--dataContext",
                    "DAL.CarContext",
                    "--referenceScriptLibraries",
                    "--relativeFolderPath",
                    Path.Combine("Areas", "Test", "Controllers"),
                    "--bootstrapVersion",
                    "3"
                };

                Scaffold(args, TestProjectPath);

                var generatedFilePath = Path.Combine(TestProjectPath, "Areas", "Test", "Controllers", "CarsWithViewController.cs");
                var viewFolder = Path.Combine(TestProjectPath, "Areas", "Test", "Views", "CarsWithView");

                VerifyFileAndContent(generatedFilePath, "CarsControllerWithDAL.txt");
                VerifyFileAndContent(Path.Combine(viewFolder, "Create.cshtml"), Path.Combine("CarViews", "Create.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Delete.cshtml"), Path.Combine("CarViews", "Delete.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Details.cshtml"), Path.Combine("CarViews", "Details.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Edit.cshtml"), Path.Combine("CarViews", "Edit.cshtml"));
                VerifyFileAndContent(Path.Combine(viewFolder, "Index.cshtml"), Path.Combine("CarViews", "Index.cshtml"));
                VerifyFileAndContent(Path.Combine(TestProjectPath, "Views", "Shared", "_ValidationScriptsPartial.cshtml"), Path.Combine("SharedViews", "_ValidationScriptsPartial.cshtml"));
            }
        }

        [Fact]
        public void TestApiControllerWithModelPropertyInParentDbContextClass()
        {
            using (TemporaryFileProvider fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjectWithModelPropertyInParentDbContextClass(fileProvider, Output);
                TestProjectPath = fileProvider.Root;

                string[] args = new string[]
                {
                    "-p",
                    Path.Combine(TestProjectPath, "Test.csproj"),
                    "-c",
                    Configuration,
                    "controller",
                    "--controllerName",
                    "BlogsController",
                    "--model",
                    "Test.Models.Blog",
                    "--dataContext",
                    "Test.Data.DerivedDbContext",
                    "--relativeFolderPath",
                    "Controllers",
                    "--restWithNoViews",
                    "--bootstrapVersion",
                    "3"
                };

                Scaffold(args, TestProjectPath);
                string generatedFilePath = Path.Combine(TestProjectPath, "Controllers", "BlogsController.cs");

                VerifyFileAndContent(generatedFilePath, "BlogsController.txt");

                // verify that the db context wasn't modified, since the model is refernced in the base class.
                string derivedDataContextPath = Path.Combine(TestProjectPath, "Data", MsBuildProjectStrings.DerivedDbContextFileName);
                VerifyFileAndContent(derivedDataContextPath, "BlogsDerivedDbContext.txt");
            }
        }
    }
}
