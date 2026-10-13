// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

public class RazorViewsNet11IntegrationTests : RazorViewsIntegrationTestsBase
{
    protected override string TargetFramework => "net11.0";
    protected override string TestClassName => nameof(RazorViewsNet11IntegrationTests);

    [Fact]
    public async Task Scaffold_Views_Net11_CliInvocation()
    {
        var projectContent = ProjectContent.Replace(
            "</PropertyGroup>",
            "    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>\n  </PropertyGroup>");
        File.WriteAllText(_testProjectPath, projectContent);

        // Write NuGet.config with preview feeds so net11.0 packages can be resolved
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());
        var modelsDir = Path.Combine(_testProjectDir, "Models");
        Directory.CreateDirectory(modelsDir);
        File.WriteAllText(Path.Combine(modelsDir, "TestModel.cs"), ScaffoldCliHelper.GetModelClassContent("TestProject", "TestModel"));

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "views",
            "--project", _testProjectPath,
            "--model", "TestModel",
            "--page", "CRUD");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — expected files were created
        var viewsDir = Path.Combine(_testProjectDir, "Views", "TestModel");
        Assert.True(Directory.Exists(viewsDir), "Views/TestModel directory should be created.");
        foreach (var view in new[] { "Create.cshtml", "Delete.cshtml", "Details.cshtml", "Edit.cshtml", "Index.cshtml" })
        {
            Assert.True(File.Exists(Path.Combine(viewsDir, view)), $"View '{view}' should be created.");
        }
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Views", "Shared", "_ValidationScriptsPartial.cshtml")),
            "_ValidationScriptsPartial.cshtml should be created.");

        // Assert no NuGet errors during scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");

        // Assert project builds after scaffolding
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
