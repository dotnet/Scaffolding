// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.ProjectInfo;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    [Collection("CodeGeneration.EF")]
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
        private IProjectDependencyProvider _projectDependencyProvider;
        private EFTestFixture _testFixture;

        public EntityFrameworkServicesTests(EFTestFixture testFixture)
        {
            _testFixture = testFixture;
        }

        private EntityFrameworkServices GetEfServices(string path, string applicationName)
        {
            _appInfo = new ApplicationInfo(applicationName, path, "Debug");
            _logger = new ConsoleLogger();
            _packageInstaller = new Mock<IPackageInstaller>();
            _serviceProvider = new Mock<IServiceProvider>();

            _workspace = _testFixture.Workspace;
            _projectDependencyProvider = _testFixture.ProjectInfo.ProjectDependencyProvider;
            _loader = new TestAssemblyLoadContext(_projectDependencyProvider);
            _modelTypesLocator = new ModelTypesLocator(_workspace);
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
            var compilationService = new RoslynCompilationService(_appInfo, _loader, _projectDependencyProvider);
            var templatingService = new Templating.RazorTemplating(compilationService);
            _dbContextEditorServices = new DbContextEditorServices(_projectDependencyProvider, _appInfo, filesLocator, templatingService);

            return new EntityFrameworkServices(
                _projectDependencyProvider,
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
            var appName = "ModelTypesTestLibrary";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "TestApps", appName);
            var efServices = GetEfServices(path, appName);

            var modelType = _modelTypesLocator.GetType("ModelWithMatchingShortName").First();
            var metadata = await efServices.GetModelMetadata(modelType);
            Assert.Equal(ContextProcessingStatus.ContextAvailable, metadata.ContextProcessingStatus);
            Assert.Null(metadata.ModelMetadata.Navigations);
            Assert.False(metadata.ModelMetadata.Properties.Any());

            modelType = _modelTypesLocator.GetType("ModelTypesTestLibrary.Car").First();
            metadata = await efServices.GetModelMetadata(modelType);
            Assert.Equal(ContextProcessingStatus.ContextAvailable, metadata.ContextProcessingStatus);
            Assert.Null(metadata.ModelMetadata.Navigations);
            Assert.Null(metadata.ModelMetadata.PrimaryKeys);
            Assert.Equal(3, metadata.ModelMetadata.Properties.Length);
        }

        [Fact(Skip ="Disable test because of https://github.com/dotnet/sdk/issues/200")]
        public async void TestGetModelMetadata_WithDbContext()
        {
            var appName = "ModelTypesTestLibrary";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "TestApps", appName);
            var efServices = GetEfServices(path, appName);

            // We need to build project here because the model type is in a dependency of the project. 
            // So we need to have an assembly which gets loaded for retrieving the type.
            BuildProject(path);
            var modelType = _modelTypesLocator.GetType("ModelTypesLocatorTestClassLibrary.Car").First();
            var metadata = await efServices.GetModelMetadata("ModelTypesLocatorTestWebApp.Models.CarContext", modelType, string.Empty);

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
