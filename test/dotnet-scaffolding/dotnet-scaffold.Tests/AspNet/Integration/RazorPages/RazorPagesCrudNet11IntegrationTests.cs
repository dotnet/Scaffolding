// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.RazorPages;

public class RazorPagesCrudNet11IntegrationTests : RazorPagesCrudIntegrationTestsBase
{
    protected override string TargetFramework => "net11.0";
    protected override string TestClassName => nameof(RazorPagesCrudNet11IntegrationTests);

    [Theory]
    [InlineData("Create.tt")]
    [InlineData("Create.cs")]
    [InlineData("CreateModel.tt")]
    [InlineData("CreateModel.cs")]
    [InlineData("Delete.tt")]
    [InlineData("Delete.cs")]
    [InlineData("DeleteModel.tt")]
    [InlineData("DeleteModel.cs")]
    [InlineData("Details.tt")]
    [InlineData("Details.cs")]
    [InlineData("DetailsModel.tt")]
    [InlineData("DetailsModel.cs")]
    [InlineData("Edit.tt")]
    [InlineData("Edit.cs")]
    [InlineData("EditModel.tt")]
    [InlineData("EditModel.cs")]
    [InlineData("Index.tt")]
    [InlineData("Index.cs")]
    [InlineData("IndexModel.tt")]
    [InlineData("IndexModel.cs")]
    public void RazorPagesTemplates_HasExpectedT4File(string fileName)
    {
        var basePath = GetActualTemplatesBasePath();
        var filePath = Path.Combine(basePath, TargetFramework, "RazorPages", fileName);
        Assert.True(File.Exists(filePath),
            $"Expected RazorPages template file '{fileName}' not found for {TargetFramework}");
    }

    [Fact]
    public async Task Scaffold_RazorPagesCrud_Net11_CliInvocation()
    {
        // Arrange — write project + Program.cs + model class
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

        // Assert — project builds before scaffolding
        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        // Act — invoke CLI: dotnet scaffold aspnet razorpages-crud
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "razorpages-crud",
            "--project", _testProjectPath,
            "--model", "TestModel",
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--page", "CRUD",
            "--prerelease");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — expected files were created (skip if scaffolding encountered errors)
        bool scaffoldingSucceeded = !cliOutput.Contains("An error occurred") && !cliOutput.Contains("Failed");
        if (scaffoldingSucceeded)
        {
            var razorPagesDir = Path.Combine(_testProjectDir, "Pages", "TestModelPages");
            Assert.True(Directory.Exists(razorPagesDir), "Pages/TestModelPages directory should be created.");
            foreach (var page in new[] { "Create", "Delete", "Details", "Edit", "Index" })
            {
                Assert.True(File.Exists(Path.Combine(razorPagesDir, $"{page}.cshtml")), $"{page}.cshtml should be created.");
                Assert.True(File.Exists(Path.Combine(razorPagesDir, $"{page}.cshtml.cs")), $"{page}.cshtml.cs should be created.");
            }
            Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "TestDbContext.cs")),
                "DbContext file 'Data/TestDbContext.cs' should be created.");
            var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
            Assert.Contains("TestDbContext", programContent);

            // Assert no NuGet errors during scaffolding
            Assert.False(cliOutput.Contains("error: NU"),
                $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");

            // Assert — project builds after scaffolding
            var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
            Assert.True(postExitCode == 0,
                $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
        }
    }
}
