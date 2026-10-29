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

        // An app upgraded from .NET 10 retains its navigation markup without a nav-menu wrapper.
        var navMenuPath = Path.Combine(_testProjectDir, "Components", "Layout", "NavMenu.razor");
        File.WriteAllText(navMenuPath, """
            <div class="top-row ps-3 navbar navbar-dark">
                <a class="navbar-brand" href="">TestProject</a>
            </div>
            <div class="nav-scrollable">
                <nav class="nav flex-column">
                    <div class="nav-item px-3">
                        <NavLink class="nav-link" href="weather">Weather</NavLink>
                    </div>
                </nav>
            </div>
            """ + System.Environment.NewLine);

        // Write a NuGet.config with the dotnet11 preview feeds so the preview-only
        // framework packages can be resolved during restore/build.
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);

        // Act invoke CLI: dotnet scaffold aspnet blazor-identity
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlserver-efcore",
            "--prerelease");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");
        Assert.Contains("<PackageReference Include=\"Microsoft.Data.SqlClient.Extensions.Azure\"", File.ReadAllText(_testProjectPath));

        // Assert expected files were created
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "TestDbContext.cs")),
            "DbContext file should be created.");
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "ApplicationUser.cs")),
            "ApplicationUser file should be created.");
        var accountPagesDir = Path.Combine(_testProjectDir, "Components", "Account", "Pages");
        Assert.True(Directory.Exists(accountPagesDir), "Components/Account/Pages directory should be created.");
        var loginContent = File.ReadAllText(Path.Combine(accountPagesDir, "Login.razor"));
        Assert.Contains("await editContext.ValidateAsync()", loginContent);
        Assert.True(File.Exists(Path.Combine(accountPagesDir, "Register.razor")), "Register.razor should be created.");
        var sharedDir = Path.Combine(_testProjectDir, "Components", "Account", "Shared");
        Assert.True(Directory.Exists(sharedDir), "Components/Account/Shared directory should be created.");
        Assert.True(File.Exists(Path.Combine(sharedDir, "ManageNavMenu.razor")), "ManageNavMenu.razor should be created.");
        Assert.True(File.Exists(Path.Combine(sharedDir, "PasskeySubmit.razor.js")), "PasskeySubmit.razor.js should be created.");
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Components", "Account", "PasskeyAuthenticators.cs")),
            "PasskeyAuthenticators.cs should be created.");

        var passkeysContent = File.ReadAllText(Path.Combine(accountPagesDir, "Manage", "Passkeys.razor"));
        Assert.Contains("@PasskeyAuthenticators.GetDisplayName(passkey)", passkeysContent);
        Assert.Contains("@passkey.CreatedAt", passkeysContent);
        Assert.Contains("PasskeyAuthenticators.TryGetDefaultDisplayName(attestationResult.Passkey, out var defaultName)", passkeysContent);
        Assert.Contains("attestationResult.Passkey.Name = defaultName;", passkeysContent);
        Assert.Contains("[SupplyParameterFromTempData(Name = IdentityRedirectManager.StatusMessageKey)]", passkeysContent);
        Assert.Contains("message = IdentityStatusMessage;", passkeysContent);
        Assert.Contains("IdentityStatusMessage = \"Your passkey was added successfully.\";", passkeysContent);

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

        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("TestDbContext", programContent);
        Assert.Contains("builder.Services.AddDatabaseDeveloperPageExceptionFilter()", programContent);
        Assert.Contains("if (app.Environment.IsDevelopment())\n{\n    app.UseMigrationsEndPoint();\n}", programContent.Replace("\r\n", "\n"));
        Assert.Contains("throw new InvalidOperationException(\"Connection string", programContent);
        Assert.DoesNotContain("Data Source=TestDb.db", programContent);
        Assert.Contains("builder.Services.AddCascadingAuthenticationState()", programContent);
        Assert.Contains("builder.Services.AddAuthorization()", programContent);
        Assert.DoesNotContain("IdentityRevalidatingAuthenticationStateProvider>()", programContent);
        Assert.False(File.Exists(Path.Combine(
            _testProjectDir, "Components", "Account", "IdentityRevalidatingAuthenticationStateProvider.cs")));
        Assert.Contains("app.MapAdditionalIdentityEndpoints();", programContent);

        var navMenuContent = File.ReadAllText(navMenuPath);
        Assert.Contains("<AuthorizeView>", navMenuContent);
        Assert.Contains("href=\"Account/Register\"", navMenuContent);
        Assert.Contains("href=\"Account/Login\"", navMenuContent);
        Assert.Contains("action=\"Account/Logout\"", navMenuContent);
        Assert.Contains("href=\"weather\"", navMenuContent);
        Assert.DoesNotContain("<nav-menu>", navMenuContent);
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
            "--dbProvider", "sqlserver-efcore",
            "--prerelease",
            "--overwrite");

        Assert.True(overwriteRun.ExitCode == 0, $"Overwrite scaffold should succeed.\nOutput: {overwriteRun.Output}\nError: {overwriteRun.Error}");
        Assert.DoesNotContain("// stale", File.ReadAllText(passkeyScriptPath));
        Assert.Contains("app.MapAdditionalIdentityEndpoints();", File.ReadAllText(programPath));
        Assert.Equal(1, CountOccurrences(File.ReadAllText(navMenuPath), "<AuthorizeView>"));
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
