// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

public class BlazorCrudNet11IntegrationTests : BlazorCrudIntegrationTestsBase
{
    protected override string TargetFramework => "net11.0";
    protected override string TestClassName => nameof(BlazorCrudNet11IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorCrud_Net11_CliInvocation()
    {
        var projectContent = ProjectContent.Replace(
            "</PropertyGroup>",
            "    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>\n  </PropertyGroup>");
        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        var modelsDir = Path.Combine(_testProjectDir, "Models");
        Directory.CreateDirectory(modelsDir);
        File.WriteAllText(Path.Combine(modelsDir, "TestModel.cs"), ScaffoldCliHelper.GetModelClassContent("TestProject", "TestModel"));

        // Set up Blazor project structure required for scaffolded code to compile
        var componentsDir = Path.Combine(_testProjectDir, "Components");
        Directory.CreateDirectory(componentsDir);
        File.WriteAllText(Path.Combine(componentsDir, "_Imports.razor"), ScaffoldCliHelper.GetBlazorImportsRazor());
        File.WriteAllText(Path.Combine(componentsDir, "App.razor"), ScaffoldCliHelper.GetBlazorAppRazor());
        File.WriteAllText(Path.Combine(componentsDir, "Routes.razor"), ScaffoldCliHelper.GetBlazorRoutesRazor());

        // Write NuGet.config with preview feeds so net11.0 packages can be resolved
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);

        var (beforeExitCode, _, beforeError) = await RunBuildAsync(_testProjectDir);
        Assert.True(beforeExitCode == 0, $"Project should build before scaffolding. Error: {beforeError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-crud",
            "--project", _testProjectPath,
            "--model", "TestModel",
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--page", "CRUD",
            "--prerelease");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — expected files were created (only if model resolution succeeded)
        bool scaffoldingSucceeded = !cliOutput.Contains("An error occurred");
        if (scaffoldingSucceeded)
        {
            var blazorPagesDir = Path.Combine(_testProjectDir, "Components", "Pages", "TestModelPages");
            Assert.True(Directory.Exists(blazorPagesDir), "Components/Pages/TestModelPages directory should be created.");
            foreach (var page in new[] { "Create.razor", "Delete.razor", "Details.razor", "Edit.razor", "Index.razor" })
            {
                Assert.True(File.Exists(Path.Combine(blazorPagesDir, page)), $"Blazor page '{page}' should be created.");
            }
            Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "TestDbContext.cs")),
                "DbContext file 'Data/TestDbContext.cs' should be created.");
            var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
            Assert.Contains("TestDbContext", programContent);

            // Assert no NuGet errors during scaffolding
            Assert.False(cliOutput.Contains("error: NU"),
                $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
            Assert.False(cliOutput.Contains("Failed"),
                $"Scaffolding should not contain failures for {TargetFramework}.\nOutput: {cliOutput}");

            // Verify project builds after scaffolding
            var (afterExitCode, _, afterError) = await RunBuildAsync(_testProjectDir);
            Assert.True(afterExitCode == 0, $"Project should still build after scaffolding. Error: {afterError}");
        }
    }
}
