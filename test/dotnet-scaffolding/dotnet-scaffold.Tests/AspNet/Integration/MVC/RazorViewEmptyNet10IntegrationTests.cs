// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// .NET 10-specific integration tests for the Razor View Empty (razorview-empty) scaffolder.
/// Inherits shared tests from <see cref="RazorViewEmptyIntegrationTestsBase"/>.
/// </summary>
public class RazorViewEmptyNet10IntegrationTests : RazorViewEmptyIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(RazorViewEmptyNet10IntegrationTests);

    [Fact]
    public async Task Scaffold_RazorViewEmpty_Net10_CliInvocation()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "razorview-empty",
            "--project", _testProjectPath,
            "--name", "TestView");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

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

        // Assert — no NuGet errors and project builds after scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
