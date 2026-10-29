// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-identity")]
public class BlazorIdentityInteractivityIntegrationTests
{
    [Theory]
    [InlineData("net9.0", "9.0.*", true, "TestProject.Client")]
    [InlineData("net10.0", "10.0.*", false, "Custom.Wasm.Root")]
    [InlineData("net11.0", "11.0.*-*", true, "Custom.Auto.Root")]
    public async Task Scaffold_BlazorIdentity_GlobalInteractivityUpdatesClientAndBuilds(
        string targetFramework, string aspNetCoreVersion, bool usesInteractiveServer, string clientNamespace)
    {
        using var project = new BlazorTestProject(targetFramework);
        project.AddWebAssemblyClient(aspNetCoreVersion, usesInteractiveServer, clientNamespace);
        var renderMode = usesInteractiveServer ? "InteractiveAuto" : "InteractiveWebAssembly";
        var componentsDir = Path.Combine(project.ProjectDirectory, "Components");
        Directory.Move(Path.Combine(componentsDir, "Layout"), Path.Combine(project.ClientDirectory, "Layout"));
        File.Delete(Path.Combine(componentsDir, "Routes.razor"));
        File.AppendAllText(Path.Combine(componentsDir, "_Imports.razor"), $"@using {clientNamespace}.Layout\n");
        File.AppendAllText(Path.Combine(project.ClientDirectory, "_Imports.razor"), $"@using {clientNamespace}.Layout\n");
        File.WriteAllText(Path.Combine(project.ClientDirectory, "Routes.razor"), """
            <Router AppAssembly="typeof(Program).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
                    <FocusOnNavigate RouteData="routeData" Selector="h1" />
                </Found>
            </Router>
            """);
        File.WriteAllText(Path.Combine(componentsDir, "App.razor"),
            ScaffoldCliHelper.GetBlazorAppRazor()
                .Replace("<HeadOutlet />", $"<HeadOutlet @rendermode=\"{renderMode}\" />")
                .Replace("<Routes />", $"<Routes @rendermode=\"{renderMode}\" />"));

        Assert.False(File.Exists(Path.Combine(project.ProjectDirectory, "obj", "project.assets.json")));
        string[] prereleaseOptions = targetFramework == "net11.0" ? ["--prerelease"] : [];
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            targetFramework,
            "blazor-identity",
            [
                "--project", project.ProjectPath,
                "--dataContext", "TestDbContext",
                "--dbProvider", "sqlite-efcore",
                .. prereleaseOptions
            ]);

        Assert.True(exitCode == 0, $"CLI scaffold should succeed.\nOutput: {output}\nError: {error}");
        var accountLayoutFile = targetFramework == "net9.0" ? "AccountLayout.razor" : "ManageLayout.razor";
        Assert.Contains($"@layout {clientNamespace}.Layout.MainLayout",
            File.ReadAllText(Path.Combine(componentsDir, "Account", "Shared", accountLayoutFile)));

        var clientProgramContent = File.ReadAllText(Path.Combine(project.ClientDirectory, "Program.cs"));
        Assert.Contains("builder.Services.AddAuthorizationCore()", clientProgramContent);
        Assert.Contains("builder.Services.AddCascadingAuthenticationState()", clientProgramContent);
        Assert.Contains("builder.Services.AddAuthenticationStateDeserialization()", clientProgramContent);
        Assert.Contains("Microsoft.AspNetCore.Components.WebAssembly.Authentication", File.ReadAllText(project.ClientProjectPath));
        Assert.True(File.Exists(Path.Combine(project.ClientDirectory, "RedirectToLogin.razor")));
        Assert.False(File.Exists(Path.Combine(componentsDir, "Account", "Shared", "RedirectToLogin.razor")));
        Assert.Equal(usesInteractiveServer, File.Exists(Path.Combine(
            componentsDir, "Account", "IdentityRevalidatingAuthenticationStateProvider.cs")));

        var serverProgramContent = File.ReadAllText(Path.Combine(project.ProjectDirectory, "Program.cs"));
        Assert.Contains("AddCascadingAuthenticationState()", serverProgramContent);
        Assert.Contains("app.UseWebAssemblyDebugging();\n    app.UseMigrationsEndPoint();", serverProgramContent.Replace("\r\n", "\n"));
        Assert.Contains("AddAuthenticationStateSerialization()", serverProgramContent);
        Assert.Equal(usesInteractiveServer, serverProgramContent.Contains("IdentityRevalidatingAuthenticationStateProvider"));
        Assert.Equal(!usesInteractiveServer, serverProgramContent.Contains("AddAuthorization()"));

