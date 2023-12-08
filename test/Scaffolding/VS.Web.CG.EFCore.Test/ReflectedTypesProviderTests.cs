// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
{
    public class ReflectedTypesProviderTests
    {
        private TestAssemblyLoadContext _loader;
        private ILogger _logger;
        private ITestOutputHelper _output;
        private IProjectContext _projectContext;
        private MsBuildProjectSetupHelper _projectSetupHelper;
        private RoslynWorkspace _workspace;

        public ReflectedTypesProviderTests(ITestOutputHelper output)
        {
            _output = output;
            _projectSetupHelper = new MsBuildProjectSetupHelper();
        }

        [Fact(Skip = "Disable tests on CI that need to load assemblies")]
        public async Task TestGetReflectedType()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                SetupProjectInformation(fileProvider);

                var compilation = await _workspace.CurrentSolution.Projects
                    .First()
                    .GetCompilationAsync();

                Func<CodeAnalysis.Compilation, CodeAnalysis.Compilation> func = (c) =>
                {
                    return c;
                };

                var reflectedTypesProvider = new ReflectedTypesProvider(
                    compilation,
                    func,
                    _projectContext,
                    _loader,
                    _logger);

                var carType = reflectedTypesProvider.GetReflectedType("Library1.Models.Car", false);
                Assert.Null(carType);

                carType = reflectedTypesProvider.GetReflectedType("Library1.Models.Car", true);
                Assert.NotNull(carType);
            }
        }

        [Fact(Skip = "test fail")]
        public async Task TestGetReflectedType_FailedCompilation()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                SetupProjectInformation(fileProvider);

                var compilation = await _workspace.CurrentSolution.Projects
                    .First()
                    .GetCompilationAsync();

                Func<CodeAnalysis.Compilation, CodeAnalysis.Compilation> func = (c) =>
                {
                    return c.WithReferences(null);
                };

                var reflectedTypesProvider = new ReflectedTypesProvider(
                    compilation,
                    func,
                    _projectContext,
                    _loader,
                    _logger);

                var carType = reflectedTypesProvider.GetReflectedType("Library1.Models.Car", true);
                Assert.Null(carType);

                var compilationErrors = reflectedTypesProvider.GetCompilationErrors();
                Assert.NotNull(compilationErrors);
            }
        }

        private void SetupProjectInformation(TemporaryFileProvider fileProvider)
        {
            _projectSetupHelper.SetupProjects(fileProvider, _output);

            var path = Path.Combine(fileProvider.Root, "Root", "Test.csproj");
            _projectContext = GetProjectInformation(path);
            _workspace = new RoslynWorkspace(_projectContext);
            _loader = new TestAssemblyLoadContext(_projectContext);
            _logger = new ConsoleLogger();
        }

        private IProjectContext GetProjectInformation(string path)
        {
            var rootContext = new MsBuildProjectContextBuilder(path, "Dummy")
                .Build();
            return rootContext;
        }
    }
}
