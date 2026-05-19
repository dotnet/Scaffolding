// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// .NET 9-specific integration tests for the Razor Page Empty (razorpage-empty) scaffolder.
/// Inherits shared tests from <see cref="RazorPageEmptyIntegrationTestsBase"/>.
/// </summary>
public class RazorPageEmptyNet9IntegrationTests : RazorPageEmptyIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(RazorPageEmptyNet9IntegrationTests);

    [Fact]
    public async Task Scaffold_RazorPageEmpty_Net9_CliInvocation()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "razorpage-empty",
            "--project", _testProjectPath,
            "--name", "TestPage");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        Assert.True(Directory.Exists(pagesDir), "Pages directory should be created.");

        string expectedCshtml = Path.Combine(pagesDir, "TestPage.cshtml");
        string expectedCodeBehind = Path.Combine(pagesDir, "TestPage.cshtml.cs");
        Assert.True(File.Exists(expectedCshtml), $"Expected file '{expectedCshtml}' was not created.");
        Assert.True(File.Exists(expectedCodeBehind), $"Expected code-behind file '{expectedCodeBehind}' was not created.");

        string content = File.ReadAllText(expectedCshtml);
        Assert.False(string.IsNullOrWhiteSpace(content), "Generated .cshtml file should not be empty.");

        string codeBehindContent = File.ReadAllText(expectedCodeBehind);
        Assert.Contains("PageModel", codeBehindContent);

        Assert.Empty(Directory.GetFiles(pagesDir, "*.razor"));

        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Views")), "Views directory should not exist.");
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Components")), "Components directory should not exist.");

        // Assert — no NuGet errors and project builds after scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
