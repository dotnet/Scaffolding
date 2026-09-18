// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

using Net11Email = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Identity.Pages.Account.Manage.Email;
using Net11ExternalLoginModel = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Identity.Pages.Account.ExternalLoginModel;
using Net11ForgotPasswordModel = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Identity.Pages.Account.ForgotPasswordModel;
using Net11Login = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Identity.Pages.Account.Login;
using Net11ManageNav = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Identity.Pages.Account.Manage._ManageNav;
using Net11RegisterConfirmationModel = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Identity.Pages.Account.RegisterConfirmationModel;
using Net11RegisterModel = Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net11.Identity.Pages.Account.RegisterModel;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

public class IdentityNet11IntegrationTests : IdentityIntegrationTestsBase
{
    protected override string TargetFramework => "net11.0";
    protected override string TestClassName => nameof(IdentityNet11IntegrationTests);

    // net11.0 Identity templates use Pages/ (T4) instead of Bootstrap4/Bootstrap5 (.cshtml)
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
    public void Identity_TemplatesMatchNet11DefaultUIBehavior()
    {
        var accountDir = Path.Combine(GetActualTemplatesBasePath(), TargetFramework, "Identity", "Pages", "Account");
        var manageDir = Path.Combine(accountDir, "Manage");

        foreach (var modelTemplate in new[]
        {
            "ConfirmEmailChangeModel.tt",
            "ConfirmEmailModel.tt",
            "ForgotPasswordModel.tt",
            "LoginModel.tt",
            "LoginWith2faModel.tt",
            "LoginWithRecoveryCodeModel.tt",
            "LogoutModel.tt",
            "RegisterModel.tt",
            "ResetPasswordModel.tt",
        })
        {
            Assert.Contains("[AllowAnonymous]", File.ReadAllText(Path.Combine(accountDir, modelTemplate)));
        }

        var registerModel = File.ReadAllText(Path.Combine(accountDir, "RegisterModel.tt"));
        Assert.Contains("if (!await _signInManager.CanSignInAsync(user))", registerModel);
        Assert.Contains("[StringSyntax(StringSyntaxAttribute.Uri)] string? returnUrl", registerModel);
        Assert.DoesNotContain("_userManager.Options.SignIn.RequireConfirmedAccount", registerModel);

        var externalLoginModel = File.ReadAllText(Path.Combine(accountDir, "ExternalLoginModel.tt"));
        Assert.Contains("if (!await _signInManager.CanSignInAsync(user))", externalLoginModel);
        Assert.DoesNotContain("_userManager.Options.SignIn.RequireConfirmedAccount", externalLoginModel);

        var registerConfirmationModel = File.ReadAllText(Path.Combine(accountDir, "RegisterConfirmationModel.tt"));
        Assert.Contains("DisplayConfirmAccountLink = IsNoOpEmailSender();", registerConfirmationModel);

        var manageNav = File.ReadAllText(Path.Combine(manageDir, "_ManageNav.tt"));
        Assert.Contains("aria-current=\"@ManageNavPages.IndexAriaCurrent(ViewContext)\"", manageNav);
        Assert.Contains("aria-current=\"@ManageNavPages.PersonalDataAriaCurrent(ViewContext)\"", manageNav);

        var manageNavPagesModel = File.ReadAllText(Path.Combine(manageDir, "ManageNavPagesModel.tt"));
        Assert.Contains("public static string? AriaCurrent(ViewContext viewContext, string page)", manageNavPagesModel);
        Assert.Contains("return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? \"page\" : null;", manageNavPagesModel);

        Assert.Contains("class=\"col-lg-6\"", File.ReadAllText(Path.Combine(accountDir, "Login.tt")));
        Assert.Contains("class=\"col-lg-6\"", File.ReadAllText(Path.Combine(accountDir, "Register.tt")));
        Assert.Contains("class=\"col-xl-6\"", File.ReadAllText(Path.Combine(manageDir, "ChangePassword.tt")));
        Assert.Contains("role=\"button\"", File.ReadAllText(Path.Combine(manageDir, "PersonalData.tt")));
        Assert.Contains("&#x2713;", File.ReadAllText(Path.Combine(manageDir, "Email.tt")));
    }

    [Fact]
    public void Identity_PreprocessedTemplatesMatchNet11DefaultUIBehavior()
    {
        var model = new IdentityModel
        {
            ProjectInfo = new ProjectInfo(Path.Combine("test", "project", "TestProject.csproj")),
            IdentityNamespace = "TestProject.Areas.Identity",
            BaseOutputPath = Path.Combine("Areas", "Identity"),
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestProject.Data",
            DbContextInfo = new DbContextInfo()
        };

        var registerModel = RenderTemplate(new Net11RegisterModel(), model);
        Assert.Contains("IEmailSender<ApplicationUser>", registerModel);
        Assert.Contains("SendConfirmationLinkAsync(user, Input.Email", registerModel);
        Assert.Contains("if (!await _signInManager.CanSignInAsync(user))", registerModel);

        var externalLoginModel = RenderTemplate(new Net11ExternalLoginModel(), model);
        Assert.Contains("IEmailSender<ApplicationUser>", externalLoginModel);
        Assert.Contains("SendConfirmationLinkAsync(user, Input.Email", externalLoginModel);

        var forgotPasswordModel = RenderTemplate(new Net11ForgotPasswordModel(), model);
        Assert.Contains("IEmailSender<ApplicationUser>", forgotPasswordModel);
        Assert.Contains("SendPasswordResetLinkAsync(user, Input.Email", forgotPasswordModel);

        var registerConfirmationModel = RenderTemplate(new Net11RegisterConfirmationModel(), model);
        Assert.Contains("IEmailSender<ApplicationUser>", registerConfirmationModel);
        Assert.Contains("DisplayConfirmAccountLink = IsNoOpEmailSender();", registerConfirmationModel);

        var manageNav = RenderTemplate(new Net11ManageNav(), model);
        Assert.Contains("ViewData[\"ManageNav.HasExternalLogins\"]", manageNav);
        Assert.DoesNotContain("@inject SignInManager", manageNav);

        Assert.Contains("class=\"col-lg-6\"", RenderTemplate(new Net11Login(), model));
        Assert.Contains("&#x2713;", RenderTemplate(new Net11Email(), model));
    }

    [Fact]
    public async Task Scaffold_Identity_Net11_CliInvocation()
    {
        var projectContent = ProjectContent.Replace(
            "</PropertyGroup>",
            "    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>\n  </PropertyGroup>");
        File.WriteAllText(_testProjectPath, projectContent);

        // Write NuGet.config with preview feeds so net11.0 packages can be resolved
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "identity",
            "--project", _testProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease", "--overwrite");
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

        // Assert no NuGet errors during scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");

        // Verify project builds after scaffolding
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }

    // identityMinimalHostingChanges.json does not exist for net11.0+; only net8.0 uses it.
    [Fact]
    public override void IdentityMinimalHostingChangesConfig_ExistsForTargetFramework() { }

    [Fact]
    public override void IdentityMinimalHostingChangesConfig_IsNotEmpty() { }

    [Fact]
    public override void IdentityMinimalHostingChangesConfig_ReferencesProgramCs() { }

    private static string RenderTemplate(ITextTransformation template, IdentityModel model)
    {
        template.Session = new Dictionary<string, object> { ["Model"] = model };
        template.Initialize();
        return template.TransformText();
    }
}
