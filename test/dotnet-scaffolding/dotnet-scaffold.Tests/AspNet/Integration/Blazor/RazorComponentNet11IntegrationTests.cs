// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

/// <summary>
/// .NET 11-specific integration tests for the Razor Component (blazor-empty) scaffolder.
/// Inherits shared tests from <see cref="RazorComponentIntegrationTestsBase"/>.
/// </summary>
public class RazorComponentNet11IntegrationTests : RazorComponentIntegrationTestsBase
{
    protected override string TargetFramework => "net11.0";
    protected override string TestClassName => nameof(RazorComponentNet11IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorEmpty_Net11_CliInvocation()
    {
        var projectContent = ProjectContent.Replace(
            "</PropertyGroup>",
            "    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>\n  </PropertyGroup>");
        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());

        // Write NuGet.config with preview feeds so net11.0 packages can be resolved
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-empty",
            "--project", _testProjectPath,
            "--name", "TestComponent");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

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

        // Assert no NuGet errors during scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");

        // Assert project builds after scaffolding
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
