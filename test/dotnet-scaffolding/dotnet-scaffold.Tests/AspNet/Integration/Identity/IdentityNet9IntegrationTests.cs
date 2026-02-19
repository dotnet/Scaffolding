// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

/// <summary>
/// Integration tests for the ASP.NET Core Identity (non-Blazor) scaffolder targeting .NET 9.
/// Validates scaffolder definition constants, ValidateIdentityStep validation logic,
/// IdentityModel/IdentitySettings properties, IdentityHelper template resolution,
/// template folder structure, code modification config (identityChanges.json),
/// NuGet package constants, and template file counts.
///
/// Net 9 Identity templates use .tt T4 templates (with .cs and .Interfaces.cs companions)
/// in a flat Identity/Pages structure (no Bootstrap4/Bootstrap5 subfolders, no wwwroot).
/// Total: 213 files (71 .tt), distributed as:
///  - Pages top-level: 12 files (4 .tt)
///  - Pages/Account: 102 files (34 .tt)
///  - Pages/Account/Manage: 99 files (33 .tt)
///
/// Key differences from net8:
///  - Uses identityChanges.json (not identityMinimalHostingChanges.json)
///  - .tt T4 templates with .cs and .Interfaces.cs companion files
///  - No Bootstrap4/Bootstrap5 subfolders — flat structure under Identity/Pages
///  - No wwwroot directory, no Data directory, no root scaffolding files
///  - identityChanges.json uses $(variable) placeholders
///  - Account has separate View + Model .tt pairs (e.g., Login.tt + LoginModel.tt)
/// </summary>
public class IdentityNet9IntegrationTests : IDisposable
{
    private const string TargetFramework = "net9.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly string _templatesDirectory;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public IdentityNet9IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "IdentityNet9IntegrationTests", Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        _templatesDirectory = Path.Combine(_testDirectory, "Templates");
        Directory.CreateDirectory(_testProjectDir);
        Directory.CreateDirectory(_templatesDirectory);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.Identity.DisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.Identity.Name);
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    #region Constants & Scaffolder Definition — ASP.NET Core Identity

    [Fact]
    public void ScaffolderName_IsIdentity_Net9()
    {
        Assert.Equal("identity", AspnetStrings.Identity.Name);
    }

    [Fact]
    public void ScaffolderDisplayName_IsAspNetCoreIdentity_Net9()
    {
        Assert.Equal("ASP.NET Core Identity", AspnetStrings.Identity.DisplayName);
    }

    [Fact]
    public void ScaffolderDescription_IsIdentityDescription_Net9()
    {
        Assert.Equal("Add ASP.NET Core identity to a project.", AspnetStrings.Identity.Description);
    }

    [Fact]
    public void ScaffolderCategory_IsIdentity_Net9()
    {
        Assert.Equal("Identity", AspnetStrings.Catagories.Identity);
    }

    [Fact]
    public void ScaffolderExample1_ContainsIdentityCommand_Net9()
    {
        Assert.Contains("identity", AspnetStrings.Identity.IdentityExample1);
        Assert.Contains("--project", AspnetStrings.Identity.IdentityExample1);
        Assert.Contains("--database-provider", AspnetStrings.Identity.IdentityExample1);
    }

    [Fact]
    public void ScaffolderExample2_ContainsOverwriteOption_Net9()
    {
        Assert.Contains("--overwrite", AspnetStrings.Identity.IdentityExample2);
        Assert.Contains("SQLite", AspnetStrings.Identity.IdentityExample2);
    }

    #endregion

    #region Identity Constants

    [Fact]
    public void Identity_UserClassName_IsApplicationUser()
    {
        Assert.Equal("ApplicationUser", AspNetConstants.Identity.UserClassName);
    }

    [Fact]
    public void Identity_DbContextName_IsNewIdentityDbContext()
    {
        Assert.Equal("NewIdentityDbContext", AspNetConstants.Identity.DbContextName);
    }

    #endregion

    #region IdentitySettings Validation

    [Fact]
    public void IdentitySettings_HasRequiredProperties()
    {
        var settings = new IdentitySettings
        {
            Project = _testProjectPath,
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            DataContext = "TestDbContext",
            Prerelease = false,
            Overwrite = false,
            BlazorScenario = false
        };

        Assert.Equal(_testProjectPath, settings.Project);
        Assert.Equal(PackageConstants.EfConstants.SqlServer, settings.DatabaseProvider);
        Assert.Equal("TestDbContext", settings.DataContext);
        Assert.False(settings.Prerelease);
        Assert.False(settings.Overwrite);
        Assert.False(settings.BlazorScenario);
    }

    [Fact]
    public void IdentitySettings_SupportsOverwrite()
    {
        var settings = new IdentitySettings
        {
            Project = _testProjectPath,
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            DataContext = "TestDbContext",
            Overwrite = true,
            BlazorScenario = false
        };

        Assert.True(settings.Overwrite);
    }

    [Fact]
    public void IdentitySettings_BlazorScenario_FalseForNonBlazorIdentity()
    {
        var settings = new IdentitySettings
        {
            Project = _testProjectPath,
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            DataContext = "TestDbContext",
            BlazorScenario = false
        };

        Assert.False(settings.BlazorScenario);
    }

    #endregion

    #region IdentityModel Properties

    [Fact]
    public void IdentityModel_NonBlazor_UsesAreasIdentityNamespace()
    {
        var model = CreateTestIdentityModel();
        Assert.Equal("TestProject.Areas.Identity", model.IdentityNamespace);
    }

    [Fact]
    public void IdentityModel_NonBlazor_HasEmptyIdentityLayoutNamespace()
    {
        var model = CreateTestIdentityModel();
        Assert.True(string.IsNullOrEmpty(model.IdentityLayoutNamespace));
    }

    [Fact]
    public void IdentityModel_HasCorrectUserClassName()
    {
        var model = CreateTestIdentityModel();
        Assert.Equal(AspNetConstants.Identity.UserClassName, model.UserClassName);
    }

    [Fact]
    public void IdentityModel_HasCorrectUserClassNamespace()
    {
        var model = CreateTestIdentityModel();
        Assert.Equal("TestProject.Data", model.UserClassNamespace);
    }

    [Fact]
    public void IdentityModel_HasBaseOutputPath()
    {
        var model = CreateTestIdentityModel();
        Assert.NotNull(model.BaseOutputPath);
        Assert.NotEmpty(model.BaseOutputPath);
    }

    [Fact]
    public void IdentityModel_OverwriteDefaultsFalse()
    {
        var model = CreateTestIdentityModel();
        Assert.False(model.Overwrite);
    }

    #endregion

    #region ValidateIdentityStep — Validation Logic

    [Fact]
    public async Task ValidateIdentityStep_FailsWithNullProject()
    {
        var step = new ValidateIdentityStep(
            _mockFileSystem.Object,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);

        step.Project = null;
        step.DataContext = "TestDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateIdentityStep_FailsWithEmptyProject()
    {
        var step = new ValidateIdentityStep(
            _mockFileSystem.Object,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);

        step.Project = string.Empty;
        step.DataContext = "TestDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateIdentityStep_FailsWithNonExistentProject()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateIdentityStep(
            _mockFileSystem.Object,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);

        step.Project = @"C:\NonExistent\Project.csproj";
        step.DataContext = "TestDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateIdentityStep_FailsWithNullDataContext()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);

        var step = new ValidateIdentityStep(
            _mockFileSystem.Object,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);

        step.Project = _testProjectPath;
        step.DataContext = null;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateIdentityStep_FailsWithEmptyDataContext()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);

        var step = new ValidateIdentityStep(
            _mockFileSystem.Object,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);

        step.Project = _testProjectPath;
        step.DataContext = string.Empty;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public void ValidateIdentityStep_HasOverwriteProperty()
    {
        var step = new ValidateIdentityStep(
            _mockFileSystem.Object,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);

        step.Overwrite = true;
        Assert.True(step.Overwrite);
    }

    [Fact]
    public void ValidateIdentityStep_HasBlazorScenarioProperty()
    {
        var step = new ValidateIdentityStep(
            _mockFileSystem.Object,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);

        step.BlazorScenario = false;
        Assert.False(step.BlazorScenario);
    }

    [Fact]
    public void ValidateIdentityStep_HasPrereleaseProperty()
    {
        var step = new ValidateIdentityStep(
            _mockFileSystem.Object,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);

        step.Prerelease = true;
        Assert.True(step.Prerelease);
    }

    #endregion

    #region Package Constants — Identity Packages

    [Fact]
    public void IdentityEfPackage_IsCorrectPackageName()
    {
        Assert.Equal("Microsoft.AspNetCore.Identity.EntityFrameworkCore", PackageConstants.AspNetCorePackages.AspNetCoreIdentityEfPackage.Name);
    }

    [Fact]
    public void IdentityUiPackage_IsCorrectPackageName()
    {
        Assert.Equal("Microsoft.AspNetCore.Identity.UI", PackageConstants.AspNetCorePackages.AspNetCoreIdentityUiPackage.Name);
    }

    [Fact]
    public void IdentityEfPackagesDict_ContainsSqlServer()
    {
        Assert.True(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(PackageConstants.EfConstants.SqlServer));
    }

    [Fact]
    public void IdentityEfPackagesDict_ContainsSqlite()
    {
        Assert.True(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(PackageConstants.EfConstants.SQLite));
    }

    [Fact]
    public void IdentityEfPackagesDict_DoesNotContainCosmos()
    {
        Assert.False(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(PackageConstants.EfConstants.CosmosDb));
    }

    [Fact]
    public void IdentityEfPackagesDict_DoesNotContainPostgres()
    {
        Assert.False(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(PackageConstants.EfConstants.Postgres));
    }

    [Fact]
    public void IdentityEfPackagesDict_HasExactlyTwoProviders()
    {
        Assert.Equal(2, PackageConstants.EfConstants.IdentityEfPackagesDict.Count);
    }

    [Fact]
    public void EfCoreToolsPackage_IsCorrectPackageName()
    {
        Assert.Equal("Microsoft.EntityFrameworkCore.Tools", PackageConstants.EfConstants.EfCoreToolsPackage.Name);
    }

    #endregion

    #region Template Folder Structure — Net9 Identity (Flat, No Bootstrap)

    [Fact]
    public void Net9_Identity_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity");
        Assert.True(Directory.Exists(identityDir), $"Identity folder not found at: {identityDir}");
    }

    [Fact]
    public void Net9_Identity_HasNoBootstrapSubfolders()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity");
        if (!Directory.Exists(identityDir)) return;

        Assert.False(Directory.Exists(Path.Combine(identityDir, "Bootstrap4")),
            "Net9 Identity should NOT have a Bootstrap4 subfolder");
        Assert.False(Directory.Exists(Path.Combine(identityDir, "Bootstrap5")),
            "Net9 Identity should NOT have a Bootstrap5 subfolder");
    }

    [Fact]
    public void Net9_Identity_HasNoWwwrootFolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var wwwrootDir = Path.Combine(basePath, TargetFramework, "Identity", "wwwroot");
        Assert.False(Directory.Exists(wwwrootDir),
            "Net9 Identity should NOT have a wwwroot subfolder");
    }

    [Fact]
    public void Net9_Identity_HasNoDataFolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var dataDir = Path.Combine(basePath, TargetFramework, "Identity", "Data");
        Assert.False(Directory.Exists(dataDir),
            "Net9 Identity should NOT have a Data subfolder");
    }

    [Fact]
    public void Net9_Identity_HasPagesFolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages");
        Assert.True(Directory.Exists(pagesDir), $"Identity/Pages not found at: {pagesDir}");
    }

    [Fact]
    public void Net9_Identity_HasExpectedTotalFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity");
        if (!Directory.Exists(identityDir)) return;

        var allFiles = Directory.EnumerateFiles(identityDir, "*", SearchOption.AllDirectories).ToList();
        Assert.Equal(213, allFiles.Count);
    }

    [Fact]
    public void Net9_Identity_HasExpectedTotalTtFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity");
        if (!Directory.Exists(identityDir)) return;

        var ttFiles = Directory.EnumerateFiles(identityDir, "*.tt", SearchOption.AllDirectories).ToList();
        Assert.Equal(71, ttFiles.Count);
    }

    [Fact]
    public void Net9_Identity_Pages_HasExpectedTopLevelFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages");
        if (!Directory.Exists(pagesDir)) return;

        var topFiles = Directory.EnumerateFiles(pagesDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(12, topFiles.Count);
    }

    [Fact]
    public void Net9_Identity_Pages_HasExpectedTopLevelTtCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages");
        if (!Directory.Exists(pagesDir)) return;

        var ttFiles = Directory.EnumerateFiles(pagesDir, "*.tt", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(4, ttFiles.Count);
    }

    [Fact]
    public void Net9_Identity_Account_HasExpectedFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var accountDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages", "Account");
        if (!Directory.Exists(accountDir)) return;

        var accountFiles = Directory.EnumerateFiles(accountDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(102, accountFiles.Count);
    }

    [Fact]
    public void Net9_Identity_Account_HasExpectedTtCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var accountDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages", "Account");
        if (!Directory.Exists(accountDir)) return;

        var ttFiles = Directory.EnumerateFiles(accountDir, "*.tt", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(34, ttFiles.Count);
    }

    [Fact]
    public void Net9_Identity_Manage_HasExpectedFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var manageDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages", "Account", "Manage");
        if (!Directory.Exists(manageDir)) return;

        var manageFiles = Directory.EnumerateFiles(manageDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(99, manageFiles.Count);
    }

    [Fact]
    public void Net9_Identity_Manage_HasExpectedTtCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var manageDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages", "Account", "Manage");
        if (!Directory.Exists(manageDir)) return;

        var ttFiles = Directory.EnumerateFiles(manageDir, "*.tt", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(33, ttFiles.Count);
    }

    #endregion

    #region Template Existence — Pages Top-Level .tt Files

    [Theory]
    [InlineData("Error.tt")]
    [InlineData("ErrorModel.tt")]
    [InlineData("_ViewImports.tt")]
    [InlineData("_ViewStart.tt")]
    public void Net9_Identity_Pages_TtFiles_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Pages", fileName));
    }

    #endregion

    #region Template Existence — Pages Top-Level .cs Companion Files

    [Theory]
    [InlineData("Error.cs")]
    [InlineData("Error.Interfaces.cs")]
    [InlineData("ErrorModel.cs")]
    [InlineData("ErrorModel.Interfaces.cs")]
    [InlineData("_ViewImports.cs")]
    [InlineData("_ViewImports.Interfaces.cs")]
    [InlineData("_ViewStart.cs")]
    [InlineData("_ViewStart.Interfaces.cs")]
    public void Net9_Identity_Pages_CompanionFiles_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Pages", fileName));
    }

    #endregion

    #region Template Existence — Account .tt Files

    [Theory]
    [InlineData("AccessDenied.tt")]
    [InlineData("AccessDeniedModel.tt")]
    [InlineData("ConfirmEmail.tt")]
    [InlineData("ConfirmEmailModel.tt")]
    [InlineData("ConfirmEmailChange.tt")]
    [InlineData("ConfirmEmailChangeModel.tt")]
    [InlineData("ExternalLogin.tt")]
    [InlineData("ExternalLoginModel.tt")]
    [InlineData("ForgotPassword.tt")]
    [InlineData("ForgotPasswordModel.tt")]
    [InlineData("ForgotPasswordConfirmation.tt")]
    [InlineData("ForgotPasswordConfirmationModel.tt")]
    [InlineData("Lockout.tt")]
    [InlineData("LockoutModel.tt")]
    [InlineData("Login.tt")]
    [InlineData("LoginModel.tt")]
    [InlineData("LoginWith2fa.tt")]
    [InlineData("LoginWith2faModel.tt")]
    [InlineData("LoginWithRecoveryCode.tt")]
    [InlineData("LoginWithRecoveryCodeModel.tt")]
    [InlineData("Logout.tt")]
    [InlineData("LogoutModel.tt")]
    [InlineData("Register.tt")]
    [InlineData("RegisterModel.tt")]
    [InlineData("RegisterConfirmation.tt")]
    [InlineData("RegisterConfirmationModel.tt")]
    [InlineData("ResendEmailConfirmation.tt")]
    [InlineData("ResendEmailConfirmationModel.tt")]
    [InlineData("ResetPassword.tt")]
    [InlineData("ResetPasswordModel.tt")]
    [InlineData("ResetPasswordConfirmation.tt")]
    [InlineData("ResetPasswordConfirmationModel.tt")]
    [InlineData("_StatusMessage.tt")]
    [InlineData("_ViewImports.tt")]
    public void Net9_Identity_Account_TtFiles_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Pages", "Account", fileName));
    }

    #endregion

    #region Template Existence — Manage .tt Files

    [Theory]
    [InlineData("ChangePassword.tt")]
    [InlineData("ChangePasswordModel.tt")]
    [InlineData("DeletePersonalData.tt")]
    [InlineData("DeletePersonalDataModel.tt")]
    [InlineData("Disable2fa.tt")]
    [InlineData("Disable2faModel.tt")]
    [InlineData("DownloadPersonalData.tt")]
    [InlineData("DownloadPersonalDataModel.tt")]
    [InlineData("Email.tt")]
    [InlineData("EmailModel.tt")]
    [InlineData("EnableAuthenticator.tt")]
    [InlineData("EnableAuthenticatorModel.tt")]
    [InlineData("ExternalLogins.tt")]
    [InlineData("ExternalLoginsModel.tt")]
    [InlineData("GenerateRecoveryCodes.tt")]
    [InlineData("GenerateRecoveryCodesModel.tt")]
    [InlineData("Index.tt")]
    [InlineData("IndexModel.tt")]
    [InlineData("ManageNavPagesModel.tt")]
    [InlineData("PersonalData.tt")]
    [InlineData("PersonalDataModel.tt")]
    [InlineData("ResetAuthenticator.tt")]
    [InlineData("ResetAuthenticatorModel.tt")]
    [InlineData("SetPassword.tt")]
    [InlineData("SetPasswordModel.tt")]
    [InlineData("ShowRecoveryCodes.tt")]
    [InlineData("ShowRecoveryCodesModel.tt")]
    [InlineData("TwoFactorAuthentication.tt")]
    [InlineData("TwoFactorAuthenticationModel.tt")]
    [InlineData("_Layout.tt")]
    [InlineData("_ManageNav.tt")]
    [InlineData("_StatusMessage.tt")]
    [InlineData("_ViewImports.tt")]
    public void Net9_Identity_Manage_TtFiles_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Pages", "Account", "Manage", fileName));
    }

    #endregion

    #region Template Existence — Files Directory (Shared Files)

    [Theory]
    [InlineData("ApplicationUser.tt")]
    [InlineData("ApplicationUser.cs")]
    [InlineData("ApplicationUser.Interfaces.cs")]
    [InlineData("_ValidationScriptsPartial.cshtml")]
    public void Net9_Files_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Files", fileName));
    }

    [Fact]
    public void Net9_Files_HasExpectedFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var filesDir = Path.Combine(basePath, TargetFramework, "Files");
        if (!Directory.Exists(filesDir)) return;

        var files = Directory.EnumerateFiles(filesDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(4, files.Count);
    }

    #endregion

    #region Net9 Account Pages — View + Model Pairs

    [Fact]
    public void Net9_Identity_Account_HasAllExpectedViewModelPairs()
    {
        var basePath = GetActualTemplatesBasePath();
        var accountDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages", "Account");
        if (!Directory.Exists(accountDir)) return;

        var ttFiles = Directory.EnumerateFiles(accountDir, "*.tt", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        // Each page should have a View .tt and a Model .tt
        var expectedPages = new[]
        {
            "AccessDenied", "ConfirmEmail", "ConfirmEmailChange", "ExternalLogin",
            "ForgotPassword", "ForgotPasswordConfirmation", "Lockout", "Login",
            "LoginWith2fa", "LoginWithRecoveryCode", "Logout", "Register",
            "RegisterConfirmation", "ResendEmailConfirmation", "ResetPassword",
            "ResetPasswordConfirmation"
        };

        foreach (var page in expectedPages)
        {
            Assert.Contains(page, ttFiles);
            Assert.Contains($"{page}Model", ttFiles);
        }

        // Plus non-paired .tt files
        Assert.Contains("_StatusMessage", ttFiles);
        Assert.Contains("_ViewImports", ttFiles);
    }

    [Fact]
    public void Net9_Identity_Manage_HasAllExpectedViewModelPairs()
    {
        var basePath = GetActualTemplatesBasePath();
        var manageDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages", "Account", "Manage");
        if (!Directory.Exists(manageDir)) return;

        var ttFiles = Directory.EnumerateFiles(manageDir, "*.tt", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        // Each manage page should have a View .tt and a Model .tt
        var expectedPages = new[]
        {
            "ChangePassword", "DeletePersonalData", "Disable2fa", "DownloadPersonalData",
            "Email", "EnableAuthenticator", "ExternalLogins", "GenerateRecoveryCodes",
            "Index", "PersonalData", "ResetAuthenticator", "SetPassword",
            "ShowRecoveryCodes", "TwoFactorAuthentication"
        };

        foreach (var page in expectedPages)
        {
            Assert.Contains(page, ttFiles);
            Assert.Contains($"{page}Model", ttFiles);
        }

        // Non-paired .tt files
        Assert.Contains("ManageNavPagesModel", ttFiles);
        Assert.Contains("_Layout", ttFiles);
        Assert.Contains("_ManageNav", ttFiles);
        Assert.Contains("_StatusMessage", ttFiles);
        Assert.Contains("_ViewImports", ttFiles);
    }

    #endregion

    #region Net9 T4 Template Companion Files

    [Fact]
    public void Net9_Identity_EachAccountTt_HasCsAndInterfacesCompanion()
    {
        var basePath = GetActualTemplatesBasePath();
        var accountDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages", "Account");
        if (!Directory.Exists(accountDir)) return;

        var ttFiles = Directory.EnumerateFiles(accountDir, "*.tt", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        foreach (var ttName in ttFiles)
        {
            var csFile = Path.Combine(accountDir, $"{ttName}.cs");
            var interfacesFile = Path.Combine(accountDir, $"{ttName}.Interfaces.cs");

            Assert.True(File.Exists(csFile), $"Missing .cs companion for {ttName}.tt: {csFile}");
            Assert.True(File.Exists(interfacesFile), $"Missing .Interfaces.cs companion for {ttName}.tt: {interfacesFile}");
        }
    }

    [Fact]
    public void Net9_Identity_EachManageTt_HasCsAndInterfacesCompanion()
    {
        var basePath = GetActualTemplatesBasePath();
        var manageDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages", "Account", "Manage");
        if (!Directory.Exists(manageDir)) return;

        var ttFiles = Directory.EnumerateFiles(manageDir, "*.tt", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        foreach (var ttName in ttFiles)
        {
            var csFile = Path.Combine(manageDir, $"{ttName}.cs");
            var interfacesFile = Path.Combine(manageDir, $"{ttName}.Interfaces.cs");

            Assert.True(File.Exists(csFile), $"Missing .cs companion for {ttName}.tt: {csFile}");
            Assert.True(File.Exists(interfacesFile), $"Missing .Interfaces.cs companion for {ttName}.tt: {interfacesFile}");
        }
    }

    [Fact]
    public void Net9_Identity_EachPagesTt_HasCsAndInterfacesCompanion()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "Identity", "Pages");
        if (!Directory.Exists(pagesDir)) return;

        var ttFiles = Directory.EnumerateFiles(pagesDir, "*.tt", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        foreach (var ttName in ttFiles)
        {
            var csFile = Path.Combine(pagesDir, $"{ttName}.cs");
            var interfacesFile = Path.Combine(pagesDir, $"{ttName}.Interfaces.cs");

            Assert.True(File.Exists(csFile), $"Missing .cs companion for {ttName}.tt: {csFile}");
            Assert.True(File.Exists(interfacesFile), $"Missing .Interfaces.cs companion for {ttName}.tt: {interfacesFile}");
        }
    }

    #endregion

    #region Code Modification Config — identityChanges.json

    [Fact]
    public void IdentityChangesConfig_ExistsForNet9()
    {
        var configPath = GetIdentityChangesConfigPath();
        Assert.True(File.Exists(configPath), $"identityChanges.json not found at: {configPath}");
    }

    [Fact]
    public void IdentityChangesConfig_IsValidJson()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        var ex = Record.Exception(() => JsonDocument.Parse(configContent));
        Assert.Null(ex);
    }

    [Fact]
    public void IdentityChangesConfig_ContainsFilesArray()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        Assert.True(configJson.RootElement.TryGetProperty("Files", out var files));
        Assert.True(files.GetArrayLength() > 0);
    }

    [Fact]
    public void IdentityChangesConfig_ReferencesProgramCs()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        var files = configJson.RootElement.GetProperty("Files");

        bool found = false;
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("FileName", out var fileName) &&
                fileName.GetString()?.Equals("Program.cs", StringComparison.OrdinalIgnoreCase) == true)
            {
                found = true;
                break;
            }
        }

        Assert.True(found, "Program.cs not found in identityChanges.json Files array");
    }

    [Fact]
    public void IdentityChangesConfig_HasGetConnectionStringChange()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("GetConnectionString", configContent);
    }

    [Fact]
    public void IdentityChangesConfig_HasAddDbContextChange()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("AddDbContext", configContent);
    }

    [Fact]
    public void IdentityChangesConfig_HasAddDefaultIdentityChange()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("AddDefaultIdentity", configContent);
    }

    [Fact]
    public void IdentityChangesConfig_HasAddEntityFrameworkStoresChange()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("AddEntityFrameworkStores", configContent);
    }

    [Fact]
    public void IdentityChangesConfig_HasRequiredUsings()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        var files = configJson.RootElement.GetProperty("Files");

        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("Usings", out var usings))
            {
                var usingsList = new List<string>();
                foreach (var u in usings.EnumerateArray())
                {
                    usingsList.Add(u.GetString()!);
                }

                Assert.Contains("Microsoft.AspNetCore.Identity", usingsList);
                Assert.Contains("Microsoft.EntityFrameworkCore", usingsList);
            }
        }
    }

    [Fact]
    public void IdentityChangesConfig_HasGlobalMethodSection()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        var files = configJson.RootElement.GetProperty("Files");

        bool found = false;
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("Methods", out var methods) &&
                methods.TryGetProperty("Global", out _))
            {
                found = true;
                break;
            }
        }

        Assert.True(found, "Global method section not found in identityChanges.json");
    }

    [Fact]
    public void IdentityChangesConfig_RequireConfirmedAccountIsTrue()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("RequireConfirmedAccount = true", configContent);
    }

    #endregion

    #region identityChanges.json — $(variable) Placeholders

    [Fact]
    public void IdentityChangesConfig_HasConnectionStringNamePlaceholder()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("$(ConnectionStringName)", configContent);
    }

    [Fact]
    public void IdentityChangesConfig_HasDbContextNamePlaceholder()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("$(DbContextName)", configContent);
    }

    [Fact]
    public void IdentityChangesConfig_HasUseDbMethodPlaceholder()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("$(UseDbMethod)", configContent);
    }

    [Fact]
    public void IdentityChangesConfig_HasUserClassNamePlaceholder()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("$(UserClassName)", configContent);
    }

    [Fact]
    public void IdentityChangesConfig_HasUserClassNamespacePlaceholder()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("$(UserClassNamespace)", configContent);
    }

    [Fact]
    public void IdentityChangesConfig_HasEfScenarioOption()
    {
        var configPath = GetIdentityChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("EfScenario", configContent);
    }

    #endregion

    #region Net9 vs Net8 — Config Differences

    [Fact]
    public void Net9_Identity_UsesIdentityChanges_NotMinimalHostingChanges()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityChangesPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "identityChanges.json");
        var minimalHostingPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "identityMinimalHostingChanges.json");

        Assert.True(File.Exists(identityChangesPath), "identityChanges.json should exist for net9.0");
        Assert.False(File.Exists(minimalHostingPath), "identityMinimalHostingChanges.json should NOT exist for net9.0 (exists in net8)");
    }

    [Fact]
    public void Net9_Identity_HasDifferentConfigNameFromBlazorIdentity()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityConfigPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "identityChanges.json");
        var blazorConfigPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "blazorIdentityChanges.json");

        Assert.True(File.Exists(identityConfigPath));
        Assert.True(File.Exists(blazorConfigPath));

        // They should be different files with different content
        var identityContent = File.ReadAllText(identityConfigPath);
        var blazorContent = File.ReadAllText(blazorConfigPath);
        Assert.NotEqual(identityContent, blazorContent);
    }

    [Fact]
    public void Net9_Identity_UsesT4Templates_NotCshtmlTemplates()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity");
        if (!Directory.Exists(identityDir)) return;

        var ttFiles = Directory.EnumerateFiles(identityDir, "*.tt", SearchOption.AllDirectories).ToList();
        var cshtmlFiles = Directory.EnumerateFiles(identityDir, "*.cshtml", SearchOption.AllDirectories).ToList();

        Assert.NotEmpty(ttFiles);
        Assert.Empty(cshtmlFiles);
    }

    #endregion

    #region Template File Discovery with Testable Utilities

    [Fact]
    public void GetAllT4TemplatesForTargetFramework_FindsTtFiles_ForIdentity()
    {
        // Arrange: Net9 Identity uses .tt templates
        var utilities = CreateTestableUtilities();
        CreateIdentityTemplateFolder(
            "Login.tt",
            "Login.cs",
            "Login.Interfaces.cs",
            "LoginModel.tt",
            "LoginModel.cs",
            "LoginModel.Interfaces.cs");

        // Act
        var ttFiles = utilities.GetAllT4TemplatesForTargetFramework(["Identity"], null).ToList();

        // Assert — Only .tt files should be returned
        Assert.Equal(2, ttFiles.Count);
        Assert.All(ttFiles, f => Assert.EndsWith(".tt", f));
    }

    [Fact]
    public void GetAllFilesForTargetFramework_FindsAllIdentityFiles()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateIdentityTemplateFolder(
            "Login.tt",
            "Login.cs",
            "Login.Interfaces.cs",
            "LoginModel.tt",
            "LoginModel.cs",
            "LoginModel.Interfaces.cs");

        // Act
        var allFiles = utilities.GetAllFilesForTargetFramework(["Identity"], null).ToList();

        // Assert
        Assert.Equal(6, allFiles.Count);
    }

    [Fact]
    public void GetAllFilesForTargetFramework_FindsNestedAccountFiles()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        var identityDir = Path.Combine(_templatesDirectory, TargetFramework, "Identity");
        var accountDir = Path.Combine(identityDir, "Pages", "Account");
        Directory.CreateDirectory(accountDir);
        File.WriteAllText(Path.Combine(accountDir, "Login.tt"), "// login");
        File.WriteAllText(Path.Combine(accountDir, "Login.cs"), "// login cs");
        File.WriteAllText(Path.Combine(accountDir, "Login.Interfaces.cs"), "// login interfaces");
        File.WriteAllText(Path.Combine(accountDir, "LoginModel.tt"), "// login model");

        // Act — get files from the Identity subtree
        var allFiles = utilities.GetAllFilesForTargetFramework(["Identity"], null).ToList();

        // Assert
        Assert.True(allFiles.Count >= 4);
        Assert.Contains(allFiles, f => f.Contains("Login.tt"));
        Assert.Contains(allFiles, f => f.Contains("LoginModel.tt"));
    }

    [Fact]
    public void GetAllT4TemplatesForTargetFramework_ReturnsOnlyTtFiles()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateIdentityTemplateFolder(
            "Error.tt",
            "Error.cs",
            "Error.Interfaces.cs",
            "ErrorModel.tt",
            "ErrorModel.cs",
            "ErrorModel.Interfaces.cs",
            "_ViewImports.tt",
            "_ViewImports.cs",
            "_ViewImports.Interfaces.cs");

        // Act
        var ttFiles = utilities.GetAllT4TemplatesForTargetFramework(["Identity"], null).ToList();

        // Assert — only .tt files
        Assert.Equal(3, ttFiles.Count);
        Assert.All(ttFiles, f => Assert.EndsWith(".tt", f));
        Assert.DoesNotContain(ttFiles, f => f.EndsWith(".cs"));
    }

    #endregion

    #region Telemetry Tracking

    [Fact]
    public async Task ValidateIdentityStep_TracksTelemetry_OnFailure()
    {
        var step = new ValidateIdentityStep(
            _mockFileSystem.Object,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);

        step.Project = null;
        step.DataContext = "TestDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        await step.ExecuteAsync(_context);

        Assert.NotEmpty(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region IdentityHelper — GetTextTemplatingProperties

    [Fact]
    public void IdentityHelper_GetTextTemplatingProperties_ReturnsEmpty_WhenProjectInfoNull()
    {
        var identityModel = new IdentityModel
        {
            ProjectInfo = new ProjectInfo(string.Empty),
            IdentityNamespace = "TestProject.Areas.Identity",
            BaseOutputPath = _testProjectDir,
            UserClassName = AspNetConstants.Identity.UserClassName,
            UserClassNamespace = "TestProject.Data",
            DbContextInfo = new DbContextInfo()
        };

        var result = IdentityHelper.GetTextTemplatingProperties([], identityModel);
        Assert.Empty(result);
    }

    [Fact]
    public void IdentityHelper_GetTextTemplatingProperties_ReturnsEmpty_WhenNoFilePaths()
    {
        var identityModel = CreateTestIdentityModel();
        var result = IdentityHelper.GetTextTemplatingProperties([], identityModel);
        Assert.Empty(result);
    }

    [Fact]
    public void IdentityHelper_GetApplicationUserTextTemplatingProperty_ReturnsNull_WhenTemplateNull()
    {
        var identityModel = CreateTestIdentityModel();
        var result = IdentityHelper.GetApplicationUserTextTemplatingProperty(null, identityModel);
        Assert.Null(result);
    }

    [Fact]
    public void IdentityHelper_GetApplicationUserTextTemplatingProperty_ReturnsNull_WhenTemplateEmpty()
    {
        var identityModel = CreateTestIdentityModel();
        var result = IdentityHelper.GetApplicationUserTextTemplatingProperty(string.Empty, identityModel);
        Assert.Null(result);
    }

    [Fact]
    public void IdentityHelper_GetApplicationUserTextTemplatingProperty_ReturnsCorrectOutputPath()
    {
        var identityModel = CreateTestIdentityModel();
        var templatePath = Path.Combine(_testProjectDir, "Templates", "ApplicationUser.tt");

        var result = IdentityHelper.GetApplicationUserTextTemplatingProperty(templatePath, identityModel);

        Assert.NotNull(result);
        Assert.EndsWith(".cs", result.OutputPath);
        Assert.Contains("Data", result.OutputPath);
        Assert.Contains(AspNetConstants.Identity.UserClassName, result.OutputPath);
    }

    #endregion

    #region Database Provider Validation

    [Theory]
    [InlineData("sqlserver-efcore")]
    [InlineData("sqlite-efcore")]
    public void IdentityEfPackagesDict_SupportsProvider(string provider)
    {
        Assert.True(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(provider));
    }

    [Theory]
    [InlineData("cosmos-efcore")]
    [InlineData("npgsql-efcore")]
    [InlineData("mysql")]
    [InlineData("")]
    public void IdentityEfPackagesDict_DoesNotSupportProvider(string provider)
    {
        Assert.False(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(provider));
    }

    #endregion

    #region CLI Option Constants

    [Fact]
    public void CliOptions_ProjectOption_IsCorrect()
    {
        Assert.Equal("--project", AspNetConstants.CliOptions.ProjectCliOption);
    }

    [Fact]
    public void CliOptions_DataContextOption_IsCorrect()
    {
        Assert.Equal("--dataContext", AspNetConstants.CliOptions.DataContextOption);
    }

    [Fact]
    public void CliOptions_DbProviderOption_IsCorrect()
    {
        Assert.Equal("--dbProvider", AspNetConstants.CliOptions.DbProviderOption);
    }

    [Fact]
    public void CliOptions_OverwriteOption_IsCorrect()
    {
        Assert.Equal("--overwrite", AspNetConstants.CliOptions.OverwriteOption);
    }

    [Fact]
    public void CliOptions_PrereleaseOption_IsCorrect()
    {
        Assert.Equal("--prerelease", AspNetConstants.CliOptions.PrereleaseCliOption);
    }

    #endregion

    #region File Extension Constants

    [Fact]
    public void CSharpExtension_IsCs()
    {
        Assert.Equal(".cs", AspNetConstants.CSharpExtension);
    }

    [Fact]
    public void ViewExtension_IsCshtml()
    {
        Assert.Equal(".cshtml", AspNetConstants.ViewExtension);
    }

    [Fact]
    public void ViewModelExtension_IsCshtmlCs()
    {
        Assert.Equal(".cshtml.cs", AspNetConstants.ViewModelExtension);
    }

    [Fact]
    public void T4TemplateExtension_IsTt()
    {
        Assert.Equal(".tt", AspNetConstants.T4TemplateExtension);
    }

    #endregion

    #region Helper Methods

    private TemplateFoldersUtilitiesTestable CreateTestableUtilities()
    {
        return new TemplateFoldersUtilitiesTestable(_testDirectory, TargetFramework);
    }

    private void CreateIdentityTemplateFolder(params string[] fileNames)
    {
        var identityFolder = Path.Combine(_templatesDirectory, TargetFramework, "Identity");
        Directory.CreateDirectory(identityFolder);
        foreach (var fileName in fileNames)
        {
            File.WriteAllText(Path.Combine(identityFolder, fileName), $"// {fileName} content");
        }
    }

    private static string GetActualTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

    private static string GetIdentityChangesConfigPath()
    {
        var basePath = GetActualTemplatesBasePath();
        return Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "identityChanges.json");
    }

    private static void AssertActualTemplateFileExists(string relativePath)
    {
        var basePath = GetActualTemplatesBasePath();
        var normalizedPath = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(basePath, normalizedPath);
        Assert.True(File.Exists(fullPath), $"Template file not found: {relativePath}\nFull path: {fullPath}");
    }

    private static IdentityModel CreateTestIdentityModel()
    {
        return new IdentityModel
        {
            ProjectInfo = new ProjectInfo(Path.Combine("test", "project", "TestProject.csproj")),
            IdentityNamespace = "TestProject.Areas.Identity",
            IdentityLayoutNamespace = string.Empty,
            BaseOutputPath = Path.Combine("test", "project"),
            UserClassName = AspNetConstants.Identity.UserClassName,
            UserClassNamespace = "TestProject.Data",
            DbContextInfo = new DbContextInfo()
        };
    }

    private class TestTelemetryService : ITelemetryService
    {
        public List<(string EventName, IReadOnlyDictionary<string, string> Properties, IReadOnlyDictionary<string, double> Measurements)> TrackedEvents { get; } = new();

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties, IReadOnlyDictionary<string, double> measurements)
        {
            TrackedEvents.Add((eventName, properties, measurements));
        }

        public void Flush()
        {
        }
    }

    /// <summary>
    /// Testable wrapper for TemplateFoldersUtilities that uses a custom base path and target framework.
    /// </summary>
    private class TemplateFoldersUtilitiesTestable : TemplateFoldersUtilities
    {
        private readonly string _basePath;
        private readonly string _targetFramework;

        public TemplateFoldersUtilitiesTestable(string basePath, string targetFramework)
        {
            _basePath = basePath;
            _targetFramework = targetFramework;
        }

        public new IEnumerable<string> GetTemplateFoldersWithFramework(string frameworkTemplateFolder, string[] baseFolders)
        {
            ArgumentNullException.ThrowIfNull(baseFolders);
            var templateFolders = new List<string>();

            foreach (var baseFolderName in baseFolders)
            {
                string templatesFolderName = "Templates";
                var candidateTemplateFolders = Path.Combine(_basePath, templatesFolderName, frameworkTemplateFolder, baseFolderName);
                if (Directory.Exists(candidateTemplateFolders))
                {
                    templateFolders.Add(candidateTemplateFolders);
                }
            }

            return templateFolders;
        }

        public new IEnumerable<string> GetAllFiles(string targetFrameworkTemplateFolder, string[] baseFolders, string? extension = null)
        {
            List<string> allTemplates = [];
            var allTemplateFolders = GetTemplateFoldersWithFramework(targetFrameworkTemplateFolder, baseFolders);
            var searchPattern = string.IsNullOrEmpty(extension) ? string.Empty : $"*{Path.GetExtension(extension)}";
            if (allTemplateFolders != null && allTemplateFolders.Any())
            {
                foreach (var templateFolder in allTemplateFolders)
                {
                    allTemplates.AddRange(Directory.EnumerateFiles(templateFolder, searchPattern, SearchOption.AllDirectories));
                }
            }

            return allTemplates;
        }

        public new IEnumerable<string> GetAllT4TemplatesForTargetFramework(string[] baseFolders, string? projectPath)
        {
            return GetAllFiles(_targetFramework, baseFolders, ".tt");
        }

        public new IEnumerable<string> GetAllFilesForTargetFramework(string[] baseFolders, string? projectPath)
        {
            return GetAllFiles(_targetFramework, baseFolders);
        }
    }

    #endregion
}
