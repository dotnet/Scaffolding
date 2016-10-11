// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;
using Moq;
using Xunit;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using NuGet.Frameworks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    //[Collection("CodeGeneration.EF")]
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

        private EntityFrameworkServices GetEfServices(string path, string applicationName)
        {
            _appInfo = new ApplicationInfo(applicationName, path, "Debug");
            _logger = new ConsoleLogger();
            _packageInstaller = new Mock<IPackageInstaller>();
            _serviceProvider = new Mock<IServiceProvider>();

            _projectContext = GetProjectContext(path, false);
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
                It.IsAny<string>()))
                .Returns(editSyntaxTreeResult);

            var filesLocator = new FilesLocator();
            var compilationService = new RoslynCompilationService(_appInfo, _loader, _projectContext);
            var templatingService = new Templating.RazorTemplating(compilationService);
            _dbContextEditorServices = new DbContextEditorServices(_projectContext, _appInfo, filesLocator, templatingService);

            return new EntityFrameworkServices(
                _projectContext,
                _appInfo,
                _loader,
                _modelTypesLocator,
                _dbContextEditorServices,
                _packageInstaller.Object,
                _serviceProvider.Object,
                _workspace,
                _logger);

        }

        private IProjectContext GetProjectContext(string path, bool isMsBuild)
        {
            if (isMsBuild)
            {
                return new MsBuildProjectContextBuilder()
                    .AsDesignTimeBuild()
                    .WithTargetFramework(FrameworkConstants.CommonFrameworks.NetCoreApp10)
                    .WithConfiguration("Debug")
                    .Build();
            }
            else
            {
                var context = Microsoft.DotNet.ProjectModel.ProjectContext.Create(path, FrameworkConstants.CommonFrameworks.NetStandard16);
                return new Microsoft.Extensions.ProjectModel.DotNetProjectContext(context, "Debug", null);
            }
        }

        [Fact(Skip ="Need to update workspace creation for this to work.")]
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
            var appName = "ModelTypesLocatorTestClassLibrary";
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
