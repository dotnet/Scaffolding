// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

public class BlazorIgniteUINet10IntegrationTests : BlazorIgniteUIIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(BlazorIgniteUINet10IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_CliInvocation()
    {
        // Arrange — Blazor Web App (server project)
        SetupBlazorWebAppProject();

        // Act + Assert — dotnet scaffold aspnet blazor-igniteui --package All
        await ScaffoldAllPackagesAndAssertAsync();
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_GridLiteOnly_StandaloneWebAssembly()
    {
        // Arrange — standalone Blazor WebAssembly project (host page is wwwroot/index.html, root _Imports.razor)
        File.WriteAllText(_testProjectPath, GetWasmProjectContent());
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), GetWasmProgramCs());
        File.WriteAllText(Path.Combine(_testProjectDir, "App.razor"), GetWasmAppRazor());
        File.WriteAllText(Path.Combine(_testProjectDir, "_Imports.razor"), GetWasmImportsRazor());
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "wwwroot"));
        File.WriteAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"), GetWasmIndexHtml());

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        // Act — dotnet scaffold aspnet blazor-igniteui --package GridLite --theme material --theme-variant dark
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-igniteui",
            "--project", _testProjectPath,
            "--package", "GridLite",
            "--theme", "material",
            "--theme-variant", "dark");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — only GridLite is referenced and no service registration was added (GridLite needs none)
        var projectContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(GridLitePackageName, projectContent);
        Assert.DoesNotContain(LitePackageName, projectContent);
        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.DoesNotContain("AddIgniteUIBlazor", programContent);

        // Assert — root _Imports.razor and wwwroot/index.html were updated with the GridLite dark material theme
        var importsContent = File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor"));
        Assert.Contains(ControlsUsing, importsContent);
        var indexHtmlContent = File.ReadAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"));
        Assert.Contains("<link href=\"_content/IgniteUI.Blazor.GridLite/css/themes/dark/material.css\" rel=\"stylesheet\" />", indexHtmlContent);
        Assert.DoesNotContain("_content/IgniteUI.Blazor/themes", indexHtmlContent);

        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_SplitWebApp_UpdatesServerAndClient()
    {
        // Arrange — Blazor Web App with a WebAssembly client project ('dotnet new blazor -int WebAssembly' layout).
        // The SERVER project is targeted; the referenced .Client project must be detected and updated as well.
        SetupBlazorWebAppProject();
        File.WriteAllText(_testProjectPath, GetSplitServerProjectContent());
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), GetSplitServerProgramCs());

        var clientProjectDir = Path.Combine(_testDirectory, "TestProject.Client");
        Directory.CreateDirectory(clientProjectDir);
        File.WriteAllText(Path.Combine(clientProjectDir, "TestProject.Client.csproj"), GetSplitClientProjectContent());
        File.WriteAllText(Path.Combine(clientProjectDir, "Program.cs"), GetSplitClientProgramCs());
        File.WriteAllText(Path.Combine(clientProjectDir, "_Imports.razor"), GetSplitClientImportsRazor());

        // Act + Assert (server) — the pre-build inside restores both projects, which the client detection relies on
        var cliOutput = await ScaffoldAllPackagesAndAssertAsync();
        Assert.Contains("Detected Blazor WebAssembly project", cliOutput);

        // Assert (client) — packages, service registration and @using were applied to the client project too
        var clientProjectContent = File.ReadAllText(Path.Combine(clientProjectDir, "TestProject.Client.csproj"));
        Assert.Contains(LitePackageName, clientProjectContent);
        Assert.Contains(GridLitePackageName, clientProjectContent);

        var clientProgramContent = File.ReadAllText(Path.Combine(clientProjectDir, "Program.cs"));
        Assert.Contains("using IgniteUI.Blazor.Controls;", clientProgramContent);
        Assert.Contains("builder.Services.AddIgniteUIBlazor();", clientProgramContent);
        Assert.True(clientProgramContent.IndexOf("AddIgniteUIBlazor", StringComparison.Ordinal) < clientProgramContent.IndexOf("await builder.Build().RunAsync();", StringComparison.Ordinal),
            $"AddIgniteUIBlazor() should be registered before the WebAssembly host runs.\n{clientProgramContent}");

        var clientImportsContent = File.ReadAllText(Path.Combine(clientProjectDir, "_Imports.razor"));
        Assert.Contains(ControlsUsing, clientImportsContent);

        // the theme stylesheet is linked in the server host page only; nothing is created in the client
        Assert.False(File.Exists(Path.Combine(clientProjectDir, "wwwroot", "index.html")), "The client project must not receive a host page.");
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_LiteOnly_WebApp()
    {
        // Arrange — Blazor Web App (server project)
        SetupBlazorWebAppProject();
        await AssertBuildsAsync("before scaffolding");

        // Act — dotnet scaffold aspnet blazor-igniteui --package Lite
        await RunScaffoldAndAssertSuccessAsync("--package", "Lite");

        // Assert — only IgniteUI.Blazor.Lite is referenced, with service registration and the Lite theme
        var projectContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(LitePackageName, projectContent);
        Assert.DoesNotContain(GridLitePackageName, projectContent);

        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("using IgniteUI.Blazor.Controls;", programContent);
        Assert.Contains("builder.Services.AddIgniteUIBlazor();", programContent);

        Assert.Contains(ControlsUsing, File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor")));

        var appRazorContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.Contains(LiteBootstrapLightStylesheet, appRazorContent);
        Assert.DoesNotContain("_content/IgniteUI.Blazor.GridLite", appRazorContent);

        await AssertBuildsAsync("after scaffolding");
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_GridLiteOnly_WebApp()
    {
        // Arrange — Blazor Web App whose App.razor uses the fingerprinted asset collection (@Assets, .NET 9+)
        SetupBlazorWebAppProject();
        File.WriteAllText(Path.Combine(_testProjectDir, "Components", "App.razor"), GetAppRazorWithAssets());
        await AssertBuildsAsync("before scaffolding");

        // Act — dotnet scaffold aspnet blazor-igniteui --package GridLite
        await RunScaffoldAndAssertSuccessAsync("--package", "GridLite");

        // Assert — only GridLite is referenced; it needs no service registration, so Program.cs must be untouched
        var projectContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(GridLitePackageName, projectContent);
        Assert.DoesNotContain(LitePackageName, projectContent);

        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.DoesNotContain("AddIgniteUIBlazor", programContent);
        Assert.DoesNotContain("IgniteUI.Blazor.Controls", programContent);

        Assert.Contains(ControlsUsing, File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor")));

        // Assert — the GridLite-only stylesheet is linked with the same @Assets syntax as the existing link, right after it
        var appRazorContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        var expectedLink = $"<link rel=\"stylesheet\" href=\"@Assets[\"{GridLiteBootstrapLightStylesheet}\"]\" />";
        Assert.Contains(expectedLink, appRazorContent);
        Assert.DoesNotContain("_content/IgniteUI.Blazor/themes", appRazorContent);
        var existingLinkIndex = appRazorContent.IndexOf("@Assets[\"app.css\"]", StringComparison.Ordinal);
        var themeLinkIndex = appRazorContent.IndexOf(expectedLink, StringComparison.Ordinal);
        var headOutletIndex = appRazorContent.IndexOf("<HeadOutlet />", StringComparison.Ordinal);
        Assert.True(existingLinkIndex < themeLinkIndex && themeLinkIndex < headOutletIndex,
            $"The theme link should follow the existing stylesheet link inside <head>.\n{appRazorContent}");

        await AssertBuildsAsync("after scaffolding");
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_RerunWithGridLite_KeepsLiteThemeAndSwapsTheme()
    {
        // Arrange — Blazor Web App that first gets IgniteUI.Blazor.Lite with the default light bootstrap theme
        SetupBlazorWebAppProject();
        await AssertBuildsAsync("before scaffolding");
        await RunScaffoldAndAssertSuccessAsync("--package", "Lite");
        Assert.Contains(LiteBootstrapLightStylesheet, File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor")));

        // Act — re-run for GridLite with another theme. Lite is already referenced, so the IgniteUI.Blazor theme (not the
        // GridLite-only one) must be kept, and the existing link must be swapped rather than duplicated.
        var cliOutput = await RunScaffoldAndAssertSuccessAsync("--package", "GridLite", "--theme", "material", "--theme-variant", "dark");
        Assert.Contains("already references IgniteUI.Blazor.Lite", cliOutput);

        // Assert — both packages, exactly one theme link, pointing at the Lite dark material theme
        var projectContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(LitePackageName, projectContent);
        Assert.Contains(GridLitePackageName, projectContent);

        var appRazorContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.Equal(1, CountOccurrences(appRazorContent, "_content/IgniteUI"));
        Assert.Contains("_content/IgniteUI.Blazor/themes/dark/material.css", appRazorContent);
        Assert.DoesNotContain(LiteBootstrapLightStylesheet, appRazorContent);
        Assert.DoesNotContain("_content/IgniteUI.Blazor.GridLite", appRazorContent);

        // Assert — the second run duplicated neither the registration nor the using directive
        Assert.Equal(1, CountOccurrences(File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs")), "AddIgniteUIBlazor"));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor")), ControlsUsing));

        await AssertBuildsAsync("after scaffolding twice");
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_BlazorServer_WithProjectReference_RelativeProjectPath()
    {
        // Arrange — legacy Blazor Server layout (Pages/_Host.cshtml host page, root _Imports.razor) that references a
        // class library. The scaffolder is invoked from the solution folder with a RELATIVE --project path: this is the
        // combination that used to crash DetectBlazorWasmStep (relative base path passed to Path.GetFullPath).
        File.WriteAllText(_testProjectPath, GetBlazorServerProjectContent());
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), GetBlazorServerProgramCs());
        File.WriteAllText(Path.Combine(_testProjectDir, "App.razor"), GetBlazorServerAppRazor());
        File.WriteAllText(Path.Combine(_testProjectDir, "_Imports.razor"), GetBlazorServerImportsRazor());
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "Pages"));
        File.WriteAllText(Path.Combine(_testProjectDir, "Pages", "_Host.cshtml"), GetBlazorServerHostPage());
        File.WriteAllText(Path.Combine(_testProjectDir, "Pages", "Index.razor"), "@page \"/\"\n<h1>@SharedLib.Greeter.Hello()</h1>\n");
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "Shared"));
        File.WriteAllText(Path.Combine(_testProjectDir, "Shared", "MainLayout.razor"), "@inherits LayoutComponentBase\n<main>@Body</main>\n");
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "wwwroot", "css"));
        File.WriteAllText(Path.Combine(_testProjectDir, "wwwroot", "css", "site.css"), "body { margin: 0; }\n");

        var libraryDir = Path.Combine(_testDirectory, "SharedLib");
        Directory.CreateDirectory(libraryDir);
        File.WriteAllText(Path.Combine(libraryDir, "SharedLib.csproj"), GetClassLibraryProjectContent());
        File.WriteAllText(Path.Combine(libraryDir, "Greeter.cs"), "namespace SharedLib;\n\npublic static class Greeter\n{\n    public static string Hello() => \"Hello\";\n}\n");

        await AssertBuildsAsync("before scaffolding");

        // Act — dotnet scaffold aspnet blazor-igniteui --project TestProject\TestProject.csproj --package All (from _testDirectory)
        var relativeProjectPath = Path.Combine("TestProject", "TestProject.csproj");
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldInDirectoryAsync(
            _testDirectory,
            TargetFramework,
            "blazor-igniteui",
            "--project", relativeProjectPath,
            "--package", "All");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed with a relative --project path.\nOutput: {cliOutput}\nError: {cliError}");
        Assert.DoesNotContain("Unhandled exception", cliOutput + cliError);
        Assert.False(cliOutput.Contains("error: NU"), $"Scaffolding should not produce NuGet errors.\nOutput: {cliOutput}");

        // Assert — packages and service registration in the server project
        var projectContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(LitePackageName, projectContent);
        Assert.Contains(GridLitePackageName, projectContent);
        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("using IgniteUI.Blazor.Controls;", programContent);
        Assert.Contains("builder.Services.AddIgniteUIBlazor();", programContent);
        Assert.True(programContent.IndexOf("AddIgniteUIBlazor", StringComparison.Ordinal) < programContent.IndexOf("var app = builder.Build();", StringComparison.Ordinal),
            $"AddIgniteUIBlazor() should be registered before the app is built.\n{programContent}");

        // Assert — Blazor Server layout: root _Imports.razor gets the using, Pages/_Host.cshtml gets the theme link
        Assert.Contains(ControlsUsing, File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor")));
        // line endings are normalized because the fixture's verbatim string follows the checkout's line endings
        var hostPageContent = File.ReadAllText(Path.Combine(_testProjectDir, "Pages", "_Host.cshtml")).ReplaceLineEndings("\n");
        Assert.Contains($"    <link href=\"css/site.css\" rel=\"stylesheet\" />\n    <link href=\"{LiteBootstrapLightStylesheet}\" rel=\"stylesheet\" />", hostPageContent);
        Assert.DoesNotContain("_content/IgniteUI", File.ReadAllText(Path.Combine(_testProjectDir, "App.razor")));

        // Assert — the class library reference was inspected without being treated as a WebAssembly client
        Assert.DoesNotContain("Detected Blazor WebAssembly project", cliOutput);
        Assert.DoesNotContain("IgniteUI", File.ReadAllText(Path.Combine(libraryDir, "SharedLib.csproj")));

        await AssertBuildsAsync("after scaffolding");
    }

    private string GetBlazorServerProjectContent() => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\SharedLib\SharedLib.csproj"" />
  </ItemGroup>
