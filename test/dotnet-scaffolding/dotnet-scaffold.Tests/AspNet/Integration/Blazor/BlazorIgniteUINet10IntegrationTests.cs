// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        // Act + Assert — dotnet scaffold aspnet blazor-igniteui --project TestProject.csproj
        await ScaffoldAllPackagesAndAssertAsync();
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_StandaloneWebAssembly_DarkMaterialTheme()
    {
        // Arrange — standalone Blazor WebAssembly project (host page is wwwroot/index.html, root _Imports.razor)
        File.WriteAllText(_testProjectPath, GetWasmProjectContent());
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), GetWasmProgramCs());
        File.WriteAllText(Path.Combine(_testProjectDir, "App.razor"), GetWasmAppRazor());
        File.WriteAllText(Path.Combine(_testProjectDir, "_Imports.razor"), GetWasmImportsRazor());
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "wwwroot"));
        File.WriteAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"), GetWasmIndexHtml());
        await AssertBuildsAsync("before scaffolding");

        // Act — dotnet scaffold aspnet blazor-igniteui --theme material --theme-variant dark
        await RunScaffoldAndAssertSuccessAsync("--theme", "material", "--theme-variant", "dark");

        // Assert — both packages are referenced and the services are registered before the WebAssembly host runs
        var projectContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(LitePackageName, projectContent);
        Assert.Contains(GridLitePackageName, projectContent);
        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("using IgniteUI.Blazor.Controls;", programContent);
        Assert.Contains("builder.Services.AddIgniteUIBlazor();", programContent);
        Assert.True(programContent.IndexOf("AddIgniteUIBlazor", StringComparison.Ordinal) < programContent.IndexOf("await builder.Build().RunAsync();", StringComparison.Ordinal),
            $"AddIgniteUIBlazor() should be registered before the WebAssembly host runs.\n{programContent}");

        // Assert — root _Imports.razor and wwwroot/index.html were updated with the dark material theme
        Assert.Contains(ControlsUsing, File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor")));
        var indexHtmlContent = File.ReadAllText(Path.Combine(_testProjectDir, "wwwroot", "index.html"));
        Assert.Contains("<link href=\"_content/IgniteUI.Blazor/themes/dark/material.css\" rel=\"stylesheet\" />", indexHtmlContent);
        Assert.Equal(1, CountOccurrences(indexHtmlContent, "_content/IgniteUI"));

        await AssertBuildsAsync("after scaffolding");
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_SplitWebApp_UpdatesServerAndClient()
    {
        // Arrange — Blazor Web App with a WebAssembly client project ('dotnet new blazor -int WebAssembly' layout).
        // The SERVER project is targeted; the referenced .Client project must be discovered and updated as well.
        var clientProjectDir = SetupSplitWebApp();

        // Neither project has been restored: client discovery evaluates the project references and the client's SDK
        // instead of reading restore output.
        Assert.False(File.Exists(Path.Combine(_testProjectDir, "obj", "project.assets.json")));
        Assert.False(File.Exists(Path.Combine(clientProjectDir, "obj", "project.assets.json")));

        // Act + Assert (server)
        var cliOutput = await ScaffoldAllPackagesAndAssertAsync(buildBeforeScaffolding: false);
        Assert.Contains("Found Blazor WebAssembly client project", cliOutput);

        // Assert (hosting) — the WebAssembly configuration is kept, and the guidance only suggests WebAssembly
        Assert.DoesNotContain("AddInteractiveServer", File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs")));
        Assert.Contains("'@rendermode InteractiveWebAssembly'", cliOutput);
        Assert.DoesNotContain("InteractiveAuto", cliOutput);

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
    public async Task Scaffold_BlazorIgniteUI_Net10_SplitWebApp_MultipleClients_FailsBeforeChanges()
    {
        // Arrange — the server references two Blazor WebAssembly projects, so the client to configure is ambiguous
        SetupSplitWebApp();
        var secondClientProjectDir = Path.Combine(_testDirectory, "TestProject.Client2");
        Directory.CreateDirectory(secondClientProjectDir);
        File.WriteAllText(Path.Combine(secondClientProjectDir, "TestProject.Client2.csproj"), GetSplitClientProjectContent());
        File.WriteAllText(_testProjectPath, File.ReadAllText(_testProjectPath).Replace(
            "</ItemGroup>\n</Project>",
            "  <ProjectReference Include=\"..\\TestProject.Client2\\TestProject.Client2.csproj\" />\n  </ItemGroup>\n</Project>"));
        Assert.Contains("TestProject.Client2.csproj", File.ReadAllText(_testProjectPath));

        // Act + Assert
        var cliOutput = await ScaffoldAndAssertFailsWithoutChangesAsync();
        Assert.Contains("Multiple referenced projects use the Microsoft.NET.Sdk.BlazorWebAssembly SDK", cliOutput);
        Assert.Contains("TestProject.Client.csproj", cliOutput);
        Assert.Contains("TestProject.Client2.csproj", cliOutput);
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_SplitWebApp_MissingClientProject_FailsBeforeChanges()
    {
        // Arrange — the server references a client project that does not exist on disk
        SetupSplitWebApp();
        File.WriteAllText(_testProjectPath, File.ReadAllText(_testProjectPath).Replace(
            "TestProject.Client\\TestProject.Client.csproj", "TestProject.Missing\\TestProject.Missing.csproj"));

        // Act + Assert
        var cliOutput = await ScaffoldAndAssertFailsWithoutChangesAsync();
        Assert.Contains("TestProject.Missing.csproj", cliOutput);
        Assert.Contains("was not found", cliOutput);
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_SplitWebApp_ClientEvaluationFails_FailsBeforeChanges()
    {
        // Arrange — the client project imports a file that does not exist, so MSBuild cannot evaluate it
        var clientProjectDir = SetupSplitWebApp();
        var clientProjectPath = Path.Combine(clientProjectDir, "TestProject.Client.csproj");
        File.WriteAllText(clientProjectPath, File.ReadAllText(clientProjectPath).Replace(
            "</Project>", "  <Import Project=\"Missing.Client.props\" />\n</Project>"));
        Assert.Contains("Missing.Client.props", File.ReadAllText(clientProjectPath));

        // Act + Assert
        var cliOutput = await ScaffoldAndAssertFailsWithoutChangesAsync();
        Assert.Contains("Unable to evaluate referenced project", cliOutput);
        Assert.Contains("TestProject.Client.csproj", cliOutput);
        Assert.Contains("Missing.Client.props", cliOutput);
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_RazorPagesApp_FailsBeforeChanges()
    {
        // Arrange — an ASP.NET Core Razor Pages app: Web SDK, but no Razor components and no WebAssembly client
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddRazorPages();\nvar app = builder.Build();\napp.MapRazorPages();\napp.Run();\n");
        Directory.CreateDirectory(Path.Combine(_testProjectDir, "Pages"));
        File.WriteAllText(Path.Combine(_testProjectDir, "Pages", "Index.cshtml"), "@page\n<h1>Hello</h1>\n");

        // Act + Assert
        var cliOutput = await ScaffoldAndAssertFailsWithoutChangesAsync();
        Assert.Contains("is not a supported Blazor app", cliOutput);
        Assert.Contains("includes no Razor components", cliOutput);
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_CommercialPackageImportedInServer_FailsBeforeChanges()
    {
        // Arrange — the commercial package is referenced from Directory.Build.props, not from the project file
        SetupBlazorWebAppProject();
        var directoryBuildProps = Path.Combine(_testProjectDir, "Directory.Build.props");
        File.WriteAllText(directoryBuildProps, """
            <Project>
              <ItemGroup>
                <PackageReference Include="IgniteUI.Blazor" Version="25.1.0" />
              </ItemGroup>
            </Project>
            """);
        Assert.DoesNotContain("IgniteUI", File.ReadAllText(_testProjectPath));

        // Act + Assert
        var cliOutput = await ScaffoldAndAssertFailsWithoutChangesAsync();
        Assert.Contains("references the commercial IgniteUI.Blazor package", cliOutput);
        Assert.Contains(directoryBuildProps, cliOutput);
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_SplitWebApp_CommercialPackageInClient_FailsBeforeChanges()
    {
        // Arrange — only the WebAssembly client references the commercial trial package, behind a condition
        var clientProjectDir = SetupSplitWebApp();
        var clientProjectPath = Path.Combine(clientProjectDir, "TestProject.Client.csproj");
        File.WriteAllText(clientProjectPath, File.ReadAllText(clientProjectPath).Replace(
            "</Project>",
            "  <ItemGroup>\n    <PackageReference Include=\"IgniteUI.Blazor.Trial\" Version=\"25.1.0\" Condition=\"'$(Configuration)' == 'Debug'\" />\n  </ItemGroup>\n</Project>"));

        // Act + Assert
        var cliOutput = await ScaffoldAndAssertFailsWithoutChangesAsync();
        Assert.Contains($"'{clientProjectPath}' references the commercial IgniteUI.Blazor.Trial package", cliOutput);
    }

    [Theory]
    [InlineData("--theme", "neon", new[] { "bootstrap", "material", "fluent", "indigo" })]
    [InlineData("--theme-variant", "night", new[] { "light", "dark" })]
    public async Task Scaffold_BlazorIgniteUI_Net10_UnsupportedThemeOption_FailsBeforeChanges(string option, string value, string[] supportedValues)
    {
        // Arrange
        SetupBlazorWebAppProject();

        // Act + Assert — the command line rejects the value and lists the supported ones before any step runs
        // (ValidateIgniteUIBlazorStep rejects it as well when the step is driven without the command-line parser)
        var cliOutput = await ScaffoldAndAssertFailsWithoutChangesAsync(option, value);
        Assert.Contains($"Argument '{value}' not recognized. Must be one of:", cliOutput);
        Assert.All(supportedValues, supportedValue => Assert.Contains($"'{supportedValue}'", cliOutput));
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_WebAppWithoutTemplateLayout_ReportsIncompleteThemeSetup()
    {
        // Arrange — a Blazor Web App whose components live in 'UI/' instead of the template's 'Components/'
        SetupBlazorWebAppProject();
        Directory.Move(Path.Combine(_testProjectDir, "Components"), Path.Combine(_testProjectDir, "UI"));
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        File.WriteAllText(programPath, File.ReadAllText(programPath).Replace("using TestProject.Components;", "using TestProject.UI;"));
        await AssertBuildsAsync("before scaffolding");

        // Act
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(TargetFramework, "blazor-igniteui", "--project", _testProjectPath);

        // Assert — the project is accepted; packages, services and the root _Imports.razor are set up
        var projectContent = File.ReadAllText(_testProjectPath);
        Assert.Contains(LitePackageName, projectContent);
        Assert.Contains(GridLitePackageName, projectContent);
        Assert.Contains("builder.Services.AddIgniteUIBlazor();", File.ReadAllText(programPath));
        Assert.Contains(ControlsUsing, File.ReadAllText(Path.Combine(_testProjectDir, "_Imports.razor")));

        // Assert — no known host page exists, so the required stylesheet link could not be added: the run fails and
        // says exactly what is left to do
        Assert.True(cliExitCode != 0, $"CLI scaffold should report the incomplete setup as a failure.\nOutput: {cliOutput}\nError: {cliError}");
        Assert.Contains("Ignite UI for Blazor setup is incomplete", cliOutput + cliError);
        Assert.Contains($"<link href=\"{LiteBootstrapLightStylesheet}\" rel=\"stylesheet\" />", cliOutput + cliError);
        Assert.DoesNotContain("_content/IgniteUI", File.ReadAllText(Path.Combine(_testProjectDir, "UI", "App.razor")));

        await AssertBuildsAsync("after scaffolding");
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_PackageInstallFails_StopsBeforeOtherChanges()
    {
        // Arrange — the only package source is an empty local folder, so 'dotnet add package' fails
        SetupBlazorWebAppProject();
        var emptyFeed = Path.Combine(_testDirectory, "empty-feed");
        Directory.CreateDirectory(emptyFeed);
        File.WriteAllText(Path.Combine(_testDirectory, "NuGet.config"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="empty" value="{emptyFeed}" />
              </packageSources>
            </configuration>
            """);

        // Act + Assert — the shared package step reports the failure and no later step runs
        var cliOutput = await ScaffoldAndAssertFailsWithoutChangesAsync();
        Assert.Contains($"Failed to add package '{LitePackageName}'", cliOutput);
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_UnrecognizedProgram_ReportsIncompleteServiceRegistration()
    {
        // Arrange — a Startup-based app: there is no 'builder.Build()' for the code modifier to insert the registration before
        SetupBlazorWebAppProject();
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        File.WriteAllText(programPath, """
            public class Program
            {
                public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

                public static IHostBuilder CreateHostBuilder(string[] args) =>
                    Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(web => web.Configure(app => { }));
            }
            """);
        var importsPath = Path.Combine(_testProjectDir, "Components", "_Imports.razor");
        var appRazorPath = Path.Combine(_testProjectDir, "Components", "App.razor");
        var importsBefore = File.ReadAllText(importsPath);
        var appRazorBefore = File.ReadAllText(appRazorPath);

        // Act
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(TargetFramework, "blazor-igniteui", "--project", _testProjectPath);

        // Assert — the run fails with the manual registration instructions and stops before _Imports.razor and the theme
        Assert.True(cliExitCode != 0, $"CLI scaffold should fail.\nOutput: {cliOutput}\nError: {cliError}");
        Assert.Contains("Ignite UI for Blazor setup is incomplete", cliOutput + cliError);
        Assert.Contains("builder.Services.AddIgniteUIBlazor();", cliOutput + cliError);
        Assert.DoesNotContain("AddIgniteUIBlazor", File.ReadAllText(programPath));
        Assert.Equal(importsBefore, File.ReadAllText(importsPath));
        Assert.Equal(appRazorBefore, File.ReadAllText(appRazorPath));
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_WebAppWithoutInteractivity_AddsInteractiveServerSupport()
    {
        // Arrange — a Blazor Web App that renders only static SSR pages
        SetupBlazorWebAppProject();
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        File.WriteAllText(programPath, GetWebAppProgramCs(string.Empty, string.Empty));
        var appRazorPath = Path.Combine(_testProjectDir, "Components", "App.razor");
        var routesRazorPath = Path.Combine(_testProjectDir, "Components", "Routes.razor");
        var routesBefore = File.ReadAllText(routesRazorPath);
        await AssertBuildsAsync("before scaffolding");

        // Act
        var cliOutput = await RunScaffoldAndAssertSuccessAsync();

        // Assert — Interactive Server support was added to the existing chains, once
        var programContent = File.ReadAllText(programPath);
        var servicesChain = programContent[programContent.IndexOf("builder.Services.AddRazorComponents()", StringComparison.Ordinal)..programContent.IndexOf("var app", StringComparison.Ordinal)];
        Assert.Contains(".AddInteractiveServerComponents()", servicesChain);
        var endpointsChain = programContent[programContent.IndexOf("app.MapRazorComponents<App>()", StringComparison.Ordinal)..];
        Assert.Contains(".AddInteractiveServerRenderMode()", endpointsChain);
        Assert.Equal(1, CountOccurrences(programContent, "AddInteractiveServerComponents"));
        Assert.Equal(1, CountOccurrences(programContent, "AddInteractiveServerRenderMode"));

        // Assert — no global or page render mode was set: the existing static SSR pages stay static
        Assert.DoesNotContain("@rendermode", File.ReadAllText(appRazorPath));
        Assert.Equal(routesBefore, File.ReadAllText(routesRazorPath));

        // Assert — the guidance names the render mode that was added, and nothing else
        Assert.Contains("Interactive Server support was added to Program.cs", cliOutput);
        Assert.Contains("'@rendermode InteractiveServer'", cliOutput);
        Assert.DoesNotContain("InteractiveAuto", cliOutput);
        Assert.DoesNotContain("InteractiveWebAssembly", cliOutput);
        await AssertBuildsAsync("after scaffolding");

        // Act + Assert — re-running keeps a single registration of each
        await RunScaffoldAndAssertSuccessAsync();
        programContent = File.ReadAllText(programPath);
        Assert.Equal(1, CountOccurrences(programContent, "AddInteractiveServerComponents"));
        Assert.Equal(1, CountOccurrences(programContent, "AddInteractiveServerRenderMode"));
        Assert.Equal(1, CountOccurrences(programContent, "AddIgniteUIBlazor"));
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_WebAppWithInteractiveServer_KeepsConfiguration()
    {
        // Arrange — a Blazor Web App that already configures Interactive Server
        SetupBlazorWebAppProject();
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        File.WriteAllText(programPath, GetWebAppProgramCs("\n    .AddInteractiveServerComponents()", "\n    .AddInteractiveServerRenderMode()"));

        // Act
        var cliOutput = await RunScaffoldAndAssertSuccessAsync();

        // Assert — the existing Interactive Server configuration is kept as is; no other render mode is added
        var programContent = File.ReadAllText(programPath);
        Assert.Contains("builder.Services.AddIgniteUIBlazor();", programContent);
        Assert.Equal(1, CountOccurrences(programContent, "AddInteractiveServerComponents"));
        Assert.Equal(1, CountOccurrences(programContent, "AddInteractiveServerRenderMode"));
        Assert.DoesNotContain("InteractiveWebAssembly", programContent);
        Assert.DoesNotContain("Interactive Server support was added", cliOutput);
        Assert.Contains("'@rendermode InteractiveServer'", cliOutput);
        Assert.DoesNotContain("InteractiveAuto", cliOutput);
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_WebApp_WithAssetsCollection()
    {
        // Arrange — Blazor Web App whose App.razor uses the fingerprinted asset collection (@Assets, .NET 9+)
        SetupBlazorWebAppProject();
        File.WriteAllText(Path.Combine(_testProjectDir, "Components", "App.razor"), GetAppRazorWithAssets());
        await AssertBuildsAsync("before scaffolding");

        // Act
        await RunScaffoldAndAssertSuccessAsync();

        // Assert — the theme is linked with the same @Assets syntax as the existing link, right before </head>
        var appRazorContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor")).ReplaceLineEndings("\n");
        Assert.Contains($"    <HeadOutlet />\n    <link rel=\"stylesheet\" href=\"@Assets[\"{LiteBootstrapLightStylesheet}\"]\" />\n</head>", appRazorContent);

        await AssertBuildsAsync("after scaffolding");
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_Rerun_ReplacesGridLiteOnlyThemeAndSwapsTheme()
    {
        // Arrange — Blazor Web App whose App.razor links the GridLite-only theme, as earlier versions of the scaffolder
        // did for '--package GridLite'
        SetupBlazorWebAppProject();
        var appRazorPath = Path.Combine(_testProjectDir, "Components", "App.razor");
        File.WriteAllText(appRazorPath, File.ReadAllText(appRazorPath).Replace(
            "<HeadOutlet />", $"<link href=\"{GridLiteBootstrapLightStylesheet}\" rel=\"stylesheet\" />\n    <HeadOutlet />"));
        Assert.Contains(GridLiteBootstrapLightStylesheet, File.ReadAllText(appRazorPath));
        await AssertBuildsAsync("before scaffolding");

        // Act — the first run replaces the GridLite-only theme with the IgniteUI.Blazor theme, which styles the grid too
        await RunScaffoldAndAssertSuccessAsync();
        var appRazorContent = File.ReadAllText(appRazorPath);
        Assert.Contains(LiteBootstrapLightStylesheet, appRazorContent);
        Assert.DoesNotContain(GridLiteBootstrapLightStylesheet, appRazorContent);

        // Act — the second run with another theme swaps the existing link rather than adding a second one
        await RunScaffoldAndAssertSuccessAsync("--theme", "material", "--theme-variant", "dark");

        // Assert — exactly one theme link, pointing at the dark material theme
        appRazorContent = File.ReadAllText(appRazorPath);
        Assert.Equal(1, CountOccurrences(appRazorContent, "_content/IgniteUI"));
        Assert.Contains("_content/IgniteUI.Blazor/themes/dark/material.css", appRazorContent);

        // Assert — re-running duplicated neither the packages, the registration nor the using directive
        var projectContent = File.ReadAllText(_testProjectPath);
        Assert.Equal(1, CountOccurrences(projectContent, $"\"{LitePackageName}\""));
        Assert.Equal(1, CountOccurrences(projectContent, $"\"{GridLitePackageName}\""));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs")), "AddIgniteUIBlazor"));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(Path.Combine(_testProjectDir, "Components", "_Imports.razor")), ControlsUsing));

        await AssertBuildsAsync("after scaffolding twice");
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_Rerun_PreservesLinkedThemeUnlessOverridden()
    {
        // Arrange — a Blazor Web App set up with the dark material theme
        SetupBlazorWebAppProject();
        var appRazorPath = Path.Combine(_testProjectDir, "Components", "App.razor");
        await RunScaffoldAndAssertSuccessAsync("--theme", "material", "--theme-variant", "dark");
        Assert.Contains("_content/IgniteUI.Blazor/themes/dark/material.css", File.ReadAllText(appRazorPath));

        // Act + Assert — a re-run without theme options keeps the linked theme instead of resetting it to bootstrap/light
        var cliOutput = await RunScaffoldAndAssertSuccessAsync();
        Assert.Contains("Keeping the Ignite UI theme 'material' (dark) already linked", cliOutput);
        AssertSingleThemeLink(appRazorPath, "_content/IgniteUI.Blazor/themes/dark/material.css");

        // Act + Assert — only '--theme' is given: the theme changes and the linked variant is kept
        await RunScaffoldAndAssertSuccessAsync("--theme", "fluent");
        AssertSingleThemeLink(appRazorPath, "_content/IgniteUI.Blazor/themes/dark/fluent.css");

        // Act + Assert — only '--theme-variant' is given: the variant changes and the linked theme is kept
        await RunScaffoldAndAssertSuccessAsync("--theme-variant", "light");
        AssertSingleThemeLink(appRazorPath, "_content/IgniteUI.Blazor/themes/light/fluent.css");
    }

    private static void AssertSingleThemeLink(string hostPagePath, string expectedStylesheet)
    {
        var content = File.ReadAllText(hostPagePath);
        Assert.Equal(1, CountOccurrences(content, "_content/IgniteUI"));
        Assert.Contains(expectedStylesheet, content);
    }

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net10_BlazorServer_WithProjectReference_RelativeProjectPath()
    {
        // Arrange — legacy Blazor Server layout (Pages/_Host.cshtml host page, root _Imports.razor) that references a
        // class library. The scaffolder is invoked from the solution folder with a RELATIVE --project path, so client
        // discovery must resolve the reference relative to the project rather than the current directory.
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

        // Act — dotnet scaffold aspnet blazor-igniteui --project TestProject\TestProject.csproj (from _testDirectory)
        var relativeProjectPath = Path.Combine("TestProject", "TestProject.csproj");
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldInDirectoryAsync(
            _testDirectory,
            TargetFramework,
            "blazor-igniteui",
            "--project", relativeProjectPath);
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
        Assert.Contains($"    <component type=\"typeof(HeadOutlet)\" render-mode=\"ServerPrerendered\" />\n    <link href=\"{LiteBootstrapLightStylesheet}\" rel=\"stylesheet\" />\n</head>", hostPageContent);
        Assert.DoesNotContain("_content/IgniteUI", File.ReadAllText(Path.Combine(_testProjectDir, "App.razor")));

        // Assert — the class library reference was inspected without being treated as a WebAssembly client
        Assert.DoesNotContain("Found Blazor WebAssembly client project", cliOutput);
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
    /// Program.cs of a Blazor Web App, with the given calls chained after 'AddRazorComponents()' and 'MapRazorComponents&lt;App&gt;()'.
    /// </summary>
    private static string GetWebAppProgramCs(string servicesChain, string endpointsChain) => $"""
        using TestProject.Components;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents(){servicesChain};

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapRazorComponents<App>(){endpointsChain};

        app.Run();

        """;

    /// <summary>
    /// Writes a Blazor Web App server project that references a Blazor WebAssembly client project.
    /// </summary>
    /// <returns>The client project directory.</returns>
    private string SetupSplitWebApp()
    {
        SetupBlazorWebAppProject();
        File.WriteAllText(_testProjectPath, GetSplitServerProjectContent().ReplaceLineEndings("\n"));
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), GetSplitServerProgramCs());

        var clientProjectDir = Path.Combine(_testDirectory, "TestProject.Client");
        Directory.CreateDirectory(clientProjectDir);
        File.WriteAllText(Path.Combine(clientProjectDir, "TestProject.Client.csproj"), GetSplitClientProjectContent());
        File.WriteAllText(Path.Combine(clientProjectDir, "Program.cs"), GetSplitClientProgramCs());
        File.WriteAllText(Path.Combine(clientProjectDir, "_Imports.razor"), GetSplitClientImportsRazor());
        return clientProjectDir;
    }

    /// <summary>
    /// Runs the scaffolder and asserts that it fails without creating or modifying any source file in the test
    /// directory (build output under obj/ and bin/ is ignored).
    /// </summary>
    /// <returns>The scaffolder's combined console output.</returns>
    private async Task<string> ScaffoldAndAssertFailsWithoutChangesAsync(params string[] extraCliArgs)
    {
        var before = SnapshotSourceFiles();

        string[] args = ["--project", _testProjectPath, .. extraCliArgs];
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(TargetFramework, "blazor-igniteui", args);

        Assert.True(cliExitCode != 0, $"CLI scaffold should fail.\nOutput: {cliOutput}\nError: {cliError}");
        Assert.DoesNotContain("Unhandled exception", cliOutput + cliError);
        Assert.Equal(before, SnapshotSourceFiles());
        return cliOutput + cliError;
    }

    private SortedDictionary<string, string> SnapshotSourceFiles()
    {
        var snapshot = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(_testDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_testDirectory, file);
            var segments = relativePath.Split(Path.DirectorySeparatorChar);
            if (!Array.Exists(segments, segment => segment is "obj" or "bin"))
            {
                snapshot[relativePath] = File.ReadAllText(file);
            }
        }

        return snapshot;
    }

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
