// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// .NET 9-specific integration tests for the Razor View Empty (razorview-empty) scaffolder.
/// Inherits shared tests from <see cref="RazorViewEmptyIntegrationTestsBase"/>.
/// </summary>
public class RazorViewEmptyNet9IntegrationTests : RazorViewEmptyIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(RazorViewEmptyNet9IntegrationTests);

    [Fact]
    public async Task ExecuteAsync_ScaffoldsCorrectFilesAndBuilds_Net9()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "TestView",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool scaffoldResult = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(scaffoldResult, "Scaffolding should succeed.");

        string viewsDir = Path.Combine(_testProjectDir, "Views");
        Assert.True(Directory.Exists(viewsDir), "Views directory should be created.");

        string expectedFile = Path.Combine(viewsDir, "TestView.cshtml");
        Assert.True(File.Exists(expectedFile), $"Expected file '{expectedFile}' was not created.");

        string content = File.ReadAllText(expectedFile);
        Assert.False(string.IsNullOrWhiteSpace(content), "Generated .cshtml file should not be empty.");

        string[] files = Directory.GetFiles(viewsDir);
        Assert.All(files, f => Assert.EndsWith(".cshtml", f));
        Assert.Single(files);
        Assert.Empty(Directory.GetFiles(viewsDir, "*.razor"));

        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Components")), "Components directory should not exist.");
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Pages")), "Pages directory should not exist.");

        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
