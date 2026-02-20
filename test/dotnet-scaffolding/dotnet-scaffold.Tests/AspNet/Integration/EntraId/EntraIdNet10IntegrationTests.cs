// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.EntraId;

public class EntraIdNet10IntegrationTests : EntraIdIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(EntraIdNet10IntegrationTests);

    [Fact]
    public async Task Scaffold_EntraId_Net10_BuildsAndValidates()
    {
        // Arrange
        File.WriteAllText(_testProjectPath, ProjectContent);

        // Verify project builds before scaffolding
        var (beforeExitCode, _, beforeError) = await RunBuildAsync(_testProjectDir);
        Assert.True(beforeExitCode == 0, $"Project should build before scaffolding. Error: {beforeError}");

        // Scaffold  use ValidateEntraIdStep with real FileSystem (returns false since Entra ID not configured)
        var realFileSystem = new FileSystem();
        var step = new ValidateEntraIdStep(
            realFileSystem,
            NullLogger<ValidateEntraIdStep>.Instance,
            _testTelemetryService);

        step.Project = _testProjectPath;
        step.Username = "test@example.com";
        step.TenantId = "test-tenant-id";

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);

        // Verify project still builds after scaffolding attempt
        var (afterExitCode, _, afterError) = await RunBuildAsync(_testProjectDir);
        Assert.True(afterExitCode == 0, $"Project should still build after scaffolding. Error: {afterError}");
    }
}
