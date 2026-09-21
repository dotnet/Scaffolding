// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

public class BlazorCrudNet9IntegrationTests : BlazorCrudIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(BlazorCrudNet9IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorCrud_Net9_CliInvocation()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
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

        var (beforeExitCode, _, beforeError) = await RunBuildAsync(_testProjectDir);
        Assert.True(beforeExitCode == 0, $"Project should build before scaffolding. Error: {beforeError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-crud",
            "--project", _testProjectPath,
            "--model", "TestModel",
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--page", "CRUD");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — expected files were created.
        var blazorPagesDir = Path.Combine(_testProjectDir, "Components", "Pages", "TestModelPages");
        Assert.True(Directory.Exists(blazorPagesDir),
            $"Components/Pages/TestModelPages directory should be created.\nOutput: {cliOutput}\nError: {cliError}");

        foreach (var page in new[] { "Create.razor", "Delete.razor", "Details.razor", "Edit.razor", "Index.razor" })
        {
            Assert.True(File.Exists(Path.Combine(blazorPagesDir, page)), $"Blazor page '{page}' should be created.");
        }

        var editContent = File.ReadAllText(Path.Combine(blazorPagesDir, "Edit.razor"));
        Assert.Contains("ApplicationState.TryTakeFromJson<TestModel>(nameof(TestModel), out var restoredTestModel)", editContent);
        Assert.Contains("TestModel = restoredTestModel", editContent);
        Assert.Contains("ApplicationState.PersistAsJson(nameof(TestModel), TestModel)", editContent);
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "TestDbContext.cs")),
            "DbContext file 'Data/TestDbContext.cs' should be created.");
        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("TestDbContext", programContent);

        // Post-scaffolding build verification
        var combinedOutput = cliOutput + cliError;
        Assert.False(combinedOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}\nError: {cliError}");
        var (afterExitCode, _, afterError) = await RunBuildAsync(_testProjectDir);
        Assert.True(afterExitCode == 0, $"Project should still build after scaffolding. Error: {afterError}");
    }
}
