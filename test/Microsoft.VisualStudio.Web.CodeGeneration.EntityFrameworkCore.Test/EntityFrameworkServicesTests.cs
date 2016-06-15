using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Workspaces;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class EntityFrameworkServicesTests
    {
        private ILibraryManager _libraryManager;
        private ILibraryExporter _libraryExporter;
        private IApplicationInfo _appInfo;
        private ICodeGenAssemblyLoadContext _loader;
        private IModelTypesLocator _modelTypesLocator;
        private IDbContextEditorServices _dbContextEditorServices;
        private Mock<IPackageInstaller> _packageInstaller;
        private Mock<IServiceProvider> _serviceProvider;
        private CodeAnalysis.Workspace _workspace;
        private ILogger _logger;

        public EntityFrameworkServicesTests()
        {
        }

        private EntityFrameworkServices GetEfServices(string path, string applicationName)
        {
            _appInfo = new ApplicationInfo(applicationName, path, "Debug");
            _logger = new ConsoleLogger();
            _packageInstaller = new Mock<IPackageInstaller>();
            _serviceProvider = new Mock<IServiceProvider>();

            var context = ProjectContext.CreateContextForEachFramework(path).First();
            _workspace = context.CreateRoslynWorkspace();
            _libraryExporter = new LibraryExporter(context, _appInfo);
            _libraryManager = new LibraryManager(context);
            _loader = new TestAssemblyLoadContext(_libraryExporter, _libraryManager);
            _modelTypesLocator = new ModelTypesLocator(_libraryExporter, _workspace);
            var dbContextMock = new Mock<IDbContextEditorServices>();
            var editSyntaxTreeResult = new EditSyntaxTreeResult()
            {
                Edited = true
            };
            dbContextMock.Setup(db => db.EditStartupForNewContext(It.IsAny<ModelType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(editSyntaxTreeResult);

            var filesLocator = new FilesLocator();
            var compilationService = new RoslynCompilationService(_appInfo, _loader, _libraryExporter);
            var templatingService = new Templating.RazorTemplating(compilationService);
            _dbContextEditorServices = new DbContextEditorServices(_libraryManager, _appInfo, filesLocator, templatingService);

            return new EntityFrameworkServices(
                _libraryManager,
                _libraryExporter,
                _appInfo,
                _loader,
                _modelTypesLocator,
                _dbContextEditorServices,
                _packageInstaller.Object,
                _serviceProvider.Object,
                _workspace,
                _logger);

        }

        [Fact]
        public async void TestGetModelMetadata_WithoutDbContext()
        {
            var appName = "ModelTypesLocatorTestClassLibrary";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "TestApps", appName);
            var efServices = GetEfServices(path, appName);

            var modelType = _modelTypesLocator.GetType("ModelWithMatchingShortName").First();
            var metadata = await efServices.GetModelMetadata(modelType);
            Assert.Equal(ContextProcessingStatus.ContextAvailable, metadata.ContextProcessingStatus);
            Assert.Null(metadata.ModelMetadata.Navigations);
            Assert.False(metadata.ModelMetadata.Properties.Any());

            modelType = _modelTypesLocator.GetType("ModelTypesLocatorTestClassLibrary.Car").First();
            metadata = await efServices.GetModelMetadata(modelType);
            Assert.Equal(ContextProcessingStatus.ContextAvailable, metadata.ContextProcessingStatus);
            Assert.Null(metadata.ModelMetadata.Navigations);
            Assert.Null(metadata.ModelMetadata.PrimaryKeys);
            Assert.Equal(3, metadata.ModelMetadata.Properties.Length);
        }

        [Fact]
        public async void TestGetModelMetadata_WithDbContext()
        {
            var appName = "ModelTypesLocatorTestWebApp";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "TestApps", appName);
            var efServices = GetEfServices(path, appName);

            // We need to build project here because the model type is in a dependency of the project. 
            // So we need to have an assembly which gets loaded for retrieving the type.
            BuildProject(path);
            var modelType = _modelTypesLocator.GetType("ModelTypesLocatorTestClassLibrary.Car").First();
            var metadata = await efServices.GetModelMetadata("ModelTypesLocatorTestWebApp.Models.CarContext", modelType);

            Assert.Equal(ContextProcessingStatus.ContextAvailable, metadata.ContextProcessingStatus);
            Assert.Equal(3, metadata.ModelMetadata.Properties.Length);
        }

        private int BuildProject(string project)
        {
            var args = new List<string>()
            {
                project,
                "--configuration", "Debug"
            };
            var command = Command.CreateDotNet(
                    "build",
                    args);

            var result = command.Execute();

            return result.ExitCode;
        }
    }
}
