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
using Xunit.Abstractions;

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
        private ITestOutputHelper _output;

        public EntityFrameworkServicesTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private EntityFrameworkServices GetEfServices(string path, string applicationName)
        {
            _appInfo = new ApplicationInfo(applicationName, path, "Debug");
            _logger = new ConsoleLogger();
            _packageInstaller = new Mock<IPackageInstaller>();
            _serviceProvider = new Mock<IServiceProvider>();

            _projectInformation = GetProjectInformation(path);
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

        private ProjectInformation GetProjectInformation(string path)
        {
            var rootContext = new MsBuildProjectContextBuilder()
                .AsDesignTimeBuild()
                .WithTargetFramework(FrameworkConstants.CommonFrameworks.NetCoreApp10)
                .WithConfiguration("Debug")
                .Build();
            // TODO needs to be fixed to get all dependent Projects as well.
            return new ProjectInformation(rootContext, null);
        }

        [Fact]
        public async void TestGetModelMetadata_WithoutDbContext()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                SetupProjects(fileProvider);

                var appName = "ModelTypesLocatorTestClassLibrary";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "TestApps", appName);
                var efServices = GetEfServices(path, appName);
                var modelType = _modelTypesLocator.GetType("ModelWithMatchingShortName").First();
                var metadata = await efServices.GetModelMetadata(modelType);
                Assert.Equal(ContextProcessingStatus.ContextAvailable, metadata.ContextProcessingStatus);
                Assert.Null(metadata.ModelMetadata.Navigations);
                Assert.False(metadata.ModelMetadata.Properties.Any());

                modelType = _modelTypesLocator.GetType("Library1.Models.Car").First();
                metadata = await efServices.GetModelMetadata(modelType);
                Assert.Equal(ContextProcessingStatus.ContextAvailable, metadata.ContextProcessingStatus);
                Assert.Null(metadata.ModelMetadata.Navigations);
                Assert.Null(metadata.ModelMetadata.PrimaryKeys);
                Assert.Equal(3, metadata.ModelMetadata.Properties.Length);
            }
        }

        private void SetupProjects(TemporaryFileProvider fileProvider)
        {
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
            fileProvider.Add("Nuget.config", MsBuildProjectStrings.NugetConfigTxt);

            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", MsBuildProjectStrings.RootProjectTxt);
            fileProvider.Add($"Root/Startup.cs", MsBuildProjectStrings.StartupTxt);

            fileProvider.Add($"Library1/{MsBuildProjectStrings.LibraryProjectName}", MsBuildProjectStrings.LibraryProjectTxt);
            fileProvider.Add($"Library1/ModelWithMatchingShortName.cs", "namespace Library1.Models { public class ModelWithMatchingShortName { } }");
            fileProvider.Add($"Library1/Car.cs", @"namespace Library1.Models
{
    public class Car
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int ManufacturerID { get; set; }
        public Manufacturer Manufacturer { get; set; }
    }

    public class Manufacturer
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Car> Cars { get; set; }
    }
}");
            var result = Command.CreateDotNet("restore3",
                new[] { Path.Combine(fileProvider.Root, "Root", "test.csproj") })
                .OnErrorLine(l => _output.WriteLine(l))
                .OnOutputLine(l => _output.WriteLine(l))
                .Execute();
        }

        [Fact]
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
    }
}
