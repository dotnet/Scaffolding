// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

public class BlazorCrudNet8IntegrationTests : BlazorCrudIntegrationTestsBase
{
    protected override string TargetFramework => "net8.0";
    protected override string TestClassName => nameof(BlazorCrudNet8IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorCrud_Net8_BuildsAndValidates()
    {
        // Arrange
        File.WriteAllText(_testProjectPath, ProjectContent);

        // Verify project builds before scaffolding
        var (beforeExitCode, _, beforeError) = await RunBuildAsync(_testProjectDir);
        Assert.True(beforeExitCode == 0, $"Project should build before scaffolding. Error: {beforeError}");

        // Scaffold  use ValidateBlazorCrudStep with real FileSystem (returns false since model class doesn't exist)
        var realFileSystem = new FileSystem();
        var step = new ValidateBlazorCrudStep(
            realFileSystem,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService);

        step.Project = _testProjectPath;
        step.Model = "TestModel";
        step.Page = "CRUD";
        step.DataContext = "TestDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);

        // Verify project still builds after scaffolding attempt
        var (afterExitCode, _, afterError) = await RunBuildAsync(_testProjectDir);
        Assert.True(afterExitCode == 0, $"Project should still build after scaffolding. Error: {afterError}");
    }
}
