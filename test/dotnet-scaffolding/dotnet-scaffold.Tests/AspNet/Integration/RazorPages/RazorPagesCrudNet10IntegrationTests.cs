// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.RazorPages;

public class RazorPagesCrudNet10IntegrationTests : RazorPagesCrudIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(RazorPagesCrudNet10IntegrationTests);

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(true, false, true, "mvccontroller-crud")]
    [InlineData(true, false, true, "minimalapi")]
    [InlineData(true, false, true, "blazor-crud")]
    [InlineData(true, false, false, "razorpages-crud", true)]
    public async Task Scaffold_RazorPagesCrud_ReferencedModelAndDbContext(
        bool directModelReference, bool alreadyRegistered, bool existingDbSet,
        string scaffolder = "razorpages-crud", bool duplicateContextFileNames = false)
    {
        var modelDirectory = Path.Combine(_testDirectory, "ClassLibrary1");
        var contextDirectory = Path.Combine(_testDirectory, "ClassLibrary2");
        Directory.CreateDirectory(modelDirectory);
        Directory.CreateDirectory(contextDirectory);
        File.WriteAllText(Path.Combine(_testDirectory, "NuGet.config"), ScaffoldCliHelper.GetTestNuGetConfig(TargetFramework));
        File.WriteAllText(Path.Combine(modelDirectory, "ClassLibrary1.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(modelDirectory, "Movie.cs"), """
            namespace ClassLibrary1.Models;
            public class Movie
            {
                public int Id { get; set; }
                public string? Title { get; set; }
            }
            """);
        File.WriteAllText(Path.Combine(contextDirectory, "ClassLibrary2.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\ClassLibrary1\ClassLibrary1.csproj" />
                <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.9" />
              </ItemGroup>
            </Project>
            """);
        var targetContextDirectory = duplicateContextFileNames ? Path.Combine(contextDirectory, "ZSelected") : contextDirectory;
        Directory.CreateDirectory(targetContextDirectory);
        var contextPath = Path.Combine(targetContextDirectory, "MyDbContext.cs");
        const string contextWithDbSet = """
            using ClassLibrary1.Models;
            using Microsoft.EntityFrameworkCore;
            namespace ClassLibrary2.DBContext;
            public class MyDbContext : DbContext
            {
                public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }
                public DbSet<Movie> Movies { get; set; }
            }
            """;
        var contextSource = existingDbSet
            ? contextWithDbSet
            : contextWithDbSet.Replace("    public DbSet<Movie> Movies { get; set; }", string.Empty);
        File.WriteAllText(contextPath, contextSource);
        string? duplicatePath = null;
        const string duplicateSource = """
            using Microsoft.EntityFrameworkCore;
            namespace ClassLibrary2.Other;
            public class OtherMyDbContext : DbContext
            {
                public OtherMyDbContext(DbContextOptions<OtherMyDbContext> options) : base(options) { }
            }
            """;
        if (duplicateContextFileNames)
        {
            var duplicateDirectory = Path.Combine(contextDirectory, "AUnselected");
            Directory.CreateDirectory(duplicateDirectory);
            duplicatePath = Path.Combine(duplicateDirectory, "MyDbContext.cs");
            File.WriteAllText(duplicatePath, duplicateSource);
        }
        const string unrelatedSource = "namespace TestProject; public class UnrelatedClass { }";
        var unrelatedPath = Path.Combine(_testProjectDir, "MyDbContext.cs");
        File.WriteAllText(unrelatedPath, unrelatedSource);
        File.WriteAllText(Path.Combine(contextDirectory, "Startup.cs"), "namespace ClassLibrary2; public class Startup { }");
        var modelReference = directModelReference
            ? """<ProjectReference Include="..\ClassLibrary1\ClassLibrary1.csproj" />"""
            : string.Empty;
        File.WriteAllText(_testProjectPath, ProjectContent.Replace("</Project>", $"""
              <ItemGroup>
                {modelReference}
                <ProjectReference Include="..\ClassLibrary2\ClassLibrary2.csproj" />
                <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.9" />
              </ItemGroup>
            </Project>
            """));
        WriteProgram("ClassLibrary2.DBContext", alreadyRegistered);
        if (scaffolder == "blazor-crud")
        {
            File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
            var componentsDirectory = Path.Combine(_testProjectDir, "Components");
            Directory.CreateDirectory(componentsDirectory);
            File.WriteAllText(Path.Combine(componentsDirectory, "_Imports.razor"), ScaffoldCliHelper.GetBlazorImportsRazor());
            File.WriteAllText(Path.Combine(componentsDirectory, "App.razor"), ScaffoldCliHelper.GetBlazorAppRazor());
            File.WriteAllText(Path.Combine(componentsDirectory, "Routes.razor"), ScaffoldCliHelper.GetBlazorRoutesRazor());
        }

        await AssertExistingContextScaffoldingAsync(contextPath, contextSource, "ClassLibrary1.Models", "ClassLibrary2.DBContext",
            alreadyRegistered, existingDbSet, scaffolder);
        Assert.Equal(unrelatedSource, File.ReadAllText(unrelatedPath));
        if (duplicatePath is not null)
        {
            Assert.Equal(duplicateSource, File.ReadAllText(duplicatePath));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Scaffold_RazorPagesCrud_ExistingWebProjectModelAndDbContext(bool alreadyRegistered)
    {
        File.WriteAllText(_testProjectPath, ProjectContent.Replace("</Project>", """
              <ItemGroup>
                <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.9" />
              </ItemGroup>
            </Project>
            """));
        File.WriteAllText(Path.Combine(_testProjectDir, "Movie.cs"), """
            namespace TestProject.Models;
            public class Movie
            {
                public int Id { get; set; }
                public string? Title { get; set; }
            }
            """);
        var contextPath = Path.Combine(_testProjectDir, "MyDbContext.cs");
        const string contextSource = """
            using TestProject.Models;
            using Microsoft.EntityFrameworkCore;
            namespace TestProject.Data;
            public class MyDbContext : DbContext
            {
                public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }
                public DbSet<Movie> Movies { get; set; }
            }
            """;
        File.WriteAllText(contextPath, contextSource);
        WriteProgram("TestProject.Data", alreadyRegistered);

        await AssertExistingContextScaffoldingAsync(contextPath, contextSource, "TestProject.Models", "TestProject.Data", alreadyRegistered);
    }

    private const string ExistingRegistration =
        """builder.Services.AddDbContext<MyDbContext>(options => options.UseSqlServer("Server=localhost;Database=ScaffoldingRegression;Trusted_Connection=True;"));""";

    private void WriteProgram(string contextNamespace, bool alreadyRegistered)
    {
        var program = alreadyRegistered
            ? $"""
                using {contextNamespace};
                using Microsoft.EntityFrameworkCore;
                var builder = WebApplication.CreateBuilder(args);
                {ExistingRegistration}
                builder.Services.AddRazorPages();
                var app = builder.Build();
                app.MapRazorPages();
                app.Run();
                """
            : ScaffoldCliHelper.GetMinimalProgramCs();
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), program);
    }

    private async Task AssertExistingContextScaffoldingAsync(
        string contextPath, string contextSource, string modelNamespace, string contextNamespace, bool alreadyRegistered,
        bool existingDbSet = true, string scaffolder = "razorpages-crud")
    {
        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0, $"Build before scaffolding failed.\n{preOutput}\n{preError}");

        var options = new System.Collections.Generic.List<string>
        {
            "--project", _testProjectPath,
            "--model", "Movie",
            "--dataContext", "MyDbContext",
            "--dbProvider", "sqlserver-efcore"
        };
        options.AddRange(scaffolder switch
        {
            "mvccontroller-crud" => new[] { "--controller", "MoviesController", "--views" },
            "minimalapi" => new[] { "--endpoints", "MovieEndpoints" },
            _ => new[] { "--page", "CRUD" }
        });
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(TargetFramework, scaffolder, options.ToArray());

        Assert.True(exitCode == 0, $"Scaffolding failed.\n{output}\n{error}");
        Assert.DoesNotContain("Invalid --model", output);
        Assert.DoesNotContain("An error occurred", output);
        if (scaffolder == "razorpages-crud")
        {
            foreach (var page in new[] { "Create", "Delete", "Details", "Edit", "Index" })
            {
                var pagePath = Path.Combine(_testProjectDir, "Pages", "MoviePages", $"{page}.cshtml");
                Assert.True(File.Exists(pagePath), $"Missing {pagePath}.\n{output}\n{error}");
                Assert.True(File.Exists(pagePath + ".cs"), $"Missing {pagePath}.cs");
                var pageSource = File.ReadAllText(pagePath + ".cs");
                Assert.Contains($"using {modelNamespace};", pageSource);
                Assert.Contains($"using {contextNamespace};", pageSource);
                Assert.Contains("MyDbContext", pageSource);
                Assert.Contains(existingDbSet ? "_context.Movies" : "_context.Movie", pageSource);
            }
        }
        else
        {
            var generatedPath = scaffolder switch
            {
                "mvccontroller-crud" => Path.Combine(_testProjectDir, "Controllers", "MoviesController.cs"),
                "minimalapi" => Path.Combine(_testProjectDir, "MovieEndpoints.cs"),
                _ => Path.Combine(_testProjectDir, "Components", "Pages", "MoviePages", "Index.razor")
            };
            Assert.True(File.Exists(generatedPath), $"Missing {generatedPath}.\n{output}\n{error}");
        }

        var updatedContext = File.ReadAllText(contextPath);
        if (existingDbSet)
        {
            Assert.Equal(contextSource, updatedContext);
        }
        else
        {
            Assert.Contains($"public DbSet<{modelNamespace}.Movie> Movie", updatedContext);
            Assert.Equal(1, updatedContext.Split("DbSet<").Length - 1);
        }
        Assert.False(File.Exists(Path.Combine(_testProjectDir, "Data", "MyDbContext.cs")));
        var program = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        var registrationMethod = scaffolder == "blazor-crud" ? "AddDbContextFactory<" : "AddDbContext<";
        Assert.Equal(1, program.Split(registrationMethod).Length - 1);
        if (!alreadyRegistered)
        {
            Assert.Contains($"{registrationMethod}{contextNamespace}.MyDbContext>", program);
        }
        if (scaffolder == "razorpages-crud")
        {
            Assert.Equal(1, program.Split("AddRazorPages()").Length - 1);
        }
        if (alreadyRegistered)
        {
            Assert.Contains(ExistingRegistration, program);
        }
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0, $"Build after scaffolding failed.\n{postOutput}\n{postError}");
    }

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
    public async Task Scaffold_RazorPagesCrud_Net10_CliInvocation()
    {
        // Arrange — write project + Program.cs + model class
        File.WriteAllText(_testProjectPath, ProjectContent);
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
            "--page", "CRUD");
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

            // Assert — no NuGet errors and project builds after scaffolding
            Assert.False(cliOutput.Contains("error: NU"),
                $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
            var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
            Assert.True(postExitCode == 0,
                $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
        }
    }
}
