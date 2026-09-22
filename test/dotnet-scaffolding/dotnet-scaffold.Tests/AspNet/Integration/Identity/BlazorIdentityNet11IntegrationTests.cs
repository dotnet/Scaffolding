// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

public class BlazorIdentityNet11IntegrationTests : BlazorIdentityIntegrationTestsBase
{
    protected override string TargetFramework => "net11.0";
    protected override string TestClassName => nameof(BlazorIdentityNet11IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorIdentity_Net11_CliInvocation()
    {
        // Arrange write project + Program.cs (allow warnings so preview-SDK warnings don't break the build)
        var projectContent = ProjectContent.Replace(
            "</PropertyGroup>",
            "    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>\n  </PropertyGroup>");
        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), """
            using TestProject.Components;

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents();

            CustomRegistrations.AddInteractiveWebAssemblyComponents();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();
            app.MapStaticAssets();
            app.MapRazorComponents<App>();

            app.Run();

            static class CustomRegistrations
            {
                public static void AddInteractiveWebAssemblyComponents() { }
            }
            """);
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);

        // Write a NuGet.config with the dotnet11 preview feeds so the preview-only
        // framework packages can be resolved during restore/build.
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);

        // Act invoke CLI: dotnet scaffold aspnet blazor-identity
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert expected files were created
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "TestDbContext.cs")),
            "DbContext file should be created.");
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "ApplicationUser.cs")),
            "ApplicationUser file should be created.");
        var accountPagesDir = Path.Combine(_testProjectDir, "Components", "Account", "Pages");
        Assert.True(Directory.Exists(accountPagesDir), "Components/Account/Pages directory should be created.");
        Assert.True(File.Exists(Path.Combine(accountPagesDir, "Login.razor")), "Login.razor should be created.");
        Assert.True(File.Exists(Path.Combine(accountPagesDir, "Register.razor")), "Register.razor should be created.");
        var sharedDir = Path.Combine(_testProjectDir, "Components", "Account", "Shared");
        Assert.True(Directory.Exists(sharedDir), "Components/Account/Shared directory should be created.");
        Assert.True(File.Exists(Path.Combine(sharedDir, "ManageNavMenu.razor")), "ManageNavMenu.razor should be created.");
        Assert.True(File.Exists(Path.Combine(sharedDir, "PasskeySubmit.razor.js")), "PasskeySubmit.razor.js should be created.");
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Components", "Account", "PasskeyAuthenticators.cs")),
            "PasskeyAuthenticators.cs should be created.");

        var endpointContent = File.ReadAllText(Path.Combine(
            _testProjectDir, "Components", "Account", "IdentityComponentsEndpointRouteBuilderExtensions.cs"));
        Assert.Equal(3, CountOccurrences(endpointContent, "[RequireAntiforgeryToken]"));
        Assert.Equal(3, CountOccurrences(endpointContent, "IAntiforgeryValidationFeature"));
        Assert.DoesNotContain("[FromServices] IAntiforgery antiforgery", endpointContent);

        var passkeySubmitContent = File.ReadAllText(Path.Combine(sharedDir, "PasskeySubmit.razor"));
        Assert.Contains("formnovalidate", passkeySubmitContent);
        Assert.DoesNotContain("<AntiforgeryToken", passkeySubmitContent);
        var passkeyScriptContent = File.ReadAllText(Path.Combine(sharedDir, "PasskeySubmit.razor.js"));
        Assert.DoesNotContain("RequestVerificationToken", passkeyScriptContent);
        Assert.DoesNotContain("headers:", passkeyScriptContent);

        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("TestDbContext", programContent);
        Assert.Contains("builder.Services.AddDatabaseDeveloperPageExceptionFilter()", programContent);
        Assert.Contains("app.UseMigrationsEndPoint()", programContent);
        Assert.Contains("builder.Services.AddAuthorization()", programContent);
        Assert.DoesNotContain("IdentityRevalidatingAuthenticationStateProvider>()", programContent);
        Assert.False(File.Exists(Path.Combine(
            _testProjectDir, "Components", "Account", "IdentityRevalidatingAuthenticationStateProvider.cs")));
        Assert.Contains("app.MapAdditionalIdentityEndpoints();", programContent);
        Assert.DoesNotContain("app.MapAdditionalIdentityEndpoints();;", programContent);

        var navMenuContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "Layout", "NavMenu.razor"));
        Assert.Contains("<AuthorizeView>", navMenuContent);
        Assert.Contains("href=\"Account/Register\"", navMenuContent);
        Assert.DoesNotContain("href=\"auth\"", navMenuContent);
        Assert.DoesNotContain("<AntiforgeryToken />", navMenuContent);

        // Assert no NuGet errors during scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");

        var passkeyScriptPath = Path.Combine(sharedDir, "PasskeySubmit.razor.js");
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        File.WriteAllText(passkeyScriptPath, "// stale");
        File.WriteAllText(programPath, programContent.Replace("app.MapAdditionalIdentityEndpoints();", string.Empty));
        var overwriteRun = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease",
            "--overwrite");

        Assert.True(overwriteRun.ExitCode == 0, $"Overwrite scaffold should succeed.\nOutput: {overwriteRun.Output}\nError: {overwriteRun.Error}");
        Assert.DoesNotContain("// stale", File.ReadAllText(passkeyScriptPath));
        Assert.Contains("app.MapAdditionalIdentityEndpoints();", File.ReadAllText(programPath));
        var navMenuCss = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "Layout", "NavMenu.razor.css"));
        Assert.Equal(1, CountOccurrences(navMenuCss, ".bi-arrow-bar-left-nav-menu {"));

        // Assert project builds after scaffolding.
        // net11.0 is in preview — build warnings are expected (e.g. preview SDK warnings,
        // preview NuGet package warnings) but actual build errors should not occur.
        // TreatWarningsAsErrors is false in the project so only real errors cause a
        // non-zero exit code; warnings alone will still return exit code 0.
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding with no errors (warnings are OK since net11.0 is in preview).\n" +
            $"Exit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }

    [Theory]
    [InlineData("net9.0", "9.0.*", true)]
    [InlineData("net10.0", "10.0.*", false)]
    [InlineData("net11.0", "11.0.0-rc.2.26455.110", true)]
    public async Task Scaffold_BlazorIdentity_GlobalInteractivityUpdatesClientAndBuilds(
        string targetFramework, string aspNetCoreVersion, bool usesInteractiveServer)
    {
        var renderMode = usesInteractiveServer ? "InteractiveAuto" : "InteractiveWebAssembly";
        var clientProjectDir = Path.Combine(_testDirectory, "UnexpectedClientDirectory");
        var clientProjectPath = Path.Combine(clientProjectDir, "TestProject.Client.csproj");
        Directory.CreateDirectory(Path.Combine(clientProjectDir, "Layout"));

        File.WriteAllText(_testProjectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{targetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
                <ClientProject>..\UnexpectedClientDirectory\TestProject.Client.csproj</ClientProject>
              </PropertyGroup>
              <Import Project="ClientReferences.props" />
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="{aspNetCoreVersion}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "ClientReferences.props"), """
            <Project>
              <ItemGroup>
                <ProjectReference Include="$(ClientProject)" Condition="'$(UsingMicrosoftNETSdkWeb)' == 'true'" />
                <ProjectReference Include="Excluded.csproj" Condition="'$(UsingMicrosoftNETSdkWeb)' != 'true'" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), $"""
            using TestProject.Components;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents()
                {(usesInteractiveServer ? ".AddInteractiveServerComponents()" : "")}
                .AddInteractiveWebAssemblyComponents();

            var app = builder.Build();
            app.MapRazorComponents<App>()
                {(usesInteractiveServer ? ".AddInteractiveServerRenderMode()" : "")}
                .AddInteractiveWebAssemblyRenderMode();
            app.Run();
            """);
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);
        Directory.Delete(Path.Combine(_testProjectDir, "Components", "Layout"), recursive: true);
        File.Delete(Path.Combine(_testProjectDir, "Components", "Routes.razor"));
        File.AppendAllText(
            Path.Combine(_testProjectDir, "Components", "_Imports.razor"),
            "@using TestProject.Client\n@using TestProject.Client.Layout\n");
        File.WriteAllText(Path.Combine(_testProjectDir, "Components", "App.razor"), $"""
            <!DOCTYPE html>
            <html>
            <head>
                <HeadOutlet @rendermode="{renderMode}" />
            </head>
            <body>
                <Routes @rendermode="{renderMode}" />
            </body>
            </html>
            """);

        File.WriteAllText(clientProjectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
              <PropertyGroup>
                <TargetFramework>{targetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="{aspNetCoreVersion}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(clientProjectDir, "Program.cs"), """
            using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            await builder.Build().RunAsync();
            """);
        File.WriteAllText(
            Path.Combine(clientProjectDir, "_Imports.razor"),
            ScaffoldCliHelper.GetBlazorImportsRazor() +
            "@using TestProject.Client\n@using TestProject.Client.Layout\n");
        File.WriteAllText(Path.Combine(clientProjectDir, "Routes.razor"), """
            <Router AppAssembly="typeof(Program).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
                    <FocusOnNavigate RouteData="routeData" Selector="h1" />
                </Found>
            </Router>
            """);
        File.WriteAllText(Path.Combine(clientProjectDir, "Layout", "MainLayout.razor"), """
            @inherits LayoutComponentBase

            <div class="top-row px-4">
            </div>

            @Body
            """);
        File.WriteAllText(
            Path.Combine(clientProjectDir, "Layout", "NavMenu.razor"),
            ScaffoldCliHelper.GetNavMenuRazor());
        File.WriteAllText(
            Path.Combine(clientProjectDir, "Layout", "NavMenu.razor.css"),
            ScaffoldCliHelper.GetNavMenuCss());
        File.WriteAllText(Path.Combine(_testDirectory, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);

        Assert.False(File.Exists(Path.Combine(_testProjectDir, "obj", "project.assets.json")));
        string[] prereleaseOptions = targetFramework == "net11.0" ? ["--prerelease"] : [];
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            targetFramework,
            "blazor-identity",
            [
                "--project", _testProjectPath,
                "--dataContext", "TestDbContext",
                "--dbProvider", "sqlite-efcore",
                .. prereleaseOptions
            ]);

        Assert.True(exitCode == 0, $"CLI scaffold should succeed.\nOutput: {output}\nError: {error}");

        var clientProgramContent = File.ReadAllText(Path.Combine(clientProjectDir, "Program.cs"));
        Assert.Contains("builder.Services.AddAuthorizationCore()", clientProgramContent);
        Assert.Contains("builder.Services.AddCascadingAuthenticationState()", clientProgramContent);
        Assert.Contains("builder.Services.AddAuthenticationStateDeserialization()", clientProgramContent);

        var clientProjectContent = File.ReadAllText(clientProjectPath);
        Assert.Contains("Microsoft.AspNetCore.Components.WebAssembly.Authentication", clientProjectContent);
        Assert.True(File.Exists(Path.Combine(clientProjectDir, "RedirectToLogin.razor")));
        Assert.False(File.Exists(Path.Combine(
            _testProjectDir, "Components", "Account", "Shared", "RedirectToLogin.razor")));
        Assert.Equal(usesInteractiveServer, File.Exists(Path.Combine(
            _testProjectDir,
            "Components",
            "Account",
            "IdentityRevalidatingAuthenticationStateProvider.cs")));

        var serverProgramContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("AddAuthenticationStateSerialization()", serverProgramContent);
        Assert.Equal(usesInteractiveServer, serverProgramContent.Contains("IdentityRevalidatingAuthenticationStateProvider"));
        Assert.Equal(!usesInteractiveServer, serverProgramContent.Contains("AddAuthorization()"));

        var clientRoutesContent = File.ReadAllText(Path.Combine(clientProjectDir, "Routes.razor"));
        Assert.Contains("<AuthorizeRouteView", clientRoutesContent);
        Assert.Contains("<RedirectToLogin />", clientRoutesContent);

        var clientNavMenuContent = File.ReadAllText(Path.Combine(clientProjectDir, "Layout", "NavMenu.razor"));
        Assert.Contains("<AuthorizeView>", clientNavMenuContent);
        Assert.Contains("href=\"Account/Register\"", clientNavMenuContent);
        Assert.DoesNotContain("href=\"auth\"", clientNavMenuContent);
        Assert.Equal(targetFramework != "net11.0", clientNavMenuContent.Contains("<AntiforgeryToken />"));
        Assert.Contains(".bi-arrow-bar-left-nav-menu {", File.ReadAllText(Path.Combine(clientProjectDir, "Layout", "NavMenu.razor.css")));

        var appContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.Contains("<HeadOutlet @rendermode=\"PageRenderMode\" />", appContent);
        Assert.Contains("<Routes @rendermode=\"PageRenderMode\" />", appContent);
        Assert.Contains($"HttpContext.AcceptsInteractiveRouting() ? {renderMode} : null", appContent);

        var (postExitCode, postOutput, postError) = await ScaffoldCliHelper.RunBuildForFrameworkAsync(_testProjectDir, targetFramework);
        Assert.True(
            postExitCode == 0,
            $"Project should build after scaffolding.\nOutput: {postOutput}\nError: {postError}");
    }

    [Fact]
    public async Task Scaffold_BlazorIdentity_Net11_PerPageInteractiveAutoQualifiesRedirectToLogin()
    {
        const string aspNetCoreVersion = "11.0.0-rc.2.26455.110";
        var clientProjectDir = Path.Combine(_testDirectory, "TestProject.Client");
        var clientProjectPath = Path.Combine(clientProjectDir, "TestProject.Client.csproj");
        Directory.CreateDirectory(clientProjectDir);

        File.WriteAllText(_testProjectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\TestProject.Client\TestProject.Client.csproj" />
                <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="{aspNetCoreVersion}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), """
            using TestProject.Components;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            var app = builder.Build();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode();
            app.Run();
            """);
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);
        File.AppendAllText(
            Path.Combine(_testProjectDir, "Components", "_Imports.razor"),
            "@using Custom.Client.Root\n");
        var routesPath = Path.Combine(_testProjectDir, "Components", "Routes.razor");
        File.WriteAllText(routesPath, """
            @using TestProject.Components.Account.Shared

            <Router AppAssembly="typeof(Program).Assembly">
                <Found Context="routeData">
                    <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">
                        <NotAuthorized>
                            <RedirectToLogin />
                        </NotAuthorized>
                    </AuthorizeRouteView>
                    <FocusOnNavigate RouteData="routeData" Selector="h1" />
                </Found>
            </Router>
            """);
        var legacyRedirectPath = Path.Combine(
            _testProjectDir,
            "Components",
            "Account",
            "Shared",
            "RedirectToLogin.razor");
        Directory.CreateDirectory(Path.GetDirectoryName(legacyRedirectPath)!);
        File.WriteAllText(legacyRedirectPath, "<p>Legacy redirect component</p>");

        File.WriteAllText(clientProjectPath, $"""
            <Project>
              <Sdk Name="Microsoft.NET.Sdk.BlazorWebAssembly" />
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
                <RootNamespace>Custom.Client.Root</RootNamespace>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="{aspNetCoreVersion}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(clientProjectDir, "Program.cs"), """
            using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            await builder.Build().RunAsync();
            """);
        File.WriteAllText(
            Path.Combine(clientProjectDir, "_Imports.razor"),
            ScaffoldCliHelper.GetBlazorImportsRazor() + "@using Custom.Client.Root\n");
        File.WriteAllText(Path.Combine(_testDirectory, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);

        Assert.False(File.Exists(Path.Combine(_testProjectDir, "obj", "project.assets.json")));
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease");

        Assert.True(exitCode == 0, $"CLI scaffold should succeed.\nOutput: {output}\nError: {error}");
        Assert.Contains("<Custom.Client.Root.RedirectToLogin />", File.ReadAllText(routesPath));
        Assert.True(File.Exists(legacyRedirectPath));
        Assert.True(File.Exists(Path.Combine(clientProjectDir, "RedirectToLogin.razor")));

        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(
            postExitCode == 0,
            $"Project should build after scaffolding.\nOutput: {postOutput}\nError: {postError}");
    }

    [Theory]
    [InlineData("blazor-identity", 0, false, "No referenced project using the Microsoft.NET.Sdk.BlazorWebAssembly SDK was found.")]
    [InlineData("blazor-identity", 2, false, "Multiple referenced projects use the Microsoft.NET.Sdk.BlazorWebAssembly SDK")]
    [InlineData("identity", 0, true, "Unable to determine a supported target framework")]
    public async Task Scaffold_Identity_Net11_ProjectDiscoveryFailureDoesNotMutateProject(
        string scaffolder, int clientCount, bool missingImport, string expectedDiagnostic)
    {
        var references = string.Empty;
        for (var index = 0; index < clientCount; index++)
        {
            var clientDirectory = Path.Combine(_testDirectory, $"Client{index}");
            Directory.CreateDirectory(clientDirectory);
            var clientProjectPath = Path.Combine(clientDirectory, $"Client{index}.csproj");
            File.WriteAllText(clientProjectPath, $"""
                <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
                  <PropertyGroup><TargetFramework>{TargetFramework}</TargetFramework></PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="11.0.0-rc.2.26455.110" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(clientDirectory, "Program.cs"), """
                using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
                var builder = WebAssemblyHostBuilder.CreateDefault(args);
                await builder.Build().RunAsync();
                """);
            references += $"""<ProjectReference Include="..\Client{index}\Client{index}.csproj" />""";
        }

        var import = missingImport ? """<Import Project="MissingClientReferences.props" />""" : string.Empty;
        var projectContent = ProjectContent.Replace("</Project>", $"""
            <ItemGroup>
              {references}
              <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="11.0.0-rc.2.26455.110" />
            </ItemGroup>
            {import}
            </Project>
            """);
        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(Path.Combine(_testDirectory, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        const string programContent = """
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
            builder.Build().Run();
            """;
        File.WriteAllText(programPath, programContent);

        var (_, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            scaffolder,
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease");

        Assert.Contains(expectedDiagnostic, output + error);
        Assert.Equal(projectContent, File.ReadAllText(_testProjectPath));
        Assert.Equal(programContent, File.ReadAllText(programPath));
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Data")));
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Areas", "Identity")));
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Components", "Account")));
    }

    [Theory]
    [InlineData("net11.0", true, "Unable to restore", "Test restore failure")]
    [InlineData("net11.0", false, "Unable to resolve Blazor registration", "AddInteractiveWebAssemblyComponents")]
    [InlineData("net8.0", false, "Unable to resolve Blazor registration", "AddInteractiveWebAssemblyComponents")]
    public async Task Scaffold_Identity_AnalysisFailureDoesNotMutateProject(
        string targetFramework, bool failRestore, string expectedDiagnostic, string expectedDetail)
    {
        var projectContent = failRestore
            ? ProjectContent.Replace("</Project>", """
                <Target Name="FailRestore" BeforeTargets="Restore">
                  <Error Text="Test restore failure" />
                </Target>
                </Project>
                """)
            : ProjectContent;
        projectContent = projectContent.Replace(TargetFramework, targetFramework);
        File.WriteAllText(_testProjectPath, projectContent);
        File.WriteAllText(Path.Combine(_testDirectory, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        const string programContent = """
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
            builder.Build().Run();
            """;
        File.WriteAllText(programPath, programContent);

        var (_, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            targetFramework,
            "blazor-identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease");

        Assert.Contains(expectedDiagnostic, output + error);
        Assert.Contains(expectedDetail, output + error);
        Assert.Equal(projectContent, File.ReadAllText(_testProjectPath));
        Assert.Equal(programContent, File.ReadAllText(programPath));
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Data")));
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Areas", "Identity")));
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Components", "Account")));
    }

    private static int CountOccurrences(string value, string substring)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(substring, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += substring.Length;
        }

        return count;
    }
}
