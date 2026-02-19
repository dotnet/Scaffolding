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
/// Integration tests for the ASP.NET Core Identity (non-Blazor) scaffolder targeting .NET 8.
/// Validates scaffolder definition constants, ValidateIdentityStep validation logic,
/// IdentityModel/IdentitySettings properties, IdentityHelper template resolution,
/// template folder structure for Bootstrap4 and Bootstrap5, code modification config
/// (identityMinimalHostingChanges.json), NuGet package constants, and template file counts.
///
/// Net 8 Identity templates use the traditional .cshtml Razor Pages format (not .tt T4 templates)
/// with Pages/Account, Pages/Account/Manage, Data, and wwwroot subfolders.
/// Bootstrap5 has 142 total files; Bootstrap4 has 117 total files.
///
/// Key differences from net9+:
///  - Uses identityMinimalHostingChanges.json (not identityChanges.json)
///  - Identity Razor Pages with .cshtml templates
///  - Bootstrap4 Manage has 33 files; Bootstrap5 Manage has 34 (extra _ViewStart.cshtml)
///  - wwwroot: Bootstrap5 has 60 files (RTL variants); Bootstrap4 has 36 files
/// </summary>
public class IdentityNet8IntegrationTests : IDisposable
{
    private const string TargetFramework = "net8.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly string _templatesDirectory;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public IdentityNet8IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "IdentityNet8IntegrationTests", Guid.NewGuid().ToString());
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
    public void ScaffolderName_IsIdentity_Net8()
    {
        Assert.Equal("identity", AspnetStrings.Identity.Name);
    }

    [Fact]
    public void ScaffolderDisplayName_IsAspNetCoreIdentity_Net8()
    {
        Assert.Equal("ASP.NET Core Identity", AspnetStrings.Identity.DisplayName);
    }

    [Fact]
    public void ScaffolderDescription_IsIdentityDescription_Net8()
    {
        Assert.Equal("Add ASP.NET Core identity to a project.", AspnetStrings.Identity.Description);
    }

    [Fact]
    public void ScaffolderCategory_IsIdentity_Net8()
    {
        Assert.Equal("Identity", AspnetStrings.Catagories.Identity);
    }

    [Fact]
    public void ScaffolderExample1_ContainsIdentityCommand_Net8()
    {
        Assert.Contains("identity", AspnetStrings.Identity.IdentityExample1);
        Assert.Contains("--project", AspnetStrings.Identity.IdentityExample1);
        Assert.Contains("--database-provider", AspnetStrings.Identity.IdentityExample1);
    }

    [Fact]
    public void ScaffolderExample2_ContainsOverwriteOption_Net8()
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

    #region Template Folder Structure — Bootstrap5

    [Fact]
    public void Net8_Identity_Bootstrap5_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5");
        Assert.True(Directory.Exists(identityDir), $"Identity/Bootstrap5 not found at: {identityDir}");
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_HasExpectedTotalFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5");
        if (!Directory.Exists(identityDir)) return;

        var allFiles = Directory.EnumerateFiles(identityDir, "*", SearchOption.AllDirectories).ToList();
        Assert.Equal(142, allFiles.Count);
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_HasExpectedRootFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5");
        if (!Directory.Exists(identityDir)) return;

        var rootFiles = Directory.EnumerateFiles(identityDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(6, rootFiles.Count);
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_HasExpectedDataFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var dataDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "Data");
        if (!Directory.Exists(dataDir)) return;

        var dataFiles = Directory.EnumerateFiles(dataDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(2, dataFiles.Count);
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_HasExpectedPagesTopLevelFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "Pages");
        if (!Directory.Exists(pagesDir)) return;

        var pagesFiles = Directory.EnumerateFiles(pagesDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(6, pagesFiles.Count);
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_HasExpectedAccountFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var accountDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "Pages", "Account");
        if (!Directory.Exists(accountDir)) return;

