// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-crud")]
public class BlazorCrudFactoryFrameworkTests
{
    [Theory]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public async Task Scaffold_RegistersFactoryAndRendersMovies_OnTargetFramework(string framework)
    {
        using var project = new BlazorTestProject(framework);
        ScaffoldCliHelper.SetupBlazorProjectStructure(project.ProjectDirectory);
        File.AppendAllText(Path.Combine(project.ProjectDirectory, "Components", "_Imports.razor"),
            "@using TestProject.Components.Layout\n");
        File.WriteAllText(Path.Combine(project.ProjectDirectory, "appsettings.json"), "{}");
        File.WriteAllText(Path.Combine(project.ProjectDirectory, "Movie.cs"), """
            namespace TestProject;
            public class Movie
            {
                public int Id { get; set; }
                public string Title { get; set; } = string.Empty;
            }
            """);
        var programPath = Path.Combine(project.ProjectDirectory, "Program.cs");
        File.WriteAllText(programPath, """
            using TestProject.Components;
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
            var app = builder.Build();
            app.UseAntiforgery();
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
            app.Run();
            """);
        var before = await ScaffoldCliHelper.RunBuildAsync(project.ProjectDirectory);
        Assert.True(before.ExitCode == 0, $"Input build failed.\n{before.Output}\n{before.Error}");
        var scaffold = await ScaffoldCliHelper.RunScaffoldAsync(
            ScaffoldCliHelper.GetTestTargetFramework(), "blazor-crud",
            ["--project", project.ProjectPath, "--model", "Movie", "--dataContext", "MovieContext",
                "--dbProvider", "sqlite-efcore", "--page", "CRUD",
                .. ScaffoldCliHelper.GetPrereleaseArguments(framework)]);
        Assert.True(scaffold.ExitCode == 0, $"Scaffolding failed.\n{scaffold.Output}\n{scaffold.Error}");
        File.WriteAllText(programPath, File.ReadAllText(programPath).Replace("app.Run();", """
            using var scope = app.Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MovieContext>>();
            await using var movies = factory.CreateDbContext();
            try
            {
                await movies.Database.EnsureCreatedAsync();
                movies.Movie.Add(new TestProject.Movie { Title = "Framework regression movie" });
                await movies.SaveChangesAsync();
                app.Urls.Add("http://127.0.0.1:0");
                await app.StartAsync();
                try
                {
                    using var client = new System.Net.Http.HttpClient
                    {
                        BaseAddress = new Uri(app.Urls.Single()),
                        Timeout = TimeSpan.FromSeconds(30)
                    };
                    var html = await client.GetStringAsync("/movies");
                    if (!html.Contains("Framework regression movie"))
                    {
                        throw new InvalidOperationException("The generated index did not query and render the movie.");
                    }
                    Console.WriteLine("Factory resolved and movies rendered.");
                }
                finally
                {
                    await app.StopAsync();
                }
            }
            finally
            {
                await movies.Database.EnsureDeletedAsync();
            }
            """));
        var build = await ScaffoldCliHelper.RunBuildAsync(project.ProjectDirectory);
        Assert.True(build.ExitCode == 0, $"Generated build failed.\n{build.Output}\n{build.Error}");
        var verify = await ScaffoldCliHelper.RunDotNetAsync(project.ProjectDirectory,
            "run", "--no-build", "--no-launch-profile");
        Assert.True(verify.ExitCode == 0, $"Page rendering failed.\n{verify.Output}\n{verify.Error}");
        Assert.Contains("Factory resolved and movies rendered.", verify.Output);
    }
}
