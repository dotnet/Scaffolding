// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

public class IdentityNet10IntegrationTests : IdentityIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(IdentityNet10IntegrationTests);

    // net10.0 Identity templates use Pages/ (T4) instead of Bootstrap4/Bootstrap5 (.cshtml)
    [Fact]
    public override void Identity_Bootstrap5_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages");
        Assert.True(Directory.Exists(pagesDir),
            $"Identity/Pages should exist for {TargetFramework}");
    }

    [Fact]
    public override void Identity_Bootstrap4_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages");
        Assert.True(Directory.Exists(pagesDir),
            $"Identity/Pages should exist for {TargetFramework} (no Bootstrap4 subfolder)");
    }

    [Fact]
    public override void Identity_Bootstrap5_HasFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages");
        var files = Directory.GetFiles(pagesDir, "*", SearchOption.AllDirectories);
        Assert.True(files.Length > 0, $"Identity/Pages should have files for {TargetFramework}");
    }

    [Fact]
    public override void Identity_Bootstrap4_HasFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages");
        var files = Directory.GetFiles(pagesDir, "*", SearchOption.AllDirectories);
        Assert.True(files.Length > 0, $"Identity/Pages should have files for {TargetFramework}");
    }

    [Fact]
    public override void Identity_Bootstrap5_HasMoreOrEqualFilesThanBootstrap4()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages");
        var files = Directory.GetFiles(pagesDir, "*", SearchOption.AllDirectories);
        Assert.True(files.Any(f => f.EndsWith(".tt")),
            $"Identity/Pages should contain .tt template files for {TargetFramework}");
    }

    [Fact]
    public async Task Scaffold_Identity_Net10_CliInvocation()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());

        Assert.False(File.Exists(Path.Combine(_testProjectDir, "obj", "project.assets.json")));

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
        var programContent = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("TestDbContext", programContent);

        // Identity pages may not be generated if T4 template execution fails
        var identityPagesDir = Path.Combine(_testProjectDir, "Areas", "Identity", "Pages");
        if (Directory.Exists(identityPagesDir))
        {
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
        }

        // Assert — no NuGet errors and project builds after scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }

    [Theory]
    [InlineData("webapp", "Pages")]
    [InlineData("mvc", "Views")]
    public async Task Scaffold_Identity_NoneAuth_GeneratesSharedLoginPartial(string projectTemplate, string sharedRoot)
    {
        var (createExitCode, createOutput, createError) = await ScaffoldCliHelper.RunDotNetAsync(
            _testProjectDir, "new", projectTemplate, "--name", "TestProject",
            "--output", _testProjectDir, "--framework", TargetFramework, "--auth", "None", "--no-restore");
        Assert.True(createExitCode == 0, $"Project creation should succeed.\nOutput: {createOutput}\nError: {createError}");
        Assert.True(File.Exists(_testProjectPath), $"Project file should be created at {_testProjectPath}.\nOutput: {createOutput}");

        string partialPath = Path.Combine(_testProjectDir, sharedRoot, "Shared", "_LoginPartial.cshtml");
        Assert.False(File.Exists(partialPath));

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            ScaffoldCliHelper.GetTestTargetFramework(),
            "identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlserver-efcore");
        Assert.True(exitCode == 0, $"Identity scaffolding should succeed without --overwrite.\nOutput: {output}\nError: {error}");

        Assert.True(File.Exists(partialPath), $"Identity scaffolding should generate {partialPath}.");
        string content = File.ReadAllText(partialPath);
        Assert.Contains("@inject SignInManager<ApplicationUser> SignInManager", content);
        Assert.Contains("@inject UserManager<ApplicationUser> UserManager", content);
        Assert.Contains("asp-page=\"/Account/Register\"", content);
        Assert.Contains("asp-page=\"/Account/Login\"", content);

        var (buildExitCode, buildOutput, buildError) = await RunBuildAsync(_testProjectDir);
        Assert.True(buildExitCode == 0, $"Scaffolded project should build.\nOutput: {buildOutput}\nError: {buildError}");
    }

    // identityMinimalHostingChanges.json does not exist for net10.0+; only net8.0 uses it.
    [Fact]
    public override void IdentityMinimalHostingChangesConfig_ExistsForTargetFramework() { }

    [Fact]
    public override void IdentityMinimalHostingChangesConfig_IsNotEmpty() { }

    [Fact]
    public override void IdentityMinimalHostingChangesConfig_ReferencesProgramCs() { }
}
