// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

public class CrudControllerNet10IntegrationTests : CrudControllerIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(CrudControllerNet10IntegrationTests);

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

        Assert.DoesNotContain("An error occurred", cliOutput);
        Assert.DoesNotContain("Failed", cliOutput);
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Controllers", "TestController.cs")),
            "Controller file 'Controllers/TestController.cs' should be created.");
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "TestDbContext.cs")),
            "DbContext file 'Data/TestDbContext.cs' should be created.");
        var viewsDir = Path.Combine(_testProjectDir, "Views", "Test");
        Assert.True(Directory.Exists(viewsDir), "Views/Test directory should be created.");
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Views", "TestModel")));
        foreach (var view in new[] { "Create.cshtml", "Delete.cshtml", "Details.cshtml", "Edit.cshtml", "Index.cshtml" })
        {
            Assert.True(File.Exists(Path.Combine(viewsDir, view)), $"View '{view}' should be created.");
        }
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Views", "Shared", "_ValidationScriptsPartial.cshtml")),
            "_ValidationScriptsPartial.cshtml should be created.");
        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("TestDbContext", programContent);

        Assert.DoesNotContain("error: NU", cliOutput);
        var (afterExitCode, afterOutput, afterError) = await RunBuildAsync(_testProjectDir);
        Assert.True(afterExitCode == 0, $"Project should still build after scaffolding.\nOutput: {afterOutput}\nError: {afterError}");
    }

    [Fact]
    public async Task Scaffold_MovieWithPluralController_RendersControllerViews()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.StableNuGetConfig);
        var appSettingsPath = Path.Combine(_testProjectDir, "appsettings.json");
        File.WriteAllText(appSettingsPath, "{}");
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        File.WriteAllText(programPath, """
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllersWithViews();
            var app = builder.Build();
            app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            app.Run();
            """);
        var modelsDir = Path.Combine(_testProjectDir, "Models");
        Directory.CreateDirectory(modelsDir);
        File.WriteAllText(Path.Combine(modelsDir, "Movie.cs"), """
            namespace TestProject.Models;

            public class Movie
            {
                public int ID { get; set; }
                public string Title { get; set; } = string.Empty;
                public DateTime ReleaseDate { get; set; }
                public string Genre { get; set; } = string.Empty;
                public decimal Price { get; set; }
            }
            """);
        var viewsDir = Path.Combine(_testProjectDir, "Views");
        Directory.CreateDirectory(viewsDir);
        File.WriteAllText(Path.Combine(viewsDir, "_ViewImports.cshtml"),
            "@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers");

        var (beforeExitCode, beforeOutput, beforeError) = await RunBuildAsync(_testProjectDir);
        Assert.True(beforeExitCode == 0, $"Project should build before scaffolding.\nOutput: {beforeOutput}\nError: {beforeError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "mvccontroller-crud",
            "--project", _testProjectPath,
            "--model", "Movie",
            "--controller", "MoviesController",
            "--dataContext", "ApplicationDbContext",
            "--dbProvider", "sqlite-efcore",
            "--views");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");
        Assert.DoesNotContain("An error occurred", cliOutput);
        Assert.DoesNotContain("Failed", cliOutput);
        using var settings = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
        Assert.False(string.IsNullOrEmpty(settings.RootElement.GetProperty("ConnectionStrings")
            .GetProperty("ApplicationDbContext").GetString()));
        var controllerPath = Path.Combine(_testProjectDir, "Controllers", "MoviesController.cs");
        Assert.True(File.Exists(controllerPath));
        Assert.Contains("public class MoviesController : Controller", File.ReadAllText(controllerPath));
        var movieViewsDir = Path.Combine(viewsDir, "Movies");
        Assert.True(Directory.Exists(movieViewsDir), "Views/Movies directory should be created.");
        Assert.False(Directory.Exists(Path.Combine(viewsDir, "Movie")));
        Assert.Equal(5, Directory.GetFiles(movieViewsDir, "*.cshtml").Length);
        foreach (var view in new[] { "Create", "Delete", "Details", "Edit", "Index" })
        {
            var viewPath = Path.Combine(movieViewsDir, $"{view}.cshtml");
            Assert.True(File.Exists(viewPath), $"View '{view}' should be created.");
            Assert.Contains("TestProject.Models.Movie", File.ReadAllText(viewPath));
        }

        var (afterExitCode, afterOutput, afterError) = await RunBuildAsync(_testProjectDir);
        Assert.True(afterExitCode == 0, $"Scaffolded project should build.\nOutput: {afterOutput}\nError: {afterError}");

        var programContent = File.ReadAllText(programPath);
        Assert.Contains("app.Run();", programContent);
        // Run real MVC requests using a temporary SQLite database and an OS-assigned port.
        File.WriteAllText(programPath, programContent.Replace("app.Run();", """
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync();
            }
            app.Urls.Add("http://127.0.0.1:0");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await app.StartAsync(timeout.Token);
            try
            {
                var server = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
                var addresses = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()
                    ?? throw new InvalidOperationException("The server did not publish its address.");
                using var handler = new HttpClientHandler { UseProxy = false };
                using var client = new HttpClient(handler) { BaseAddress = new Uri(addresses.Addresses.Single()) };
                foreach (var action in new[] { "Index", "Create" })
                {
                    var path = action == "Index" ? "/Movies" : "/Movies/Create";
                    using var response = await client.GetAsync(path, timeout.Token);
                    var body = await response.Content.ReadAsStringAsync(timeout.Token);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK || !body.Contains($"<h1>{action}</h1>"))
                    {
                        throw new InvalidOperationException($"GET {path} returned {response.StatusCode}: {body}");
                    }
                    Console.WriteLine($"MVC_ACCEPTANCE {path} 200");
                }
                using var missing = await client.GetAsync("/Movie", timeout.Token);
                if (missing.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    throw new InvalidOperationException($"GET /Movie returned {missing.StatusCode} instead of 404.");
                }
            }
            finally
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
            """));
        var (runtimeExitCode, runtimeOutput, runtimeError) = await ScaffoldCliHelper.RunDotNetAsync(
            _testProjectDir, "run", "--no-launch-profile");
        Assert.True(runtimeExitCode == 0, $"Scaffolded MVC views should render.\nOutput: {runtimeOutput}\nError: {runtimeError}");
        Assert.Contains("MVC_ACCEPTANCE /Movies 200", runtimeOutput);
        Assert.Contains("MVC_ACCEPTANCE /Movies/Create 200", runtimeOutput);
        Assert.DoesNotContain("was not found", runtimeOutput);
    }
}
