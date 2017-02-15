// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
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

        [Fact (Skip = "Disable tests on CI that need to load assemblies")]
        public void TestGetReflectedType()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                SetupProjectInformation(fileProvider);

                var compilation = _workspace.CurrentSolution.Projects
                    .First()
                    .GetCompilationAsync()
                    .Result;

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

        [Fact]
        public void TestGetReflectedType_FailedCompilation()
        {
            using (var fileProvider = new TemporaryFileProvider())
            {
                SetupProjectInformation(fileProvider);

                var compilation = _workspace.CurrentSolution.Projects
                    .First()
                    .GetCompilationAsync()
                    .Result;

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
