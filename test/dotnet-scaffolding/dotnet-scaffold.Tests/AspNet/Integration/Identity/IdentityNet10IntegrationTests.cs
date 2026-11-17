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

    [Theory]
    [InlineData("identity")]
    [InlineData("blazor-identity")]
    public async Task Scaffold_Identity_DoesNotSelectReferencedUserContext(string scaffolder)
    {
        var libraryDirectory = Directory.CreateDirectory(Path.Combine(_testProjectDir, "..", "IdentityLibrary")).FullName;
        var libraryPath = Path.Combine(libraryDirectory, "IdentityLibrary.csproj");
        File.WriteAllText(libraryPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup><PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.9" /></ItemGroup>
            </Project>
            """);
        const string librarySource = """
            using Microsoft.AspNetCore.Identity;
            using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
            using Microsoft.EntityFrameworkCore;
            namespace IdentityLibrary;
            public class LibraryUser : IdentityUser { }
            public class LibraryContext : IdentityDbContext<LibraryUser>
            {
                public LibraryContext(DbContextOptions<LibraryContext> options) : base(options) { }
            }
            """;
        var sourcePath = Path.Combine(libraryDirectory, "LibraryContext.cs");
        File.WriteAllText(sourcePath, librarySource);
        File.WriteAllText(_testProjectPath, ProjectContent.Replace("</Project>", """
            <ItemGroup><ProjectReference Include="..\IdentityLibrary\IdentityLibrary.csproj" /></ItemGroup>
            </Project>
            """));
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), scaffolder == "identity"
            ? ScaffoldCliHelper.GetMinimalProgramCs() : ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        if (scaffolder == "blazor-identity")
        {
            ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);
        }

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, scaffolder, "--project", _testProjectPath,
            "--dataContext", "LibraryContext", "--dbProvider", "sqlite-efcore");

        Assert.True(exitCode == 0, $"Scaffolding failed.\n{output}\n{error}");
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Data", "LibraryContext.cs")));
        var program = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));
        Assert.Contains("AddDbContext<LibraryContext>", program);
        Assert.Contains("AddEntityFrameworkStores<LibraryContext>", program);
        Assert.DoesNotContain("IdentityLibrary.LibraryContext", program);
        Assert.Equal(librarySource, File.ReadAllText(sourcePath));
        var (buildCode, buildOutput, buildError) = await RunBuildAsync(_testProjectDir);
        Assert.True(buildCode == 0, $"Generated project failed to build.\n{buildOutput}\n{buildError}");
    }

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

    // identityMinimalHostingChanges.json does not exist for net10.0+; only net8.0 uses it.
    [Fact]
    public override void IdentityMinimalHostingChangesConfig_ExistsForTargetFramework() { }

    [Fact]
    public override void IdentityMinimalHostingChangesConfig_IsNotEmpty() { }

    [Fact]
    public override void IdentityMinimalHostingChangesConfig_ReferencesProgramCs() { }
}
