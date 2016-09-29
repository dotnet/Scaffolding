using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace E2E_Test
{
    [Collection("ScaffoldingE2ECollection")]
    public class MvcControllerTest : E2ETestBase
    {
        private static string[] EMPTY_CONTROLLER_ARGS = new string[] { "-p", ".", "controller", "--controllerName", "EmptyController" };
        private static string[] EMPTY_CONTROLLER_WITH_RELATIVE_PATH = new string[] { "-p", ".", "controller", "--controllerName", "EmptyController", "--relativeFolderPath", "Controllers" };
        private static string[] READ_WRITE_CONTROLLER = new string[] { "-p", ".", "controller", "--controllerName", "ActionsController", "--readWriteActions" };

        public MvcControllerTest(ScaffoldingE2ETestFixture fixture) : base(fixture)
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

        [Theory(Skip="Disabling E2E test"), MemberData("TestData")]
        public void TestControllerGenerators(string baselineFile, string generatedFilePath, string[] args)
        {
            Scaffold(args);

            generatedFilePath = Path.Combine(_testProjectPath, generatedFilePath);
            VerifyFileAndContent(generatedFilePath, baselineFile);

            _fixture.FilesToCleanUp.Add(generatedFilePath);
        }

        [Fact(Skip = "Disabling E2E test")]
        public void TestControllerWithContext()
        {
            var args = new string[]
            {
                "-p",
                _testProjectPath,
                "controller",
                "--controllerName",
                "CarsController",
                "--model",
                "WebApplication1.Models.Car",
                "--dataContext",
                "WebApplication1.Models.CarContext",
                "--noViews"
            };

            Scaffold(args);
            var generatedFilePath = Path.Combine(_testProjectPath, "CarsController.cs");
            VerifyFileAndContent(generatedFilePath, "CarsController.txt");

            _fixture.FilesToCleanUp.Add(generatedFilePath);
        }

        [Fact(Skip = "Disabling E2E test")]
        public void TestControllerWithContext_WithViews()
        {
            var args = new string[]
            {
                "-p",
                _testProjectPath,
                "controller",
                "--controllerName",
                "CarsWithViewController",
                "--model",
                "WebApplication1.Models.Car",
                "--dataContext",
                "WebApplication1.Models.CarContext",
                "--referenceScriptLibraries",
                "--relativeFolderPath",
                Path.Combine("Areas", "Test", "Controllers")
            };

            Scaffold(args);

            var generatedFilePath = Path.Combine(_testProjectPath, "Areas", "Test", "Controllers", "CarsWithViewController.cs");
            var viewFolder = Path.Combine(_testProjectPath, "Areas", "Test", "Views", "CarsWithView");

            VerifyFileAndContent(generatedFilePath, "CarsWithViewController.txt");
            VerifyFileAndContent(Path.Combine(viewFolder, "Create.cshtml"), Path.Combine("CarViews", "Create.cshtml"));
            VerifyFileAndContent(Path.Combine(viewFolder, "Delete.cshtml"), Path.Combine("CarViews", "Delete.cshtml"));
            VerifyFileAndContent(Path.Combine(viewFolder, "Details.cshtml"), Path.Combine("CarViews", "Details.cshtml"));
            VerifyFileAndContent(Path.Combine(viewFolder, "Edit.cshtml"), Path.Combine("CarViews", "Edit.cshtml"));
            VerifyFileAndContent(Path.Combine(viewFolder, "Index.cshtml"), Path.Combine("CarViews", "Index.cshtml"));
            VerifyFileAndContent(Path.Combine(_testProjectPath, "Views", "Shared", "_ValidationScriptsPartial.cshtml"), Path.Combine("SharedViews", "_ValidationScriptsPartial.cshtml"));

            _fixture.FilesToCleanUp.Add(generatedFilePath);
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Create.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Index.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Delete.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Details.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Edit.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Index.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(_testProjectPath, "Views", "Shared", "_ValidationScriptsPartial.cshtml"));
        }

        [Fact(Skip = "Disabling E2E test")]
        public void TestControllerWithContext_WithForeignKey()
        {
            var args = new string[]
            {
                "-p",
                _testProjectPath,
                "controller",
                "--controllerName",
                "ProductsController",
                "--model",
                "WebApplication1.Models.Product",
                "--dataContext",
                "WebApplication1.Models.ProductContext"
            };

            Scaffold(args);

            var generatedFilePath = Path.Combine(_testProjectPath, "ProductsController.cs");
            var viewFolder = Path.Combine(_testProjectPath, "Views", "Products");

            VerifyFileAndContent(generatedFilePath, "ProductsController.txt");
            VerifyFileAndContent(Path.Combine(viewFolder, "Create.cshtml"), Path.Combine("ProductViews", "Create.cshtml"));
            VerifyFileAndContent(Path.Combine(viewFolder, "Delete.cshtml"), Path.Combine("ProductViews", "Delete.cshtml"));
            VerifyFileAndContent(Path.Combine(viewFolder, "Details.cshtml"), Path.Combine("ProductViews", "Details.cshtml"));
            VerifyFileAndContent(Path.Combine(viewFolder, "Edit.cshtml"), Path.Combine("ProductViews", "Edit.cshtml"));
            VerifyFileAndContent(Path.Combine(viewFolder, "Index.cshtml"), Path.Combine("ProductViews", "Index.cshtml"));

            _fixture.FilesToCleanUp.Add(generatedFilePath);
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Create.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Index.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Delete.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Details.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Edit.cshtml"));
            _fixture.FilesToCleanUp.Add(Path.Combine(viewFolder, "Index.cshtml"));
        }
    }
}