        var accountFiles = Directory.EnumerateFiles(accountDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(34, accountFiles.Count);
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_HasExpectedManageFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var manageDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "Pages", "Account", "Manage");
        if (!Directory.Exists(manageDir)) return;

        var manageFiles = Directory.EnumerateFiles(manageDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(34, manageFiles.Count);
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_HasExpectedWwwrootFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var wwwrootDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "wwwroot");
        if (!Directory.Exists(wwwrootDir)) return;

        var wwwrootFiles = Directory.EnumerateFiles(wwwrootDir, "*", SearchOption.AllDirectories).ToList();
        Assert.Equal(60, wwwrootFiles.Count);
    }

    #endregion

    #region Template Folder Structure — Bootstrap4

    [Fact]
    public void Net8_Identity_Bootstrap4_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4");
        Assert.True(Directory.Exists(identityDir), $"Identity/Bootstrap4 not found at: {identityDir}");
    }

    [Fact]
    public void Net8_Identity_Bootstrap4_HasExpectedTotalFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4");
        if (!Directory.Exists(identityDir)) return;

        var allFiles = Directory.EnumerateFiles(identityDir, "*", SearchOption.AllDirectories).ToList();
        Assert.Equal(117, allFiles.Count);
    }

    [Fact]
    public void Net8_Identity_Bootstrap4_HasExpectedRootFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4");
        if (!Directory.Exists(identityDir)) return;

        var rootFiles = Directory.EnumerateFiles(identityDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(6, rootFiles.Count);
    }

    [Fact]
    public void Net8_Identity_Bootstrap4_HasExpectedManageFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var manageDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4", "Pages", "Account", "Manage");
        if (!Directory.Exists(manageDir)) return;

        // Bootstrap4 Manage has 33 files (no _ViewStart.cshtml compared to Bootstrap5's 34)
        var manageFiles = Directory.EnumerateFiles(manageDir, "*", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(33, manageFiles.Count);
    }

    [Fact]
    public void Net8_Identity_Bootstrap4_HasExpectedWwwrootFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var wwwrootDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4", "wwwroot");
        if (!Directory.Exists(wwwrootDir)) return;

        // Bootstrap4 wwwroot has 36 files (no RTL variants compared to Bootstrap5's 60)
        var wwwrootFiles = Directory.EnumerateFiles(wwwrootDir, "*", SearchOption.AllDirectories).ToList();
        Assert.Equal(36, wwwrootFiles.Count);
    }

    #endregion

    #region Bootstrap5 vs Bootstrap4: Structural Differences

    [Fact]
    public void Net8_Bootstrap5_Manage_HasViewStart_Bootstrap4_DoesNot()
    {
        var basePath = GetActualTemplatesBasePath();
        var b5ManageDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "Pages", "Account", "Manage");
        var b4ManageDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4", "Pages", "Account", "Manage");
        if (!Directory.Exists(b5ManageDir) || !Directory.Exists(b4ManageDir)) return;

        var b5Files = Directory.EnumerateFiles(b5ManageDir, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).ToList();
        var b4Files = Directory.EnumerateFiles(b4ManageDir, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).ToList();

        Assert.Contains("Account.Manage._ViewStart.cshtml", b5Files);
        Assert.DoesNotContain("Account.Manage._ViewStart.cshtml", b4Files);
    }

    [Fact]
    public void Net8_Bootstrap5_HasMoreWwwrootFiles_ThanBootstrap4()
    {
        var basePath = GetActualTemplatesBasePath();
        var b5WwwrootDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "wwwroot");
        var b4WwwrootDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4", "wwwroot");
        if (!Directory.Exists(b5WwwrootDir) || !Directory.Exists(b4WwwrootDir)) return;

        var b5Count = Directory.EnumerateFiles(b5WwwrootDir, "*", SearchOption.AllDirectories).Count();
        var b4Count = Directory.EnumerateFiles(b4WwwrootDir, "*", SearchOption.AllDirectories).Count();

        Assert.True(b5Count > b4Count, $"Bootstrap5 wwwroot ({b5Count}) should have more files than Bootstrap4 ({b4Count})");
    }

    [Fact]
    public void Net8_Bootstrap4_And_Bootstrap5_HaveSameRootFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var b5Dir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5");
        var b4Dir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4");
        if (!Directory.Exists(b5Dir) || !Directory.Exists(b4Dir)) return;

        var b5RootFiles = Directory.EnumerateFiles(b5Dir, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(f => f).ToList();
        var b4RootFiles = Directory.EnumerateFiles(b4Dir, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(f => f).ToList();

        Assert.Equal(b4RootFiles, b5RootFiles);
    }

    [Fact]
    public void Net8_Bootstrap4_And_Bootstrap5_HaveSameAccountFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var b5AccountDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "Pages", "Account");
        var b4AccountDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4", "Pages", "Account");
        if (!Directory.Exists(b5AccountDir) || !Directory.Exists(b4AccountDir)) return;

        var b5AccountFiles = Directory.EnumerateFiles(b5AccountDir, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(f => f).ToList();
        var b4AccountFiles = Directory.EnumerateFiles(b4AccountDir, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(f => f).ToList();

        Assert.Equal(b4AccountFiles, b5AccountFiles);
    }

    #endregion

    #region Actual Template Existence Tests — Bootstrap5 Root Files

    [Theory]
    [InlineData("IdentityHostingStartup.cshtml")]
    [InlineData("ScaffoldingReadme.cshtml")]
    [InlineData("SupportPages._CookieConsentPartial.cshtml")]
    [InlineData("SupportPages._ViewImports.cshtml")]
    [InlineData("SupportPages._ViewStart.cshtml")]
    [InlineData("_LoginPartial.cshtml")]
    public void Net8_Identity_Bootstrap5_RootFiles_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Bootstrap5", fileName));
    }

    #endregion

    #region Actual Template Existence Tests — Bootstrap5 Data Files

    [Theory]
    [InlineData("ApplicationDbContext.cshtml")]
    [InlineData("ApplicationUser.cshtml")]
    public void Net8_Identity_Bootstrap5_DataFiles_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Bootstrap5", "Data", fileName));
    }

    #endregion

    #region Actual Template Existence Tests — Bootstrap5 Pages

    [Theory]
    [InlineData("_Layout.cshtml")]
    [InlineData("_ValidationScriptsPartial.cshtml")]
    [InlineData("_ViewImports.cshtml")]
    [InlineData("_ViewStart.cshtml")]
    [InlineData("Error.cshtml")]
    [InlineData("Error.cs.cshtml")]
    public void Net8_Identity_Bootstrap5_PagesFiles_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Bootstrap5", "Pages", fileName));
    }

    #endregion

    #region Actual Template Existence Tests — Bootstrap5 Account Pages

    [Theory]
    [InlineData("Account.AccessDenied.cshtml")]
    [InlineData("Account.AccessDenied.cs.cshtml")]
    [InlineData("Account.ConfirmEmail.cshtml")]
    [InlineData("Account.ConfirmEmail.cs.cshtml")]
    [InlineData("Account.ConfirmEmailChange.cshtml")]
    [InlineData("Account.ConfirmEmailChange.cs.cshtml")]
    [InlineData("Account.ExternalLogin.cshtml")]
    [InlineData("Account.ExternalLogin.cs.cshtml")]
    [InlineData("Account.ForgotPassword.cshtml")]
    [InlineData("Account.ForgotPassword.cs.cshtml")]
    [InlineData("Account.ForgotPasswordConfirmation.cshtml")]
    [InlineData("Account.ForgotPasswordConfirmation.cs.cshtml")]
    [InlineData("Account.Lockout.cshtml")]
    [InlineData("Account.Lockout.cs.cshtml")]
    [InlineData("Account.Login.cshtml")]
    [InlineData("Account.Login.cs.cshtml")]
    [InlineData("Account.LoginWith2fa.cshtml")]
    [InlineData("Account.LoginWith2fa.cs.cshtml")]
    [InlineData("Account.LoginWithRecoveryCode.cshtml")]
    [InlineData("Account.LoginWithRecoveryCode.cs.cshtml")]
    [InlineData("Account.Logout.cshtml")]
    [InlineData("Account.Logout.cs.cshtml")]
    [InlineData("Account.Register.cshtml")]
    [InlineData("Account.Register.cs.cshtml")]
    [InlineData("Account.RegisterConfirmation.cshtml")]
    [InlineData("Account.RegisterConfirmation.cs.cshtml")]
    [InlineData("Account.ResendEmailConfirmation.cshtml")]
    [InlineData("Account.ResendEmailConfirmation.cs.cshtml")]
    [InlineData("Account.ResetPassword.cshtml")]
    [InlineData("Account.ResetPassword.cs.cshtml")]
    [InlineData("Account.ResetPasswordConfirmation.cshtml")]
    [InlineData("Account.ResetPasswordConfirmation.cs.cshtml")]
    [InlineData("Account._StatusMessage.cshtml")]
    [InlineData("Account._ViewImports.cshtml")]
    public void Net8_Identity_Bootstrap5_AccountPages_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Bootstrap5", "Pages", "Account", fileName));
    }

    #endregion

    #region Actual Template Existence Tests — Bootstrap5 Manage Pages

    [Theory]
    [InlineData("Account.Manage.ChangePassword.cshtml")]
    [InlineData("Account.Manage.ChangePassword.cs.cshtml")]
    [InlineData("Account.Manage.DeletePersonalData.cshtml")]
    [InlineData("Account.Manage.DeletePersonalData.cs.cshtml")]
    [InlineData("Account.Manage.Disable2fa.cshtml")]
    [InlineData("Account.Manage.Disable2fa.cs.cshtml")]
    [InlineData("Account.Manage.DownloadPersonalData.cshtml")]
    [InlineData("Account.Manage.DownloadPersonalData.cs.cshtml")]
    [InlineData("Account.Manage.Email.cshtml")]
    [InlineData("Account.Manage.Email.cs.cshtml")]
    [InlineData("Account.Manage.EnableAuthenticator.cshtml")]
    [InlineData("Account.Manage.EnableAuthenticator.cs.cshtml")]
    [InlineData("Account.Manage.ExternalLogins.cshtml")]
    [InlineData("Account.Manage.ExternalLogins.cs.cshtml")]
    [InlineData("Account.Manage.GenerateRecoveryCodes.cshtml")]
    [InlineData("Account.Manage.GenerateRecoveryCodes.cs.cshtml")]
    [InlineData("Account.Manage.Index.cshtml")]
    [InlineData("Account.Manage.Index.cs.cshtml")]
    [InlineData("Account.Manage.ManageNavPages.cshtml")]
    [InlineData("Account.Manage.PersonalData.cshtml")]
    [InlineData("Account.Manage.PersonalData.cs.cshtml")]
    [InlineData("Account.Manage.ResetAuthenticator.cshtml")]
    [InlineData("Account.Manage.ResetAuthenticator.cs.cshtml")]
    [InlineData("Account.Manage.SetPassword.cshtml")]
    [InlineData("Account.Manage.SetPassword.cs.cshtml")]
    [InlineData("Account.Manage.ShowRecoveryCodes.cshtml")]
    [InlineData("Account.Manage.ShowRecoveryCodes.cs.cshtml")]
    [InlineData("Account.Manage.TwoFactorAuthentication.cshtml")]
    [InlineData("Account.Manage.TwoFactorAuthentication.cs.cshtml")]
    [InlineData("Account.Manage._Layout.cshtml")]
    [InlineData("Account.Manage._ManageNav.cshtml")]
    [InlineData("Account.Manage._StatusMessage.cshtml")]
    [InlineData("Account.Manage._ViewImports.cshtml")]
    [InlineData("Account.Manage._ViewStart.cshtml")]
    public void Net8_Identity_Bootstrap5_ManagePages_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Bootstrap5", "Pages", "Account", "Manage", fileName));
    }

    #endregion

    #region Actual Template Existence Tests — Bootstrap4 Root Files

    [Theory]
    [InlineData("IdentityHostingStartup.cshtml")]
    [InlineData("ScaffoldingReadme.cshtml")]
    [InlineData("SupportPages._CookieConsentPartial.cshtml")]
    [InlineData("SupportPages._ViewImports.cshtml")]
    [InlineData("SupportPages._ViewStart.cshtml")]
    [InlineData("_LoginPartial.cshtml")]
    public void Net8_Identity_Bootstrap4_RootFiles_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Bootstrap4", fileName));
    }

    #endregion

    #region Actual Template Existence Tests — Bootstrap4 Data Files

    [Theory]
    [InlineData("ApplicationDbContext.cshtml")]
    [InlineData("ApplicationUser.cshtml")]
    public void Net8_Identity_Bootstrap4_DataFiles_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Identity", "Bootstrap4", "Data", fileName));
    }

    #endregion

    #region Code Modification Config — identityMinimalHostingChanges.json

    [Fact]
    public void IdentityMinimalHostingChangesConfig_ExistsForNet8()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        Assert.True(File.Exists(configPath), $"identityMinimalHostingChanges.json not found at: {configPath}");
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_IsValidJson()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        var ex = Record.Exception(() => JsonDocument.Parse(configContent));
        Assert.Null(ex);
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_ContainsFilesArray()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        Assert.True(configJson.RootElement.TryGetProperty("Files", out var files));
        Assert.True(files.GetArrayLength() > 0);
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_ReferencesProgramCs()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
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

        Assert.True(found, "Program.cs not found in identityMinimalHostingChanges.json Files array");
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_HasGetConnectionStringChange()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("GetConnectionString", configContent);
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_HasAddDbContextChange()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("AddDbContext", configContent);
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_HasAddDefaultIdentityChange()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("AddDefaultIdentity", configContent);
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_HasAddEntityFrameworkStoresChange()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("AddEntityFrameworkStores", configContent);
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_HasRequiredUsings()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
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
    public void IdentityMinimalHostingChangesConfig_HasGlobalMethodSection()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
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

        Assert.True(found, "Global method section not found in identityMinimalHostingChanges.json");
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_RequireConfirmedAccountIsTrue()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        if (!File.Exists(configPath)) return;

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("RequireConfirmedAccount = true", configContent);
    }

    #endregion

    #region Net8-specific: Identity vs Blazor Identity — Config Differences

    [Fact]
    public void Net8_Identity_UsesMinimalHostingChanges_NotIdentityChanges()
    {
        var basePath = GetActualTemplatesBasePath();
        var minimalHostingPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "identityMinimalHostingChanges.json");
        var identityChangesPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "identityChanges.json");

        Assert.True(File.Exists(minimalHostingPath), "identityMinimalHostingChanges.json should exist for net8.0");
        Assert.False(File.Exists(identityChangesPath), "identityChanges.json should NOT exist for net8.0 (exists in net9+)");
    }

    [Fact]
    public void Net8_Identity_HasDifferentConfigNameFromBlazorIdentity()
    {
        var basePath = GetActualTemplatesBasePath();
        var identityConfigPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "identityMinimalHostingChanges.json");
        var blazorConfigPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "blazorIdentityChanges.json");

        Assert.True(File.Exists(identityConfigPath));
        Assert.True(File.Exists(blazorConfigPath));

        // They should be different files with different content
        var identityContent = File.ReadAllText(identityConfigPath);
        var blazorContent = File.ReadAllText(blazorConfigPath);
        Assert.NotEqual(identityContent, blazorContent);
    }

    #endregion

    #region Template File Discovery with Testable Utilities

    [Fact]
    public void GetAllFilesForTargetFramework_FindsAllIdentityFiles_Bootstrap5()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateIdentityTemplateFolder("Bootstrap5",
            "IdentityHostingStartup.cshtml",
            "ScaffoldingReadme.cshtml",
            "_LoginPartial.cshtml",
            "SupportPages._CookieConsentPartial.cshtml",
            "SupportPages._ViewImports.cshtml",
            "SupportPages._ViewStart.cshtml");

        // Act
        var allFiles = utilities.GetAllFilesForTargetFramework(["Identity", "Bootstrap5"], null).ToList();

        // Assert
        Assert.Equal(6, allFiles.Count);
        Assert.All(allFiles, f => Assert.EndsWith(".cshtml", f));
    }

    [Fact]
    public void GetAllFilesForTargetFramework_FindsNestedAccountFiles()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        var b5Dir = Path.Combine(_templatesDirectory, TargetFramework, "Identity", "Bootstrap5");
        var accountDir = Path.Combine(b5Dir, "Pages", "Account");
        Directory.CreateDirectory(accountDir);
        File.WriteAllText(Path.Combine(accountDir, "Account.Login.cshtml"), "// login");
        File.WriteAllText(Path.Combine(accountDir, "Account.Login.cs.cshtml"), "// login model");
        File.WriteAllText(Path.Combine(accountDir, "Account.Register.cshtml"), "// register");

        // Act — get files from the Bootstrap5 subtree
        var allFiles = utilities.GetAllFilesForTargetFramework(["Identity"], null).ToList();

        // Assert
        Assert.True(allFiles.Count >= 3);
        Assert.Contains(allFiles, f => f.Contains("Account.Login.cshtml"));
        Assert.Contains(allFiles, f => f.Contains("Account.Register.cshtml"));
    }

    [Fact]
    public void GetAllT4TemplatesForTargetFramework_ReturnsNoTTFiles_ForIdentity()
    {
        // Arrange: Identity net8.0 templates are .cshtml, not .tt
        var utilities = CreateTestableUtilities();
        CreateIdentityTemplateFolder("Bootstrap5",
            "IdentityHostingStartup.cshtml",
            "_LoginPartial.cshtml",
            "ScaffoldingReadme.cshtml");

        // Act
        var ttFiles = utilities.GetAllT4TemplatesForTargetFramework(["Identity", "Bootstrap5"], null).ToList();

        // Assert — .cshtml files should NOT be returned by T4 discovery
        Assert.Empty(ttFiles);
    }

    #endregion

    #region Net8 Identity Template Content — Account Pages

    [Fact]
    public void Net8_Identity_Bootstrap5_HasAllExpectedAccountPages()
    {
        var basePath = GetActualTemplatesBasePath();
        var accountDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "Pages", "Account");
        if (!Directory.Exists(accountDir)) return;

        var accountFiles = Directory.EnumerateFiles(accountDir, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToList();

        // Verify all expected account pages exist as pairs (.cshtml + .cs.cshtml)
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
            Assert.Contains($"Account.{page}.cshtml", accountFiles);
            Assert.Contains($"Account.{page}.cs.cshtml", accountFiles);
        }

        // Plus 2 non-paired files
        Assert.Contains("Account._StatusMessage.cshtml", accountFiles);
        Assert.Contains("Account._ViewImports.cshtml", accountFiles);
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_HasAllExpectedManagePages()
    {
        var basePath = GetActualTemplatesBasePath();
        var manageDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "Pages", "Account", "Manage");
        if (!Directory.Exists(manageDir)) return;

        var manageFiles = Directory.EnumerateFiles(manageDir, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToList();

        // Verify all expected manage pages exist as pairs (.cshtml + .cs.cshtml)
        var expectedPages = new[]
        {
            "ChangePassword", "DeletePersonalData", "Disable2fa", "DownloadPersonalData",
            "Email", "EnableAuthenticator", "ExternalLogins", "GenerateRecoveryCodes",
            "Index", "PersonalData", "ResetAuthenticator", "SetPassword",
            "ShowRecoveryCodes", "TwoFactorAuthentication"
        };

        foreach (var page in expectedPages)
        {
            Assert.Contains($"Account.Manage.{page}.cshtml", manageFiles);
            Assert.Contains($"Account.Manage.{page}.cs.cshtml", manageFiles);
        }

        // Plus non-paired files
        Assert.Contains("Account.Manage.ManageNavPages.cshtml", manageFiles);
        Assert.Contains("Account.Manage._Layout.cshtml", manageFiles);
        Assert.Contains("Account.Manage._ManageNav.cshtml", manageFiles);
        Assert.Contains("Account.Manage._StatusMessage.cshtml", manageFiles);
        Assert.Contains("Account.Manage._ViewImports.cshtml", manageFiles);
        Assert.Contains("Account.Manage._ViewStart.cshtml", manageFiles);
    }

    #endregion

    #region Net8 Identity Template — wwwroot Assets

    [Fact]
    public void Net8_Identity_Bootstrap5_Wwwroot_ContainsCss()
    {
        var basePath = GetActualTemplatesBasePath();
        var cssDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "wwwroot", "css");
        if (!Directory.Exists(cssDir)) return;

        var cssFiles = Directory.EnumerateFiles(cssDir, "*", SearchOption.AllDirectories).ToList();
        Assert.NotEmpty(cssFiles);
        Assert.Contains(cssFiles, f => f.EndsWith("site.css", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_Wwwroot_ContainsJs()
    {
        var basePath = GetActualTemplatesBasePath();
        var jsDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "wwwroot", "js");
        if (!Directory.Exists(jsDir)) return;

        var jsFiles = Directory.EnumerateFiles(jsDir, "*", SearchOption.AllDirectories).ToList();
        Assert.NotEmpty(jsFiles);
        Assert.Contains(jsFiles, f => f.EndsWith("site.js", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_Wwwroot_ContainsFavicon()
    {
        var basePath = GetActualTemplatesBasePath();
        var wwwrootDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "wwwroot");
        if (!Directory.Exists(wwwrootDir)) return;

        Assert.True(File.Exists(Path.Combine(wwwrootDir, "favicon.ico")));
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_Wwwroot_ContainsBootstrapLib()
    {
        var basePath = GetActualTemplatesBasePath();
        var libDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "wwwroot", "lib", "bootstrap");
        if (!Directory.Exists(libDir)) return;

        var bootstrapFiles = Directory.EnumerateFiles(libDir, "*", SearchOption.AllDirectories).ToList();
        Assert.NotEmpty(bootstrapFiles);
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_Wwwroot_ContainsJQueryLib()
    {
        var basePath = GetActualTemplatesBasePath();
        var jqueryDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "wwwroot", "lib", "jquery");
        if (!Directory.Exists(jqueryDir)) return;

        var jqueryFiles = Directory.EnumerateFiles(jqueryDir, "*", SearchOption.AllDirectories).ToList();
        Assert.NotEmpty(jqueryFiles);
    }

    [Fact]
    public void Net8_Identity_Bootstrap5_Wwwroot_ContainsJQueryValidationLib()
    {
        var basePath = GetActualTemplatesBasePath();
        var jqvDir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5", "wwwroot", "lib", "jquery-validation");
        if (!Directory.Exists(jqvDir)) return;

        var jqvFiles = Directory.EnumerateFiles(jqvDir, "*", SearchOption.AllDirectories).ToList();
        Assert.NotEmpty(jqvFiles);
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

    #region IdentityHelper — GetFormattedRelativeIdentityFile via GetTextTemplatingProperties

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

    private void CreateIdentityTemplateFolder(string bootstrapVersion, params string[] fileNames)
    {
        var identityFolder = Path.Combine(_templatesDirectory, TargetFramework, "Identity", bootstrapVersion);
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

    private static string GetIdentityMinimalHostingChangesConfigPath()
    {
        var basePath = GetActualTemplatesBasePath();
        return Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "identityMinimalHostingChanges.json");
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
