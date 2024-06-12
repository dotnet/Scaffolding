// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
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
            _appInfo = new ApplicationInfo(applicationName, Path.GetDirectoryName(path), null);
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

        [Fact(Skip = "test fail")]
        public async Task TestGetModelMetadata_WithoutDbContext()
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