</Project>";

    private string GetClassLibraryProjectContent() => $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";

    private static string GetBlazorServerProgramCs() => @"using TestProject;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage(""/_Host"");
app.Run();
";

    private static string GetBlazorServerHostPage() => @"@page ""/""
@namespace TestProject.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@using Microsoft.AspNetCore.Components.Web
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <base href=""~/"" />
    <link href=""css/site.css"" rel=""stylesheet"" />
    <component type=""typeof(HeadOutlet)"" render-mode=""ServerPrerendered"" />
</head>
<body>
    <component type=""typeof(App)"" render-mode=""ServerPrerendered"" />
    <script src=""_framework/blazor.server.js""></script>
</body>
</html>
";

    private static string GetBlazorServerAppRazor() => @"<Router AppAssembly=""@typeof(App).Assembly"">
    <Found Context=""routeData"">
        <RouteView RouteData=""@routeData"" DefaultLayout=""@typeof(MainLayout)"" />
    </Found>
    <NotFound>
        <p>Not found</p>
    </NotFound>
</Router>
";

    private static string GetBlazorServerImportsRazor() => @"@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using TestProject
@using TestProject.Shared
";

    private const string GridLiteBootstrapLightStylesheet = "_content/IgniteUI.Blazor.GridLite/css/themes/light/bootstrap.css";

    private async Task AssertBuildsAsync(string phase)
    {
        var (exitCode, output, error) = await RunBuildAsync(_testProjectDir);
        Assert.True(exitCode == 0, $"Project should build {phase}.\nExit code: {exitCode}\nOutput: {output}\nError: {error}");
    }

    private async Task<string> RunScaffoldAndAssertSuccessAsync(params string[] extraCliArgs)
    {
        string[] args = ["--project", _testProjectPath, .. extraCliArgs];
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(TargetFramework, "blazor-igniteui", args);
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");
        Assert.False(cliOutput.Contains("error: NU"), $"Scaffolding should not produce NuGet errors.\nOutput: {cliOutput}");
        return cliOutput;
    }

    private static int CountOccurrences(string content, string value) => Regex.Matches(content, Regex.Escape(value)).Count;

    /// <summary>
    /// App.razor in the .NET 9+ template style, where stylesheet links use the fingerprinted asset collection.
    /// </summary>
    private static string GetAppRazorWithAssets() => @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <base href=""/"" />
    <link rel=""stylesheet"" href=""@Assets[""app.css""]"" />
    <HeadOutlet />
