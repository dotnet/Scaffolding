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
/// .NET 11-specific integration tests for the Razor Component (blazor-empty) scaffolder.
/// Inherits shared tests from <see cref="RazorComponentIntegrationTestsBase"/>.
/// </summary>
public class RazorComponentNet11IntegrationTests : RazorComponentIntegrationTestsBase
{
    protected override string TargetFramework => "net11.0";
    protected override string TestClassName => nameof(RazorComponentNet11IntegrationTests);

    [Fact]
    public async Task ExecuteAsync_ScaffoldsCorrectFilesAndBuilds_Net11()
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
            FileName = "TestComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        bool scaffoldResult = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(scaffoldResult, "Scaffolding should succeed.");

        string componentsDir = Path.Combine(_testProjectDir, "Components");
        Assert.True(Directory.Exists(componentsDir), "Components directory should be created.");

        string expectedFile = Path.Combine(componentsDir, "TestComponent.razor");
        Assert.True(File.Exists(expectedFile), $"Expected file '{expectedFile}' was not created.");

        string content = File.ReadAllText(expectedFile);
        Assert.False(string.IsNullOrWhiteSpace(content), "Generated .razor file should not be empty.");
        Assert.Contains("<h3>", content);
        Assert.Contains("@code", content);
        Assert.DoesNotContain("@page", content);

        string[] files = Directory.GetFiles(componentsDir);
        Assert.All(files, f => Assert.EndsWith(".razor", f));

        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Pages")), "Pages directory should not exist.");
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Views")), "Views directory should not exist.");

        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
