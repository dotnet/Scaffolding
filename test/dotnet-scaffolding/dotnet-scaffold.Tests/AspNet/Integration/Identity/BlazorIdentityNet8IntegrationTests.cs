// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

public class BlazorIdentityNet8IntegrationTests : BlazorIdentityIntegrationTestsBase
{
    protected override string TargetFramework => "net8.0";
    protected override string TestClassName => nameof(BlazorIdentityNet8IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorIdentity_Net8_CliInvocation()
    {
        // Arrange write project + Program.cs + Blazor project structure
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);

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
        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("TestDbContext", programContent);

        // Assert — no NuGet errors and project builds after scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