</head>
<body>
    <Routes />
    <script src=""_framework/blazor.web.js""></script>
</body>
</html>
";

    private string GetSplitServerProjectContent() => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly.Server"" Version=""10.0.*"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\TestProject.Client\TestProject.Client.csproj"" />
  </ItemGroup>
</Project>";

    private static string GetSplitServerProgramCs() => @"using TestProject.Components;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode();
app.Run();
";

    private string GetSplitClientProjectContent() => $@"<Project Sdk=""Microsoft.NET.Sdk.BlazorWebAssembly"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly"" Version=""10.0.*"" />
  </ItemGroup>
</Project>";

    private static string GetSplitClientProgramCs() => @"using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

await builder.Build().RunAsync();
";

    private static string GetSplitClientImportsRazor() => @"@using System.Net.Http
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using TestProject.Client
";

    private string GetWasmProjectContent() => $@"<Project Sdk=""Microsoft.NET.Sdk.BlazorWebAssembly"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly"" Version=""10.0.*"" />
  </ItemGroup>
</Project>";

    private static string GetWasmProgramCs() => @"using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TestProject;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>(""#app"");
builder.RootComponents.Add<HeadOutlet>(""head::after"");

await builder.Build().RunAsync();
";

    private static string GetWasmAppRazor() => @"<Router AppAssembly=""typeof(App).Assembly"">
    <Found Context=""routeData"">
        <RouteView RouteData=""routeData"" />
    </Found>
</Router>
";

    private static string GetWasmImportsRazor() => @"@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using TestProject
";

    private static string GetWasmIndexHtml() => @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <title>TestProject</title>
    <base href=""/"" />
    <link rel=""stylesheet"" href=""css/app.css"" />
</head>
<body>
    <div id=""app"">Loading...</div>
    <script src=""_framework/blazor.webassembly.js""></script>
</body>
</html>
";
}
