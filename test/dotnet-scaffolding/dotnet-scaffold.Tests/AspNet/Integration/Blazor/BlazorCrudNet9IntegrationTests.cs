// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

public class BlazorCrudNet9IntegrationTests : BlazorCrudIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(BlazorCrudNet9IntegrationTests);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Scaffold_BlazorCrud_Net9_ExistingIdentityContext_BuildsSuccessfully(bool useRelativeProjectPath)
    {
        File.WriteAllText(_testProjectPath, ProjectContent.Replace("</Project>", """
            <ItemGroup>
              <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.21" />
              <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.21" />
            </ItemGroup>
            </Project>
            """));
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"),
            "using TestProject.Data;\n" + ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);
        File.WriteAllText(Path.Combine(_testProjectDir, "Movie.cs"), """
            public class Movie
            {
                public int ID { get; set; }
                public string Title { get; set; } = string.Empty;
                public DateTime ReleaseDate { get; set; }
                public string Genre { get; set; } = string.Empty;
                public decimal Price { get; set; }
            }
            """);
        var dataDirectory = Path.Combine(_testProjectDir, "Data");
        Directory.CreateDirectory(dataDirectory);
        var contextPath = Path.Combine(dataDirectory, "ApplicationDbContext.cs");
        File.WriteAllText(contextPath, """
            using Microsoft.AspNetCore.Identity;
            using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore;

            namespace TestProject.Data;

            public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<IdentityUser>(options)
            {
            }
            """);

        var (beforeExitCode, beforeOutput, beforeError) = await RunBuildAsync(_testProjectDir);
        Assert.True(beforeExitCode == 0, $"Project should build before scaffolding.\n{beforeOutput}\n{beforeError}");

        var projectPath = useRelativeProjectPath
            ? Path.GetRelativePath(Directory.GetCurrentDirectory(), _testProjectPath)
            : _testProjectPath;
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "blazor-crud",
            "--project", projectPath,
            "--model", "Movie",
            "--dataContext", "ApplicationDbContext",
            "--dbProvider", "sqlserver-efcore",
            "--page", "CRUD");
        Assert.True(exitCode == 0, $"Scaffolding should succeed.\n{output}\n{error}");
        Assert.Contains("DbSet<Movie> Movie", File.ReadAllText(contextPath));
        var adapter = Assert.Single(XDocument.Load(_testProjectPath).Descendants("PackageReference"),
            reference => (string?)reference.Attribute("Include") == "Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter");
        Assert.StartsWith("9.", (string?)adapter.Attribute("Version"));

        var (afterExitCode, afterOutput, afterError) = await RunBuildAsync(_testProjectDir);
        Assert.True(afterExitCode == 0, $"Project should build after scaffolding.\n{afterOutput}\n{afterError}");
    }

    [Fact]
    public async Task Scaffold_BlazorCrud_Net9_CliInvocation()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        var modelsDir = Path.Combine(_testProjectDir, "Models");
        Directory.CreateDirectory(modelsDir);
        File.WriteAllText(Path.Combine(modelsDir, "TestModel.cs"), @"namespace TestProject.Models;

public class TestModel
{
    public int Id { get; set; }
    public EmploymentType EmploymentType { get; set; }
    public EmploymentType? OptionalEmploymentType { get; set; }
}

public enum EmploymentType
{
    Permanent,
    Contract
}
");

        // Set up Blazor project structure required for scaffolded code to compile
        var componentsDir = Path.Combine(_testProjectDir, "Components");
        Directory.CreateDirectory(componentsDir);
        File.WriteAllText(Path.Combine(componentsDir, "_Imports.razor"), ScaffoldCliHelper.GetBlazorImportsRazor());
        File.WriteAllText(Path.Combine(componentsDir, "App.razor"), ScaffoldCliHelper.GetBlazorAppRazor());
        File.WriteAllText(Path.Combine(componentsDir, "Routes.razor"), ScaffoldCliHelper.GetBlazorRoutesRazor());

        var (beforeExitCode, beforeOutput, beforeError) = await RunBuildAsync(_testProjectDir);
        Assert.True(beforeExitCode == 0, $"Project should build before scaffolding.\nOutput: {beforeOutput}\nError: {beforeError}");

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

        var createContent = File.ReadAllText(Path.Combine(blazorPagesDir, "Create.razor"));
        Assert.Contains("<InputSelect id=\"employmenttype\"", createContent);
        Assert.Contains("<InputSelect id=\"optionalemploymenttype\"", createContent);

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
        var (afterExitCode, afterOutput, afterError) = await RunBuildAsync(_testProjectDir);
        Assert.True(afterExitCode == 0, $"Project should still build after scaffolding.\nOutput: {afterOutput}\nError: {afterError}");
    }
}
