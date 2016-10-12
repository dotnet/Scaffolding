// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using Moq;
using NuGet.Frameworks;
using Xunit;

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
        private ProjectInformation _projectInformation;

        private EntityFrameworkServices GetEfServices(string path, string applicationName)
        {
            _appInfo = new ApplicationInfo(applicationName, path, "Debug");
            _logger = new ConsoleLogger();
            _packageInstaller = new Mock<IPackageInstaller>();
            _serviceProvider = new Mock<IServiceProvider>();

            _projectInformation = GetProjectInformation(path, false);
            _workspace = new RoslynWorkspace(_projectInformation);
            _loader = new TestAssemblyLoadContext(_projectInformation.RootProject);
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
            var compilationService = new RoslynCompilationService(_appInfo, _loader, _projectInformation.RootProject);
            var templatingService = new Templating.RazorTemplating(compilationService);
            _dbContextEditorServices = new DbContextEditorServices(_projectInformation.RootProject, _appInfo, filesLocator, templatingService);

            return new EntityFrameworkServices(
                _projectInformation.RootProject,
                _appInfo,
                _loader,
                _modelTypesLocator,
                _dbContextEditorServices,
                _packageInstaller.Object,
                _serviceProvider.Object,
                _workspace,
                _logger);

        }

        private ProjectInformation GetProjectInformation(string path, bool isMsBuild)
        {
            if (isMsBuild)
            {
                var rootContext = new MsBuildProjectContextBuilder()
                    .AsDesignTimeBuild()
                    .WithTargetFramework(FrameworkConstants.CommonFrameworks.NetCoreApp10)
                    .WithConfiguration("Debug")
                    .Build();
                // TODO needs to be fixed to get all dependent Projects as well.
                return new ProjectInformation(rootContext, null);
            }
            else
            {
                var context = Microsoft.DotNet.ProjectModel.ProjectContext.Create(path, FrameworkConstants.CommonFrameworks.NetStandard16);
                var dotnetContext = new DotNetProjectContext(context, "Debug", null);
                var dependencyProjectContexts = new List<Microsoft.DotNet.ProjectModel.ProjectContext>();
                foreach (var dependency in dotnetContext.ProjectReferences)
                {
                    var dependencyContext = Microsoft.DotNet.ProjectModel.ProjectContext.Create(dependency, FrameworkConstants.CommonFrameworks.NetStandard16);
                    dependencyProjectContexts.Add(dependencyContext);
                }

                return new ProjectInformation(dotnetContext,
                    dependencyProjectContexts.Select(d => new DotNetProjectContext(d, "Debug", null)));
            }
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

        [Fact/*(Skip ="Need to update workspace creation for this to work.")*/]
        public async void TestGetModelMetadata_WithDbContext()
        {
            var appName = "ModelTypesLocatorTestWebApp";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "TestApps", appName);
            var efServices = GetEfServices(path, appName);

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
