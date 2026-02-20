// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

/// <summary>
/// .NET 8-specific integration tests for the Razor Views scaffolder.
/// Inherits shared tests from <see cref="RazorViewsIntegrationTestsBase"/>.
/// </summary>
public class RazorViewsNet8IntegrationTests : RazorViewsIntegrationTestsBase
{
    protected override string TargetFramework => "net8.0";
    protected override string TestClassName => nameof(RazorViewsNet8IntegrationTests);

    [Fact]
    public async Task ValidateViewsStep_ValidatesAndBuilds_Net8()
    {
        // Arrange - write a real .NET 8 project
        File.WriteAllText(_testProjectPath, ProjectContent);

        // Assert - project builds before scaffolding
        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        // Act - run validate step with a real file system
        var realFileSystem = new FileSystem();
        var step = new ValidateViewsStep(
            realFileSystem,
            NullLogger<ValidateViewsStep>.Instance,
            _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD"
        };

        // ValidateViewsStep returns false when model class cannot be found,
        // but it should still exercise the validation pipeline without error.
        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result, "Validation step should fail because model class does not exist in the project.");

        // Assert - project still builds after the validation step (nothing was modified)
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after validation step.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
