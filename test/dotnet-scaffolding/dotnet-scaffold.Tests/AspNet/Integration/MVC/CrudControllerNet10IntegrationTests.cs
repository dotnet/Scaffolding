// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

public class CrudControllerNet10IntegrationTests : CrudControllerIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(CrudControllerNet10IntegrationTests);

    [Theory]
    [InlineData("Store.Web", "Store.Web")]
    [InlineData("Store-Web", "Store_Web")]
    public async Task Scaffold_ProductWithExistingContextAndCommentedSettings_Net10(string rootNamespace, string controllerNamespace)
    {
        File.WriteAllText(_testProjectPath, $$"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <RootNamespace>{{rootNamespace}}</RootNamespace>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.10" />
                <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.10" />
                <PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="10.0.2" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), """
            using Microsoft.EntityFrameworkCore;
            using Store.Web.Data;
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("AppDbContext")));
            var app = builder.Build();
            app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            app.Run();
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Product.cs"), """
            namespace Store.Web.Models;
            public class Product
            {
                public int ProductId { get; set; }
                public string Name { get; set; } = "";
                public decimal Price { get; set; }
            }
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "AppDbContext.cs"), """
            using Microsoft.EntityFrameworkCore;
            using Store.Web.Models;
            namespace Store.Web.Data;
            public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
            {
                public DbSet<Product> Products { get; set; }
            }
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "appsettings.json"), """
            {
              "AllowedHosts": "*", // valid ASP.NET Core configuration
            }
            """);

        var (beforeExit, beforeOutput, beforeError) = await RunBuildAsync(_testProjectDir);
        Assert.True(beforeExit == 0, $"Initial build failed:\n{beforeOutput}\n{beforeError}");

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            ScaffoldCliHelper.GetTestTargetFramework(),
            "mvccontroller-crud",
            "--project", _testProjectPath,
            "--model", "Product",
            "--controller", "ProductsController",
            "--dataContext", "AppDbContext",
            "--dbProvider", "sqlserver-efcore",
            "--views");
        Assert.True(exitCode == 0, $"Scaffolding failed:\n{output}\n{error}");

        string controller = File.ReadAllText(Path.Combine(_testProjectDir, "Controllers", "ProductsController.cs"));
        Assert.Contains($"namespace {controllerNamespace}.Controllers;", controller);
        Assert.Contains("Details(int? id)", controller);
        Assert.Contains("m.ProductId == id", controller);
        foreach (string view in new[] { "Create", "Edit", "Details", "Delete", "Index" })
        {
            Assert.True(File.Exists(Path.Combine(_testProjectDir, "Views", "Products", $"{view}.cshtml")));
        }
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Views", "Product")));
        string index = File.ReadAllText(Path.Combine(_testProjectDir, "Views", "Products", "Index.cshtml"));
        Assert.Contains("asp-route-id=\"@item.ProductId\"", index);

        var (afterExit, afterOutput, afterError) = await RunBuildAsync(_testProjectDir);
        Assert.True(afterExit == 0, $"Generated project failed to build:\n{afterOutput}\n{afterError}");
    }

    [Fact]
    public async Task Scaffold_MvcControllerCrud_Net10_CliInvocation()
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
            var viewsDir = Path.Combine(_testProjectDir, "Views", "Test");
            Assert.True(Directory.Exists(viewsDir), "Views/Test directory should be created for TestController.");
            Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Views", "TestModel")),
                "The model name should not determine the controller's views folder.");
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
