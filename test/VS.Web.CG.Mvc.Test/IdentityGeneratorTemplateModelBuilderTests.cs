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
