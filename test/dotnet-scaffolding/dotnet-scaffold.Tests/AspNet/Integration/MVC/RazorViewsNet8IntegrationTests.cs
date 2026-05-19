// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
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
    public async Task Scaffold_Views_Net8_CliInvocation()
    {
        // Arrange — set up project with Program.cs and a model class
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());
        var modelsDir = Path.Combine(_testProjectDir, "Models");
        Directory.CreateDirectory(modelsDir);
        File.WriteAllText(Path.Combine(modelsDir, "TestModel.cs"), ScaffoldCliHelper.GetModelClassContent("TestProject", "TestModel"));

        // Assert - project builds before scaffolding
        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        // Act - invoke CLI: dotnet scaffold aspnet views
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "views",
            "--project", _testProjectPath,
            "--model", "TestModel",
            "--page", "CRUD");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — expected files were created
        var viewsDir = Path.Combine(_testProjectDir, "Views", "TestModel");
        Assert.True(Directory.Exists(viewsDir),
            $"Views/TestModel directory should be created.\nProject dir: {_testProjectDir}\nCLI Output: {cliOutput}\nCLI Error: {cliError}\nFiles in project dir: {string.Join(", ", Directory.GetFileSystemEntries(_testProjectDir, "*", SearchOption.AllDirectories))}");
        foreach (var view in new[] { "Create.cshtml", "Delete.cshtml", "Details.cshtml", "Edit.cshtml", "Index.cshtml" })
        {
            Assert.True(File.Exists(Path.Combine(viewsDir, view)), $"View '{view}' should be created.");
        }
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Views", "Shared", "_ValidationScriptsPartial.cshtml")),
            "_ValidationScriptsPartial.cshtml should be created.");

        // Assert — no NuGet errors and project builds after scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
