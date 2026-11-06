// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-crud")]
public class BlazorCrudInteractivityIntegrationTests
{
    [Theory]
    [InlineData("net9.0", "InteractiveAuto", true)]
    [InlineData("net9.0", "InteractiveAuto", false)]
    [InlineData("net9.0", "InteractiveWebAssembly", true)]
    [InlineData("net9.0", "InteractiveWebAssembly", false)]
    [InlineData("net10.0", "InteractiveAuto", true)]
    [InlineData("net10.0", "InteractiveAuto", false)]
    [InlineData("net10.0", "InteractiveWebAssembly", true)]
    [InlineData("net10.0", "InteractiveWebAssembly", false)]
    [InlineData("net10.0", "InteractiveServer", false)]
    [InlineData("net11.0", "InteractiveAuto", false)]
    [InlineData("net10.0", "InteractiveAuto", false, "@rendermode = \"@(InteractiveAuto)\"")]
    [InlineData("net10.0", "new InteractiveServerRenderMode(prerender: \"server\" == \"server\")", false,
        "@rendermode=\"@(new InteractiveServerRenderMode(prerender: \"server\" == \"server\"))\"")]
    [InlineData("net10.0", "InteractiveAuto", false, null, "@* <Routes /> <HeadOutlet /> *@")]
    [InlineData("net10.0", "InteractiveAuto", false, null, "<!-- <Routes /> <HeadOutlet /> -->")]
    public async Task Scaffold_BlazorCrud_GlobalInteractivityKeepsCrudRoutesOnServer(string framework, string renderMode, bool individualAccounts, string? routesRenderModeAttribute = null, string? commentedMarkup = null)
    {
        using var project = new BlazorTestProject(framework);
        project.AddWebAssemblyClient(framework == "net11.0" ? "11.0.*-*" : framework == "net9.0" ? "9.0.*" : "10.0.*",
            usesInteractiveServer: renderMode != "InteractiveWebAssembly");
        string serverDirectory = project.ProjectDirectory;
        string serverProject = project.ProjectPath;
        string componentsDirectory = Path.Combine(serverDirectory, "Components");
        Directory.Move(Path.Combine(componentsDirectory, "Layout"), Path.Combine(project.ClientDirectory, "Layout"));
        File.Delete(Path.Combine(componentsDirectory, "Routes.razor"));
        File.AppendAllText(Path.Combine(componentsDirectory, "_Imports.razor"), "@using TestProject.Client.Layout\n");
        File.AppendAllText(Path.Combine(project.ClientDirectory, "_Imports.razor"), "@using TestProject.Client.Layout\n");
        File.WriteAllText(Path.Combine(project.ClientDirectory, "Routes.razor"), """
            <Router AppAssembly="typeof(Program).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
                    <FocusOnNavigate RouteData="routeData" Selector="h1" />
                </Found>
            </Router>
            """);
        File.WriteAllText(Path.Combine(componentsDirectory, "App.razor"),
            (commentedMarkup ?? string.Empty) + ScaffoldCliHelper.GetBlazorAppRazor()
                .Replace("<HeadOutlet />", routesRenderModeAttribute is null
                    ? $"<HeadOutlet @rendermode=\"{(renderMode == "InteractiveServer" ? "@" : "")}{renderMode}\" />"
                    : $"<HeadOutlet {routesRenderModeAttribute} />")
                .Replace("<Routes />", routesRenderModeAttribute is null
                    ? $"<Routes @rendermode=\"{(renderMode == "InteractiveServer" ? "@" : "")}{renderMode}\" />"
                    : $"<Routes {routesRenderModeAttribute} />"));
        Directory.CreateDirectory(Path.Combine(serverDirectory, "Models"));
        File.WriteAllText(Path.Combine(serverDirectory, "Models", "Movie.cs"), """
            namespace TestProject.Models;

            public class Movie
            {
                public int ID { get; set; }
                public string Title { get; set; } = string.Empty;
                public System.DateTime ReleaseDate { get; set; }
                public string Genre { get; set; } = string.Empty;
                public decimal Price { get; set; }
            }
            """);
        if (individualAccounts)
        {
            var (identityExitCode, identityOutput, identityError) = await ScaffoldCliHelper.RunScaffoldAsync(
                ScaffoldCliHelper.GetTestTargetFramework(), "blazor-identity",
                "--project", serverProject, "--dataContext", "ApplicationDbContext", "--dbProvider", "sqlite-efcore");
            Assert.True(identityExitCode == 0, $"Identity scaffolding failed.\n{identityOutput}\n{identityError}");

            string contextPath = Path.Combine(serverDirectory, "Data", "ApplicationDbContext.cs");
            string context = File.ReadAllText(contextPath);
            File.WriteAllText(contextPath, context.Insert(context.LastIndexOf('}'),
                "    public Microsoft.EntityFrameworkCore.DbSet<TestProject.Models.Movie> Movie { get; set; } = default!;\n"));
        }

        string appBeforeCrud = File.ReadAllText(Path.Combine(componentsDirectory, "App.razor"));
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            ScaffoldCliHelper.GetTestTargetFramework(), "blazor-crud",
            [
                "--project", serverProject, "--model", "Movie", "--dataContext", "ApplicationDbContext",
                "--dbProvider", "sqlite-efcore", "--page", "CRUD",
                .. ScaffoldCliHelper.GetPrereleaseArguments(framework)
            ]);
        Assert.True(exitCode == 0, $"Scaffolding failed.\n{output}\n{error}");

        foreach (string page in new[] { "Index", "Create", "Edit", "Details", "Delete" })
        {
            string content = File.ReadAllText(Path.Combine(serverDirectory, "Components", "Pages", "MoviePages", page + ".razor"));
            Assert.Contains("@attribute [Microsoft.AspNetCore.Components.ExcludeFromInteractiveRouting]", content);
            Assert.False(File.Exists(Path.Combine(project.ClientDirectory, "Pages", "MoviePages", page + ".razor")));
        }

        string app = File.ReadAllText(Path.Combine(serverDirectory, "Components", "App.razor"));
        Assert.Contains($"HttpContext.AcceptsInteractiveRouting() ? {renderMode} : null", app);
        Assert.Contains("<Routes @rendermode=\"PageRenderMode\"", app);
        Assert.Contains("<HeadOutlet @rendermode=\"PageRenderMode\"", app);
        if (commentedMarkup is not null)
        {
            Assert.Contains(commentedMarkup, app);
        }

        Assert.Equal(1, app.Split("private IComponentRenderMode? PageRenderMode").Length - 1);
        Assert.Equal(1, app.Split("private HttpContext HttpContext").Length - 1);
        if (individualAccounts)
        {
            Assert.Equal(appBeforeCrud, app);
        }

        var (afterExitCode, afterOutput, afterError) = await ScaffoldCliHelper.RunBuildAsync(serverDirectory);
        Assert.True(afterExitCode == 0, $"Scaffolded app must compile.\n{afterOutput}\n{afterError}");
    }
}
