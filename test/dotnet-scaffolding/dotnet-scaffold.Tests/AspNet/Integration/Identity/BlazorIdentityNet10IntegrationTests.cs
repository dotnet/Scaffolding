// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

public class BlazorIdentityNet10IntegrationTests : BlazorIdentityIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(BlazorIdentityNet10IntegrationTests);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Scaffold_BlazorIdentity_Net10_CliInvocation(bool usesInteractiveServer)
    {
        // Arrange write project + Program.cs + Blazor project structure
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), $$"""
            using TestProject.Components;

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddRazorComponents(){{(usesInteractiveServer ? ".AddInteractiveServerComponents()" : "")}};
            var app = builder.Build();
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }
            app.MapRazorComponents<App>(){{(usesInteractiveServer ? ".AddInteractiveServerRenderMode()" : "")}};
            app.Run();
            """);
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);
        if (usesInteractiveServer)
        {
            File.WriteAllText(Path.Combine(_testProjectDir, "Components", "App.razor"),
                ScaffoldCliHelper.GetBlazorAppRazor()
                    .Replace("<HeadOutlet />", "<HeadOutlet @rendermode=\"InteractiveServer\" />")
                    .Replace("<Routes />", "<Routes @rendermode=\"InteractiveServer\" />"));
        }

        // Assert project builds before scaffolding
        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        // Act invoke CLI: dotnet scaffold aspnet blazor-identity
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "blazor-identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // The baseline covers complete default output; these cases cover static SSR and global interactivity.
        var appContent = File.ReadAllText(Path.Combine(_testProjectDir, "Components", "App.razor"));
        if (usesInteractiveServer)
        {
            Assert.Contains("<HeadOutlet @rendermode=\"PageRenderMode\" />", appContent);
            Assert.Contains("<Routes @rendermode=\"PageRenderMode\" />", appContent);
            Assert.Contains("HttpContext.AcceptsInteractiveRouting() ? InteractiveServer : null", appContent);
        }
        else
        {
            Assert.DoesNotContain("PageRenderMode", appContent);
        }
        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("TestDbContext", programContent);
        Assert.Contains("app.MapAdditionalIdentityEndpoints()", programContent);
        Assert.Contains("if (app.Environment.IsDevelopment())\n{\n    app.UseMigrationsEndPoint();\n}", programContent.Replace("\r\n", "\n"));
        Assert.Equal(usesInteractiveServer, programContent.Contains("IdentityRevalidatingAuthenticationStateProvider"));
        Assert.Equal(usesInteractiveServer, File.Exists(Path.Combine(
            _testProjectDir, "Components", "Account", "IdentityRevalidatingAuthenticationStateProvider.cs")));
        Assert.DoesNotContain("AddAuthenticationStateSerialization()", programContent);
        Assert.Equal(!usesInteractiveServer, programContent.Contains("AddAuthorization()"));

        // Assert — no NuGet errors and project builds after scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
