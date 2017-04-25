// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    public class MvcControllerTest : E2ETestBase
    {
        private static string[] EMPTY_CONTROLLER_ARGS = new string[] { codegeneratorToolName, "-p", ".", "controller", "--controllerName", "EmptyController" };
        private static string[] EMPTY_CONTROLLER_WITH_RELATIVE_PATH = new string[] { codegeneratorToolName, "-p", ".", "controller", "--controllerName", "EmptyController", "--relativeFolderPath", "Controllers" };
        private static string[] READ_WRITE_CONTROLLER = new string[] { codegeneratorToolName, "-p", ".", "controller", "--controllerName", "ActionsController", "--readWriteActions" };


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

        [Theory(Skip="Need new CLI that can run tools on netcoreapp2.0"), MemberData("TestData")]
        public void TestControllerGenerators(string baselineFile, string generatedFilePath, string[] args)
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                Scaffold(args, TestProjectPath);

                generatedFilePath = Path.Combine(TestProjectPath, generatedFilePath);
                VerifyFileAndContent(generatedFilePath, baselineFile);
            }
        }

        [Fact (Skip ="Disable tests that need loading assemblies in insideman")]
        public void TestControllerWithContext()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var args = new string[]
                {
                    "aspnet-codegenerator",
                    "-p",
                    TestProjectPath,
                    "controller",
                    "--controllerName",
                    "CarsController",
                    "--model",
                    "Library1.Models.Car",
                    "--dataContext",
                    "WebApplication1.Models.CarContext",
                    "--noViews"
                };

                Scaffold(args, TestProjectPath);
                var generatedFilePath = Path.Combine(TestProjectPath, "CarsController.cs");
                VerifyFileAndContent(generatedFilePath, "CarsController.txt");
            }
        }

        [Fact(Skip = "Disable tests that need loading assemblies in insideman")]
        public void TestControllerWithContext_WithViews()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                var args = new string[]
                {
                    codegeneratorToolName,
                    "-p",
                    TestProjectPath,
                    "controller",
                    "--controllerName",
                    "CarsWithViewController",
                    "--model",
                    "Library1.Models.Car",
                    "--dataContext",
                    "WebApplication1.Models.CarContext",
                    "--referenceScriptLibraries",
                    "--relativeFolderPath",
                    Path.Combine("Areas", "Test", "Controllers")
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

        [Fact(Skip = "Disable tests that need loading assemblies in insideman")]
        public void TestControllerWithContext_WithForeignKey()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                new MsBuildProjectSetupHelper().SetupProjects(fileProvider, Output, true);
                TestProjectPath = Path.Combine(fileProvider.Root, "Root");
                Directory.SetCurrentDirectory(TestProjectPath);
                var args = new string[]
                {
                    codegeneratorToolName,
                    "-p",
                    TestProjectPath,
                    "controller",
                    "--controllerName",
                    "ProductsController",
                    "--model",
                    "WebApplication1.Models.Product",
                    "--dataContext",
                    "WebApplication1.Models.ProductContext"
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

        [Fact (Skip = "Disable tests that need loading assemblies in insideman")]
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
                    codegeneratorToolName,
                    "-p",
                    TestProjectPath,
                    "controller",
                    "--controllerName",
                    "ActionsController",
                    "--readWriteActions"
                };

                Scaffold(args, TestProjectPath);

                var generatedFilePath = Path.Combine(TestProjectPath, "ActionsController.cs");

                VerifyFileAndContent(generatedFilePath, "ReadWriteController.txt");
            }
        }
    }
}
