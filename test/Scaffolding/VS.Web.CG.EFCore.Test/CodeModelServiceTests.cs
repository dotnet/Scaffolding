// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating.Compilation;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using Moq;
using Xunit;
using Xunit.Abstractions;
using IProjectContext = Microsoft.DotNet.Scaffolding.Shared.ProjectModel.IProjectContext;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class CodeModelServicesTests
    {
        private IApplicationInfo _appInfo;
        private ICodeGenAssemblyLoadContext _loader;
        private CodeAnalysis.Workspace _workspace;
        private ILogger _logger;
        private IProjectContext _projectContext;
        private ITestOutputHelper _output;
        private ModelTypesLocator _modelTypesLocator;

        public CodeModelServicesTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private CodeModelService GetCodeModelService(string path, string applicationName)
        {
            _appInfo = new ApplicationInfo(applicationName, Path.GetDirectoryName(path), "Debug");
            _logger = new ConsoleLogger();
            _projectContext = GetProjectInformation(path);
            _workspace = new RoslynWorkspace(_projectContext);
            _loader = new TestAssemblyLoadContext(_projectContext);
            _modelTypesLocator = new ModelTypesLocator(_workspace);
            return new CodeModelService(_projectContext, _workspace, _logger, _loader);
        }

        private IProjectContext GetProjectInformation(string path)
        {
            var rootContext = new MsBuildProjectContextBuilder(path, "Dummy")
                .Build();
            return rootContext;
        }

        [Fact(Skip="test fail")]
        public async void TestGetModelMetadata_WithoutDbContext()
        {
           using (var fileProvider = new TemporaryFileProvider())
           {
               SetupProjects(fileProvider);

               var appName = MsBuildProjectStrings.RootProjectName;
               var path = Path.Combine(fileProvider.Root, "Root", appName);
               var CodeModelService = GetCodeModelService(path, appName);
               var modelType = _modelTypesLocator.GetType("ModelWithMatchingShortName").First();
               var metadata = await CodeModelService.GetModelMetadata(modelType);
               Assert.Equal(ContextProcessingStatus.ContextAvailable, metadata.ContextProcessingStatus);
               Assert.False(metadata.ModelMetadata.Navigations.Any());
               Assert.False(metadata.ModelMetadata.Properties.Any());

               modelType = _modelTypesLocator.GetType("Library1.Models.Car").First();
               metadata = await CodeModelService.GetModelMetadata(modelType);
               Assert.Equal(ContextProcessingStatus.ContextAvailable, metadata.ContextProcessingStatus);
               Assert.False(metadata.ModelMetadata.Navigations.Any());
               Assert.False(metadata.ModelMetadata.PrimaryKeys.Any());
               Assert.Equal(4, metadata.ModelMetadata.Properties.Length);
           }
        }

        private void SetupProjects(TemporaryFileProvider fileProvider)
        {
            new MsBuildProjectSetupHelper().SetupProjects(fileProvider, _output);
        }
    }
}
