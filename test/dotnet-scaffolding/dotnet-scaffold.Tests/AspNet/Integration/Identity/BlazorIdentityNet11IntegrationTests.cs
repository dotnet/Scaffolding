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
    public async Task Scaffold_BlazorIdentity_Net11_AlreadyConfiguredDoesNotModifyProject()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        var programPath = Path.Combine(_testProjectDir, "Program.cs");
        var appSettingsPath = Path.Combine(_testProjectDir, "appsettings.json");
        var loginPath = Path.Combine(_testProjectDir, "Components", "Account", "Pages", "Login.razor");
        var endpointsPath = Path.Combine(_testProjectDir, "Components", "Account", "IdentityComponentsEndpointRouteBuilderExtensions.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(loginPath)!);
        File.WriteAllText(programPath, ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        File.WriteAllText(appSettingsPath, """{"ConnectionStrings":{"DefaultConnection":"DataSource=Data/app.db;Cache=Shared"}}""");
        File.WriteAllText(loginPath, "@page \"/Account/Login\"");
        File.WriteAllText(endpointsPath, "namespace Microsoft.AspNetCore.Routing;");

        var projectContent = File.ReadAllText(_testProjectPath);
        var programContent = File.ReadAllText(programPath);
        var appSettingsContent = File.ReadAllText(appSettingsPath);

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-identity",
            "--project", _testProjectPath,
            "--dataContext", "ApplicationDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease");

        Assert.True(exitCode == 0, $"CLI scaffold should succeed.\nOutput: {output}\nError: {error}");
        Assert.Contains("Blazor Identity is already configured. No changes were made.", output);
        Assert.Equal(projectContent, File.ReadAllText(_testProjectPath));
        Assert.Equal(programContent, File.ReadAllText(programPath));
        Assert.Equal(appSettingsContent, File.ReadAllText(appSettingsPath));
        Assert.Equal("@page \"/Account/Login\"", File.ReadAllText(loginPath));
        Assert.Equal("namespace Microsoft.AspNetCore.Routing;", File.ReadAllText(endpointsPath));
    }

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
            """);
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);

        // Write a NuGet.config with the dotnet11 preview feeds so the preview-only
        // framework packages can be resolved during restore/build.
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);

        // Assert project builds before scaffolding (exit code 0 means success, warnings are OK)
        // Note: net11.0 SDK may produce warnings about using preview features or NuGet packages, but the build should succeed
        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

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

        var redirectManagerContent = File.ReadAllText(Path.Combine(
            _testProjectDir, "Components", "Account", "IdentityRedirectManager.cs"));
        Assert.DoesNotContain("Append(StatusMessageCookieName", redirectManagerContent);
        Assert.DoesNotContain("RedirectToCurrentPageWithStatus", redirectManagerContent);
        var changePasswordContent = File.ReadAllText(Path.Combine(accountPagesDir, "Manage", "ChangePassword.razor"));
        Assert.Contains("[SupplyParameterFromTempData(Name = IdentityRedirectManager.StatusMessageKey)]", changePasswordContent);
        Assert.Contains("<StatusMessage Message=\"@message\" />", changePasswordContent);
        var invalidUserContent = File.ReadAllText(Path.Combine(accountPagesDir, "InvalidUser.razor"));
        Assert.Contains("Unable to load user with ID '{UserManager.GetUserId(HttpContext.User)}'.", invalidUserContent);

        var passkeysContent = File.ReadAllText(Path.Combine(accountPagesDir, "Manage", "Passkeys.razor"));
        Assert.Contains("PasskeyAuthenticators.GetDisplayName(passkey)", passkeysContent);
        Assert.Contains("passkey.CreatedAt.UtcDateTime", passkeysContent);
        Assert.Contains("TryGetDefaultDisplayName", passkeysContent);
        var authenticatorsContent = File.ReadAllText(Path.Combine(
            _testProjectDir, "Components", "Account", "PasskeyAuthenticators.cs"));
        Assert.Contains("Google Password Manager", authenticatorsContent);
        Assert.Contains("Windows Hello", authenticatorsContent);

        var loginContent = File.ReadAllText(Path.Combine(accountPagesDir, "Login.razor"));
        Assert.Contains("await editContext.ValidateAsync()", loginContent);
        Assert.Contains("<DisplayName For=\"() => Input.Email\" />", loginContent);
        Assert.Contains("[Display(Name = \"Email\")]", loginContent);

        var emailSenderContent = File.ReadAllText(Path.Combine(
            _testProjectDir, "Components", "Account", "IdentityNoOpEmailSender.cs"));
        Assert.Contains("If you didn't request this email confirmation, you can ignore this email.", emailSenderContent);
        Assert.Contains("If you didn't request a password reset, you can ignore this email.", emailSenderContent);

        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("TestDbContext", programContent);
        Assert.Contains("builder.Services.AddDatabaseDeveloperPageExceptionFilter()", programContent);
        Assert.Contains("app.UseMigrationsEndPoint()", programContent);
        Assert.Contains("builder.Services.AddAuthorization()", programContent);
        Assert.DoesNotContain("IdentityRevalidatingAuthenticationStateProvider>()", programContent);
        Assert.Contains("app.MapAdditionalIdentityEndpoints();", programContent);
        Assert.DoesNotContain("app.MapAdditionalIdentityEndpoints();;", programContent);

        var navMenuContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "Layout", "NavMenu.razor"));
        Assert.Contains("href=\"auth\"", navMenuContent);
        Assert.DoesNotContain("<AntiforgeryToken />", navMenuContent);

        // Assert no NuGet errors during scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");

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

    [Fact]
    public void BlazorIdentityChangesConfig_HasRenderModeSpecificAuthenticationWiring()
    {
        var configContent = File.ReadAllText(GetBlazorIdentityChangesConfigPath());
        Assert.Contains("\"Options\": [ \"InteractiveServer\" ]", configContent);
        Assert.Contains("AddAuthenticationStateSerialization()", configContent);
        Assert.Contains("\"Options\": [ \"NonInteractiveServer\" ]", configContent);
        Assert.Contains("AcceptsInteractiveRouting()", configContent);

        var clientConfigPath = Path.Combine(
            GetActualTemplatesBasePath(),
            TargetFramework,
            "CodeModificationConfigs",
            "blazorIdentityClientChanges.json");
        var clientConfigContent = File.ReadAllText(clientConfigPath);
        Assert.Contains("AddAuthenticationStateDeserialization()", clientConfigContent);
        Assert.Contains("AddAuthorizationCore()", clientConfigContent);
    }

    [Fact]
    public async Task Scaffold_BlazorIdentity_Net11_GlobalInteractiveServerUsesStaticSsrForIdentity()
    {
        File.WriteAllText(_testProjectPath, ProjectContent.Replace(
            "</PropertyGroup>",
            "    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>\n  </PropertyGroup>"));
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), """
            using TestProject.Components;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            app.Run();
            """);
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);
        File.WriteAllText(Path.Combine(_testProjectDir, "Components", "App.razor"), """
            <!DOCTYPE html>
            <html>
            <head>
                <HeadOutlet @rendermode="InteractiveServer" />
            </head>
            <body>
                <Routes @rendermode="InteractiveServer" />
            </body>
            </html>
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease");

        Assert.True(exitCode == 0, $"CLI scaffold should succeed.\nOutput: {output}\nError: {error}");

        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains(
            "AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>()",
            programContent);
        Assert.DoesNotContain("builder.Services.AddAuthorization()", programContent);

        var appContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.Contains("<HeadOutlet @rendermode=\"PageRenderMode\" />", appContent);
        Assert.Contains("<Routes @rendermode=\"PageRenderMode\" />", appContent);
        Assert.Contains("HttpContext.AcceptsInteractiveRouting() ? InteractiveServer : null", appContent);
    }

    [Fact]
    public async Task Scaffold_BlazorIdentity_Net11_WebAssemblyClientWithWindowsProjectReferenceIsModified()
    {
        var clientProjectDir = Path.Combine(_testProjectDir, "TestProject.Client");
        var clientProjectPath = Path.Combine(clientProjectDir, "TestProject.Client.csproj");
        Directory.CreateDirectory(Path.Combine(clientProjectDir, "Layout"));

        File.WriteAllText(_testProjectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="TestProject.Client\TestProject.Client.csproj" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), """
            using TestProject.Components;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();

            var app = builder.Build();
            app.MapRazorComponents<App>()
                .AddInteractiveWebAssemblyRenderMode();
            app.Run();
            """);
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);
        File.WriteAllText(Path.Combine(_testProjectDir, "Components", "App.razor"), """
            <!DOCTYPE html>
            <html>
            <head>
                <HeadOutlet @rendermode="InteractiveWebAssembly" />
            </head>
            <body>
                <Routes @rendermode="InteractiveWebAssembly" />
            </body>
            </html>
            """);

        File.WriteAllText(clientProjectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(clientProjectDir, "Program.cs"), """
            using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            await builder.Build().RunAsync();
            """);
        File.WriteAllText(Path.Combine(clientProjectDir, "_Imports.razor"), """
            @using Microsoft.AspNetCore.Components.Forms
            """);
        File.WriteAllText(Path.Combine(clientProjectDir, "Layout", "MainLayout.razor"), """
            @inherits LayoutComponentBase

            <div class="top-row px-4">
            </div>

            @Body
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease");

        Assert.True(exitCode == 0, $"CLI scaffold should succeed.\nOutput: {output}\nError: {error}");

        var clientProgramContent = File.ReadAllText(Path.Combine(clientProjectDir, "Program.cs"));
        Assert.Contains("builder.Services.AddAuthorizationCore()", clientProgramContent);
        Assert.Contains("builder.Services.AddCascadingAuthenticationState()", clientProgramContent);
        Assert.Contains("builder.Services.AddAuthenticationStateDeserialization()", clientProgramContent);
        Assert.DoesNotContain("WebAssemblyHostBuilder.CreateDefault.Services", clientProgramContent);

        var clientProjectContent = File.ReadAllText(clientProjectPath);
        Assert.Contains("Microsoft.AspNetCore.Components.WebAssembly.Authentication", clientProjectContent);

        var appContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        Assert.Contains("<HeadOutlet @rendermode=\"PageRenderMode\" />", appContent);
        Assert.Contains("<Routes @rendermode=\"PageRenderMode\" />", appContent);
        Assert.Contains("HttpContext.AcceptsInteractiveRouting() ? InteractiveWebAssembly : null", appContent);

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
