using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Test.Sources;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Identity;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class IdentityGeneratorTemplateModelBuilderTests
    {
        private IProjectContext _projectContext;
        private ICodeGenAssemblyLoadContext _loader;
        private Mock<ILogger> _logger;
        private ITestOutputHelper _output;

        public IdentityGeneratorTemplateModelBuilderTests(ITestOutputHelper output)
        {
            _output = output;
            _loader = new DefaultAssemblyLoadContext();
            _logger = new Mock<ILogger>(MockBehavior.Strict);

            _logger.Setup(l => l.LogMessage(It.IsAny<string>()));
        }

        [Fact]
        public async Task TestValidateAndBuild()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                SetupProjects(fileProvider);

                var workspace = GetWorkspace(Path.Combine(fileProvider.Root, "Root"));

                var commandLineModel = new IdentityGeneratorCommandLineModel()
                {
                    RootNamespace = "Test.Namespace",
                    UseSQLite = false
                };

                var applicationInfo = new ApplicationInfo("TestApp", "Sample");

                var builder = new IdentityGeneratorTemplateModelBuilder(
                    commandLineModel,
                    applicationInfo,
                    _projectContext,
                    workspace,
                    _loader,
                    new DefaultFileSystem(),
                    _logger.Object);

                var templateModel = await builder.ValidateAndBuild();

                Assert.Equal(commandLineModel.RootNamespace, templateModel.Namespace);
                Assert.Equal("TestAppIdentityDbContext", templateModel.DbContextClass);
                Assert.Equal("Test.Namespace.Areas.Identity.Data", templateModel.DbContextNamespace);
                Assert.False(templateModel.IsUsingExistingDbContext);

                Assert.Equal("IdentityUser", templateModel.UserClass);
                Assert.False(templateModel.IsGenerateCustomUser);
            }
        }

        private static readonly string LayoutFileLocationTestProjectBasePath = "c:\\users\\test\\source\\repos\\Test1\\";

        // Tests for determining the support file location when an existing layout path is specified.
        // The input layout file path is relative to the project root.
        [Theory]
        [InlineData("Views/Shared/Layout.cshtml", "Views\\Shared", "Views/Shared/Layout.cshtml")]
        [InlineData("/V/S/Layout.cshtml", "V\\S", "V/S/Layout.cshtml")]
        [InlineData("~/Test/Dir/Layout.cshtml", "Test\\Dir", "Test/Dir/Layout.cshtml")]

        [InlineData("Custom\\Location\\Layout.cshtml", "Custom\\Location", "Custom\\Location\\Layout.cshtml")]
        [InlineData("\\My\\Files\\Layout.cshtml", "My\\Files", "My\\Files\\Layout.cshtml")]
        [InlineData("~\\Some\\Location\\Layout.cshtml", "Some\\Location", "Some\\Location\\Layout.cshtml")]

        [InlineData("\\\\/\\///\\Crazy\\Input\\Layout.cshtml", "Crazy\\Input", "Crazy\\Input\\Layout.cshtml")]
        public void SupportFileLocationForExistingLayoutFileTest(string existingLayoutFile, string expectedSupportFileLocation, string expectedLayoutFile)
        {
            IdentityGeneratorCommandLineModel commandLineModel = new IdentityGeneratorCommandLineModel();
            commandLineModel.Layout = existingLayoutFile;

            IApplicationInfo applicationInfo = new ApplicationInfo("test", LayoutFileLocationTestProjectBasePath);
            CommonProjectContext context = new CommonProjectContext();
            context.ProjectFullPath = LayoutFileLocationTestProjectBasePath;
            context.ProjectName = "TestProject";
            context.AssemblyName = "TestAssembly";
            context.CompilationItems = new List<string>();
            context.CompilationAssemblies = new List<ResolvedReference>();

            Workspace workspace = new RoslynWorkspace(context);
            ICodeGenAssemblyLoadContext assemblyLoadContext = new DefaultAssemblyLoadContext();
            IFileSystem mockFileSystem = new MockFileSystem();
            ILogger logger = new ConsoleLogger();

            IdentityGeneratorTemplateModelBuilder modelBuilder = new IdentityGeneratorTemplateModelBuilder(commandLineModel, applicationInfo, context, workspace, assemblyLoadContext, mockFileSystem, logger);

            modelBuilder.DetermineSupportFileLocation(out string supportFileLocation, out string layoutFile);
            Assert.Equal(expectedSupportFileLocation, supportFileLocation);
            Assert.Equal(expectedLayoutFile, layoutFile);
        }

        private Workspace GetWorkspace(string path)
        {
            _projectContext = new MsBuildProjectContextBuilder(path, "Dummy")
                .Build();

            return new RoslynWorkspace(_projectContext);
        }

        private void SetupProjects(TemporaryFileProvider fileProvider)
        {
            new MsBuildProjectSetupHelper().SetupProjectsForIdentityScaffolder(fileProvider, _output);
        }
    }
}
