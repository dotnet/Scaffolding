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

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

public class IdentityNet8IntegrationTests : IdentityIntegrationTestsBase
{
    protected override string TargetFramework => "net8.0";
    protected override string TestClassName => nameof(IdentityNet8IntegrationTests);

    [Fact]
    public async Task Scaffold_Identity_Net8_BuildsAndValidates()
    {
        // Arrange
        File.WriteAllText(_testProjectPath, ProjectContent);

        // Verify project builds before scaffolding
        var (beforeExitCode, _, beforeError) = await RunBuildAsync(_testProjectDir);
        Assert.True(beforeExitCode == 0, $"Project should build before scaffolding. Error: {beforeError}");

        // Scaffold  use ValidateIdentityStep with real FileSystem (returns false since project doesn't have identity setup)
        var realFileSystem = new FileSystem();
        var step = new ValidateIdentityStep(
            realFileSystem,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);

        step.Project = _testProjectPath;
        step.DataContext = "TestDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);

        // Verify project still builds after scaffolding attempt
        var (afterExitCode, _, afterError) = await RunBuildAsync(_testProjectDir);
        Assert.True(afterExitCode == 0, $"Project should still build after scaffolding. Error: {afterError}");
    }
}
