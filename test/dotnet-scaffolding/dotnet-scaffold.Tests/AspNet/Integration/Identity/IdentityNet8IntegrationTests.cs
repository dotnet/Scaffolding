// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

public class IdentityNet8IntegrationTests : IdentityIntegrationTestsBase
{
    protected override string TargetFramework => "net8.0";
    protected override string TestClassName => nameof(IdentityNet8IntegrationTests);

    [Fact]
    public async Task Scaffold_Identity_Net8_CliInvocation()
    {
        // Arrange — write project + Program.cs
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());

        // Assert — project builds before scaffolding
        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        // Act — invoke CLI: dotnet scaffold aspnet identity
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — expected files/directories were created
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "TestDbContext.cs")),
            "DbContext file should be created.");
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "ApplicationUser.cs")),
            "ApplicationUser file should be created.");
        var identityPagesDir = Path.Combine(_testProjectDir, "Areas", "Identity", "Pages");
        Assert.True(Directory.Exists(identityPagesDir), "Areas/Identity/Pages directory should be created.");
        var accountDir = Path.Combine(identityPagesDir, "Account");
        Assert.True(Directory.Exists(accountDir), "Account directory should be created.");
        Assert.True(File.Exists(Path.Combine(accountDir, "Login.cshtml")), "Login.cshtml should be created.");
        Assert.True(File.Exists(Path.Combine(accountDir, "Login.cshtml.cs")), "Login.cshtml.cs should be created.");
        Assert.True(File.Exists(Path.Combine(accountDir, "Register.cshtml")), "Register.cshtml should be created.");
        Assert.True(File.Exists(Path.Combine(accountDir, "Register.cshtml.cs")), "Register.cshtml.cs should be created.");
        Assert.True(File.Exists(Path.Combine(accountDir, "Logout.cshtml")), "Logout.cshtml should be created.");
        var manageDir = Path.Combine(accountDir, "Manage");
        Assert.True(Directory.Exists(manageDir), "Manage directory should be created.");
        Assert.True(File.Exists(Path.Combine(manageDir, "Index.cshtml")), "Manage/Index.cshtml should be created.");
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
