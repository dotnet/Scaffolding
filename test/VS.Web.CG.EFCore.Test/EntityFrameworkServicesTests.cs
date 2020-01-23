// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using Moq;
using NuGet.Frameworks;
using Xunit;
using Xunit.Abstractions;
using IProjectContext = Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel.IProjectContext;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class EntityFrameworkServicesTests
    {
        private IApplicationInfo _appInfo;
        private ICodeGenAssemblyLoadContext _loader;
        private IModelTypesLocator _modelTypesLocator;
        private IDbContextEditorServices _dbContextEditorServices;
        private Mock<IPackageInstaller> _packageInstaller;
        private Mock<IServiceProvider> _serviceProvider;
        private CodeAnalysis.Workspace _workspace;
        private ILogger _logger;
        private IProjectContext _projectContext;
        private ITestOutputHelper _output;

        public EntityFrameworkServicesTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private EntityFrameworkServices GetEfServices(string path, string applicationName, bool useSqlite)
        {
            _appInfo = new ApplicationInfo(applicationName, Path.GetDirectoryName(path), "Debug");
            _logger = new ConsoleLogger();
            _packageInstaller = new Mock<IPackageInstaller>();
            _serviceProvider = new Mock<IServiceProvider>();

            _projectContext = GetProjectInformation(path);
            _workspace = new RoslynWorkspace(_projectContext);
            _loader = new TestAssemblyLoadContext(_projectContext);
            _modelTypesLocator = new ModelTypesLocator(_workspace);
            var dbContextMock = new Mock<IDbContextEditorServices>();
            var editSyntaxTreeResult = new EditSyntaxTreeResult()
            {
                Edited = true
            };

            dbContextMock.Setup(db => db.EditStartupForNewContext(It.IsAny<ModelType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                useSqlite))
                .Returns(editSyntaxTreeResult);

            var connectionStringsWriter = new Mock<IConnectionStringsWriter>();
            connectionStringsWriter.Setup(c => c.AddConnectionString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));

            var filesLocator = new FilesLocator();
            var compilationService = new RoslynCompilationService(_appInfo, _loader, _projectContext);
            var templatingService = new Templating.RazorTemplating(compilationService);
            _dbContextEditorServices = new DbContextEditorServices(_projectContext, _appInfo, filesLocator, templatingService, connectionStringsWriter.Object);

            return new EntityFrameworkServices(
                _projectContext,
                _appInfo,
                _loader,
                _modelTypesLocator,
                _dbContextEditorServices,
                _packageInstaller.Object,
                _serviceProvider.Object,
                _workspace,
                DefaultFileSystem.Instance,
                _logger);

        }

        private IProjectContext GetProjectInformation(string path)
        {
            var rootContext = new MsBuildProjectContextBuilder(path, "Dummy")
                .Build();
            return rootContext;
        }


        private void SetupProjects(TemporaryFileProvider fileProvider)
        {
            new MsBuildProjectSetupHelper().SetupProjects(fileProvider, _output);
        }

        [Fact (Skip = "Need to workaround the fact that the test doesn't run in the project's dependency context.")]
        public async void TestGetModelMetadata_WithDbContext()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                SetupProjects(fileProvider);

                var appName = MsBuildProjectStrings.RootProjectName;
                var path = Path.Combine(fileProvider.Root, "Root", appName);
                var efServices = GetEfServices(path, appName, false);
                var efServicesSqlite = GetEfServices(path, appName, true);

                var modelType = _modelTypesLocator.GetType("Library1.Models.Car").First();
                var metadata = await efServices.GetModelMetadata("TestProject.Models.CarContext", modelType, string.Empty, false);
                var metadataSqlite = await efServicesSqlite.GetModelMetadata("TestProject.Models.CarContext", modelType, string.Empty, true);

                Assert.Equal(ContextProcessingStatus.ContextAvailable, metadata.ContextProcessingStatus);
                Assert.Equal(3, metadata.ModelMetadata.Properties.Length);

                Assert.Equal(ContextProcessingStatus.ContextAvailable, metadataSqlite.ContextProcessingStatus);
                Assert.Equal(3, metadataSqlite.ModelMetadata.Properties.Length);
            }
        }
    }
}
