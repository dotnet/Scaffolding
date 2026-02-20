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
/// .NET 9-specific integration tests for the MVC Empty Controller scaffolder.
/// Inherits shared tests from <see cref="ControllerIntegrationTestsBase"/>.
/// </summary>
public class ControllerNet9IntegrationTests : ControllerIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(ControllerNet9IntegrationTests);

    [Fact]
    public async Task ExecuteAsync_ScaffoldsCorrectFilesAndBuilds_Net9()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var realFileSystem = new FileSystem();
        var step = new EmptyControllerScaffolderStep(
            NullLogger<EmptyControllerScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "TestController",
            CommandName = "mvccontroller"
        };

        bool scaffoldResult = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(scaffoldResult, "Scaffolding should succeed.");

        string controllersDir = Path.Combine(_testProjectDir, "Controllers");
        Assert.True(Directory.Exists(controllersDir), "Controllers directory should be created.");

        string expectedFile = Path.Combine(controllersDir, "TestController.cs");
        Assert.True(File.Exists(expectedFile), $"Expected file '{expectedFile}' was not created.");

        string content = File.ReadAllText(expectedFile);
        Assert.False(string.IsNullOrWhiteSpace(content), "Generated controller file should not be empty.");
        Assert.Contains("Controller", content);

        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
