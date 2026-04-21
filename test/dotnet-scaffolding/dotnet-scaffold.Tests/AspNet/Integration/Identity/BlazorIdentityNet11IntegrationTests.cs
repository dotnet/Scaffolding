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
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
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
        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("TestDbContext", programContent);

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
}