        var clientRoutesContent = File.ReadAllText(Path.Combine(project.ClientDirectory, "Routes.razor"));
        Assert.Contains("<AuthorizeRouteView", clientRoutesContent);
        Assert.Contains("<RedirectToLogin />", clientRoutesContent);

        var clientNavMenuContent = File.ReadAllText(Path.Combine(project.ClientDirectory, "Layout", "NavMenu.razor"));
        Assert.Contains("<AuthorizeView>", clientNavMenuContent);
        Assert.Contains("href=\"Account/Register\"", clientNavMenuContent);
        Assert.Equal(targetFramework != "net11.0", clientNavMenuContent.Contains("<AntiforgeryToken />"));
        Assert.Contains(".bi-arrow-bar-left-nav-menu {", File.ReadAllText(Path.Combine(project.ClientDirectory, "Layout", "NavMenu.razor.css")));

        var appContent = File.ReadAllText(Path.Combine(componentsDir, "App.razor"));
        Assert.Contains("<HeadOutlet @rendermode=\"PageRenderMode\" />", appContent);
        Assert.Contains("<Routes @rendermode=\"PageRenderMode\" />", appContent);
        Assert.Contains($"HttpContext.AcceptsInteractiveRouting() ? {renderMode} : null", appContent);

        var (postExitCode, postOutput, postError) = await ScaffoldCliHelper.RunBuildForFrameworkAsync(project.ProjectDirectory, targetFramework);
        Assert.True(postExitCode == 0, $"Project should build after scaffolding.\nOutput: {postOutput}\nError: {postError}");
    }

    [Fact]
    public async Task Scaffold_BlazorIdentity_PerPageInteractiveAutoBuilds()
    {
        using var project = new BlazorTestProject("net11.0");
        project.AddWebAssemblyClient("11.0.*-*", usesInteractiveServer: true, clientNamespace: "Custom.Client.Root");
        File.WriteAllText(project.ClientProjectPath, File.ReadAllText(project.ClientProjectPath).Replace(
            """<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">""",
            """<Project><Sdk Name="Microsoft.NET.Sdk.BlazorWebAssembly" />"""));
        var clientProgramPath = Path.Combine(project.ClientDirectory, "Program.cs");
        File.WriteAllText(clientProgramPath, File.ReadAllText(clientProgramPath).Replace(
            "await builder.Build().RunAsync();",
            """
            var app = builder.Build();
            await app.RunAsync();
            """));
        var routesPath = Path.Combine(project.ProjectDirectory, "Components", "Routes.razor");
        File.WriteAllText(routesPath, File.ReadAllText(routesPath).Replace(
            """<RouteView RouteData="routeData" />""",
            """<RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />"""));

        Assert.False(File.Exists(Path.Combine(project.ProjectDirectory, "obj", "project.assets.json")));
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            "net11.0",
            "blazor-identity",
            "--project", project.ProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease");

        Assert.True(exitCode == 0, $"CLI scaffold should succeed.\nOutput: {output}\nError: {error}");
        Assert.Contains("<RedirectToLogin />", File.ReadAllText(routesPath));
        Assert.True(File.Exists(Path.Combine(project.ClientDirectory, "RedirectToLogin.razor")));

        var clientProgramContent = File.ReadAllText(clientProgramPath);
        Assert.Contains("var app = builder.Build();", clientProgramContent);
        var beforeBuild = clientProgramContent[..clientProgramContent.IndexOf("builder.Build()", System.StringComparison.Ordinal)];
        Assert.Contains("builder.Services.AddAuthorizationCore()", beforeBuild);
        Assert.Contains("builder.Services.AddCascadingAuthenticationState()", beforeBuild);
        Assert.Contains("builder.Services.AddAuthenticationStateDeserialization()", beforeBuild);

        var (postExitCode, postOutput, postError) = await ScaffoldCliHelper.RunBuildForFrameworkAsync(project.ProjectDirectory, "net11.0");
        Assert.True(postExitCode == 0, $"Project should build after scaffolding.\nOutput: {postOutput}\nError: {postError}");
    }
}
