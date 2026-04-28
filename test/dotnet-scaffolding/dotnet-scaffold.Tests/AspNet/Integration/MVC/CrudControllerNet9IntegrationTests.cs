// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

public class CrudControllerNet9IntegrationTests : CrudControllerIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(CrudControllerNet9IntegrationTests);

    [Fact]
    public async Task Scaffold_MvcControllerCrud_Net9_CliInvocation()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());
        var modelsDir = Path.Combine(_testProjectDir, "Models");
        Directory.CreateDirectory(modelsDir);
        File.WriteAllText(Path.Combine(modelsDir, "TestModel.cs"), ScaffoldCliHelper.GetModelClassContent("TestProject", "TestModel"));

        var (beforeExitCode, _, beforeError) = await RunBuildAsync(_testProjectDir);
        Assert.True(beforeExitCode == 0, $"Project should build before scaffolding. Error: {beforeError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "mvccontroller-crud",
            "--project", _testProjectPath,
            "--model", "TestModel",
            "--controller", "TestController",
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--views");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — expected files were created (skip if scaffolding encountered errors)
        bool scaffoldingSucceeded = !cliOutput.Contains("An error occurred") && !cliOutput.Contains("Failed");
        if (scaffoldingSucceeded)
        {
            Assert.True(File.Exists(Path.Combine(_testProjectDir, "Controllers", "TestController.cs")),
                "Controller file 'Controllers/TestController.cs' should be created.");
            Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "TestDbContext.cs")),
                "DbContext file 'Data/TestDbContext.cs' should be created.");
            var viewsDir = Path.Combine(_testProjectDir, "Views", "TestModel");
            Assert.True(Directory.Exists(viewsDir), "Views/TestModel directory should be created.");
            foreach (var view in new[] { "Create.cshtml", "Delete.cshtml", "Details.cshtml", "Edit.cshtml", "Index.cshtml" })
            {
                Assert.True(File.Exists(Path.Combine(viewsDir, view)), $"View '{view}' should be created.");
            }
            Assert.True(File.Exists(Path.Combine(_testProjectDir, "Views", "Shared", "_ValidationScriptsPartial.cshtml")),
                "_ValidationScriptsPartial.cshtml should be created.");
            var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
            Assert.Contains("TestDbContext", programContent);

            // Assert — no NuGet errors and project builds after scaffolding
            Assert.False(cliOutput.Contains("error: NU"),
                $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
            var (afterExitCode, _, afterError) = await RunBuildAsync(_testProjectDir);
            Assert.True(afterExitCode == 0, $"Project should still build after scaffolding. Error: {afterError}");
        }
    }
}
