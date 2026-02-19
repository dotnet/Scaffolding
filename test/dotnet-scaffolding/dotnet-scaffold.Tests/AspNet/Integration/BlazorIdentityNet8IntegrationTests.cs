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
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// Integration tests to verify that all Blazor Identity files are correctly discovered,
/// added, and referenced when scaffolding targets .NET 8.
/// Net 8 differs from net9+ in several ways:
///  - Root templates: same 5 as net9 (IdentityUserAccessor, no passkeys)
///  - Pages: 17 templates (no AccessDenied compared to net9's 18)
///  - Manage: 13 templates (same as net9)
///  - Shared: 7 templates (same as net9: AccountLayout, no PasskeySubmit)
///  - Files: 12 files (IdentityApplicationUser/IdentityDbContext pattern, various .cshtml)
///  - blazorIdentityChanges.json: NavMenu.razor (not Components\Layout\NavMenu.razor)
/// </summary>
public class BlazorIdentityNet8IntegrationTests : IDisposable
{
    private const string TargetFramework = "net8.0";
    private readonly string _testDirectory;
    private readonly string _toolsDirectory;
    private readonly string _templatesDirectory;

    public BlazorIdentityNet8IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "BlazorIdentityNet8IntegrationTests", Guid.NewGuid().ToString());
        _toolsDirectory = Path.Combine(_testDirectory, "tools");
        _templatesDirectory = Path.Combine(_testDirectory, "Templates");
        Directory.CreateDirectory(_toolsDirectory);
        Directory.CreateDirectory(_templatesDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    #region Template File Discovery - Static Files (AddFileStep)

    /// <summary>
    /// Verifies that GetAllFilesForTargetFramework returns all files from the net8.0 Files folder.
    /// Net 8 has 12 files with a completely different structure from net9+.
    /// </summary>
    [Fact]
    public void GetAllFilesForTargetFramework_ReturnsAllFileTypes_NotJustTT()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateFilesTemplateFolder(
            "_Layout.cshtml",
            "Startup.cshtml",
            "ReadMe.cshtml",
            "Error.cshtml",
            "IdentityDbContextModel.cs",
            "IdentityDbContext.tt",
            "IdentityDbContext.Interfaces.cs",
            "IdentityDbContext.cs",
            "IdentityApplicationUserModel.cs",
            "IdentityApplicationUser.tt",
            "IdentityApplicationUser.Interfaces.cs",
            "IdentityApplicationUser.cs");

        // Act
        var allFiles = utilities.GetAllFilesForTargetFramework(["Files"], null).ToList();

        // Assert - all 12 files should be found
        Assert.Equal(12, allFiles.Count);
        Assert.Contains(allFiles, f => f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(allFiles, f => f.EndsWith(".tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(allFiles, f => f.EndsWith("IdentityApplicationUser.cs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(allFiles, f => f.EndsWith("IdentityDbContext.cs", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that GetAllT4TemplatesForTargetFramework only returns .tt files from Files folder.
    /// Net 8 has 2 .tt files: IdentityApplicationUser.tt and IdentityDbContext.tt.
    /// </summary>
    [Fact]
    public void GetAllT4TemplatesForTargetFramework_ReturnsOnlyTTFiles()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateFilesTemplateFolder(
            "_Layout.cshtml",
            "Error.cshtml",
            "IdentityDbContext.tt",
            "IdentityDbContext.cs",
            "IdentityApplicationUser.tt",
            "IdentityApplicationUser.cs");

        // Act
        var ttFiles = utilities.GetAllT4TemplatesForTargetFramework(["Files"], null).ToList();

        // Assert - only .tt files
        Assert.Equal(2, ttFiles.Count);
        Assert.Contains(ttFiles, f => f.EndsWith("IdentityApplicationUser.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ttFiles, f => f.EndsWith("IdentityDbContext.tt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ttFiles, f => f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ttFiles, f => f.EndsWith("IdentityApplicationUser.cs", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Net 8 does NOT have PasskeySubmit.razor.js (no passkey support).
    /// </summary>
    [Fact]
    public void Net8_Files_DoesNotContainPasskeySubmitRazorJs()
    {
        var basePath = GetActualTemplatesBasePath();
        var filesDir = Path.Combine(basePath, TargetFramework, "Files");
        if (!Directory.Exists(filesDir))
        {
            return;
        }

        var allFiles = Directory.EnumerateFiles(filesDir, "*", SearchOption.AllDirectories).ToList();
        Assert.DoesNotContain(allFiles, f => f.EndsWith("PasskeySubmit.razor.js", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Net 8 does NOT use ApplicationUser.tt (uses IdentityApplicationUser.tt instead).
    /// </summary>
    [Fact]
    public void Net8_Files_DoesNotContainApplicationUserTT()
    {
        var basePath = GetActualTemplatesBasePath();
        var filesDir = Path.Combine(basePath, TargetFramework, "Files");
        if (!Directory.Exists(filesDir))
        {
            return;
        }

        var allFiles = Directory.EnumerateFiles(filesDir, "*", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .ToList();
        Assert.DoesNotContain("ApplicationUser.tt", allFiles);
        Assert.Contains("IdentityApplicationUser.tt", allFiles);
    }

    #endregion

    #region Blazor Identity T4 Template Discovery

    /// <summary>
    /// Verifies that GetAllT4TemplatesForTargetFramework finds all expected BlazorIdentity
    /// T4 templates for net8.0.
    /// </summary>
    [Fact]
    public void GetAllT4Templates_FindsAllBlazorIdentityTemplates()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateBlazorIdentityTemplateFolder();

        // Act
        var templates = utilities.GetAllT4TemplatesForTargetFramework(["BlazorIdentity"], null).ToList();

        // Assert - should find all .tt files we created
        Assert.NotEmpty(templates);
        Assert.All(templates, t => Assert.EndsWith(".tt", t));

        // Root-level templates (same as net9)
        Assert.Contains(templates, f => f.EndsWith("IdentityComponentsEndpointRouteBuilderExtensions.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.EndsWith("IdentityNoOpEmailSender.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.EndsWith("IdentityRedirectManager.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.EndsWith("IdentityRevalidatingAuthenticationStateProvider.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.EndsWith("IdentityUserAccessor.tt", StringComparison.OrdinalIgnoreCase));

        // Net8 should NOT have passkey root templates
        Assert.DoesNotContain(templates, f => f.EndsWith("PasskeyInputModel.tt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(templates, f => f.EndsWith("PasskeyOperation.tt", StringComparison.OrdinalIgnoreCase));

        // Pages templates
        Assert.Contains(templates, f => f.Contains("Pages") && f.EndsWith("Login.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.Contains("Pages") && f.EndsWith("Register.tt", StringComparison.OrdinalIgnoreCase));

        // Net8 does NOT have AccessDenied
        Assert.DoesNotContain(templates, f => f.Contains("Pages") && f.EndsWith("AccessDenied.tt", StringComparison.OrdinalIgnoreCase));

        // Shared templates (same as net9: AccountLayout, no PasskeySubmit)
        Assert.Contains(templates, f => f.Contains("Shared") && f.EndsWith("AccountLayout.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.Contains("Shared") && f.EndsWith("StatusMessage.tt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(templates, f => f.Contains("Shared") && f.EndsWith("PasskeySubmit.tt", StringComparison.OrdinalIgnoreCase));

        // Manage templates (same as net9: no Passkeys/RenamePasskey)
        Assert.Contains(templates, f => f.Contains("Manage") && f.EndsWith("Index.tt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(templates, f => f.Contains("Manage") && f.EndsWith("Passkeys.tt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(templates, f => f.Contains("Manage") && f.EndsWith("RenamePasskey.tt", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Code Modification Config - blazorIdentityChanges.json

    /// <summary>
    /// Verifies that the net8.0 blazorIdentityChanges.json config file exists.
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_ExistsForNet8()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        Assert.True(File.Exists(configPath), $"blazorIdentityChanges.json not found at: {configPath}");
    }

    /// <summary>
    /// Verifies that NavMenu.razor.css is referenced in blazorIdentityChanges.json for net8.0.
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_ReferencesNavMenuRazorCss()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        if (!File.Exists(configPath))
        {
            return;
        }

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        var files = configJson.RootElement.GetProperty("Files");

        bool found = false;
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("FileName", out var fileName) &&
                fileName.GetString()?.Contains("NavMenu.razor.css", StringComparison.OrdinalIgnoreCase) == true)
            {
                found = true;
                Assert.True(file.TryGetProperty("Replacements", out var replacements));
                Assert.True(replacements.GetArrayLength() > 0);
                break;
            }
        }

        Assert.True(found, "NavMenu.razor.css not found in blazorIdentityChanges.json Files array");
    }

    /// <summary>
    /// Verifies that NavMenu.razor is referenced in blazorIdentityChanges.json for net8.0.
    /// Note: net8 uses "NavMenu.razor" (not "Components\Layout\NavMenu.razor").
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_ReferencesNavMenuRazor()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        if (!File.Exists(configPath))
        {
            return;
        }

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        var files = configJson.RootElement.GetProperty("Files");

        bool found = false;
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("FileName", out var fileName) &&
                fileName.GetString()?.Contains("NavMenu.razor", StringComparison.OrdinalIgnoreCase) == true &&
                !fileName.GetString()!.Contains(".css", StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                Assert.True(file.TryGetProperty("Replacements", out var replacements));
                Assert.True(replacements.GetArrayLength() > 0);

                var replacementsText = replacements.ToString();
                Assert.Contains("AuthorizeView", replacementsText);
                break;
            }
        }

        Assert.True(found, "NavMenu.razor not found in blazorIdentityChanges.json Files array");
    }

    /// <summary>
    /// Net 8 does NOT have an App.razor entry in blazorIdentityChanges.json.
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_DoesNotReferenceAppRazor()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        if (!File.Exists(configPath))
        {
            return;
        }

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        var files = configJson.RootElement.GetProperty("Files");

        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("FileName", out var fileName) &&
                fileName.GetString()?.Equals("App.razor", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (file.TryGetProperty("Replacements", out var replacements))
                {
                    var replacementsText = replacements.ToString();
                    Assert.DoesNotContain("PasskeySubmit.razor.js", replacementsText);
                }
            }
        }
    }

    /// <summary>
    /// Verifies that Routes.razor is referenced in blazorIdentityChanges.json for net8.0.
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_ReferencesRoutesRazor()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        if (!File.Exists(configPath))
        {
            return;
        }

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        var files = configJson.RootElement.GetProperty("Files");

        bool found = false;
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("FileName", out var fileName) &&
                fileName.GetString()?.Equals("Routes.razor", StringComparison.OrdinalIgnoreCase) == true)
            {
                found = true;
                Assert.True(file.TryGetProperty("Replacements", out var replacements));
                Assert.True(replacements.GetArrayLength() > 0);
                break;
            }
        }

        Assert.True(found, "Routes.razor not found in blazorIdentityChanges.json Files array");
    }

    /// <summary>
    /// Verifies that _Imports.razor is referenced in blazorIdentityChanges.json for net8.0.
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_ReferencesImportsRazor()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        if (!File.Exists(configPath))
        {
            return;
        }

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        var files = configJson.RootElement.GetProperty("Files");

        bool found = false;
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("FileName", out var fileName) &&
                fileName.GetString()?.Contains("_Imports.razor", StringComparison.OrdinalIgnoreCase) == true)
            {
                found = true;
                break;
            }
        }

        Assert.True(found, "_Imports.razor not found in blazorIdentityChanges.json Files array");
    }

    /// <summary>
    /// Verifies that Program.cs is referenced in blazorIdentityChanges.json for net8.0.
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_ReferencesProgramCs()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        if (!File.Exists(configPath))
        {
            return;
        }

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

        Assert.True(found, "Program.cs not found in blazorIdentityChanges.json Files array");
    }

    /// <summary>
    /// Comprehensive test: verifies ALL expected file references exist in blazorIdentityChanges.json.
    /// Net 8 references: Program.cs, Routes.razor, NavMenu.razor.css, NavMenu.razor, Components\_Imports.razor.
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_ContainsAllRequiredFileReferences()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        if (!File.Exists(configPath))
        {
            return;
        }

        var configContent = File.ReadAllText(configPath);
        var configJson = JsonDocument.Parse(configContent);
        var files = configJson.RootElement.GetProperty("Files");

        var referencedFileNames = new List<string>();
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("FileName", out var fileName))
            {
                referencedFileNames.Add(fileName.GetString()!);
            }
        }

        Assert.Contains(referencedFileNames, f => f.Equals("Program.cs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(referencedFileNames, f => f.Equals("Routes.razor", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(referencedFileNames, f => f.Contains("NavMenu.razor.css", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(referencedFileNames, f => f.Contains("NavMenu.razor", StringComparison.OrdinalIgnoreCase) && !f.Contains(".css", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(referencedFileNames, f => f.Contains("_Imports.razor", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that Program.cs code changes reference IdentityUserAccessor (net8 uses this, not passkeys).
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_ProgramCs_ReferencesIdentityUserAccessor()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        if (!File.Exists(configPath))
        {
            return;
        }

        var configContent = File.ReadAllText(configPath);
        Assert.Contains("IdentityUserAccessor", configContent);
    }

    #endregion

    #region Actual Template Existence Tests on Disk

    /// <summary>
    /// Verifies net8-specific Files exist on disk.
    /// </summary>
    [Theory]
    [InlineData("_Layout.cshtml")]
    [InlineData("Startup.cshtml")]
    [InlineData("ReadMe.cshtml")]
    [InlineData("Error.cshtml")]
    [InlineData("IdentityDbContextModel.cs")]
    [InlineData("IdentityDbContext.tt")]
    [InlineData("IdentityDbContext.Interfaces.cs")]
    [InlineData("IdentityDbContext.cs")]
    [InlineData("IdentityApplicationUserModel.cs")]
    [InlineData("IdentityApplicationUser.tt")]
    [InlineData("IdentityApplicationUser.Interfaces.cs")]
    [InlineData("IdentityApplicationUser.cs")]
    public void Net8_Files_ExistOnDisk(string fileName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Files", fileName));
    }

    /// <summary>
    /// Verifies all BlazorIdentity root-level .tt templates exist on disk for net8.0.
    /// Same 5 root templates as net9.
    /// </summary>
    [Theory]
    [InlineData("IdentityComponentsEndpointRouteBuilderExtensions")]
    [InlineData("IdentityNoOpEmailSender")]
    [InlineData("IdentityRedirectManager")]
    [InlineData("IdentityRevalidatingAuthenticationStateProvider")]
    [InlineData("IdentityUserAccessor")]
    public void Net8_BlazorIdentity_RootTemplates_ExistOnDisk(string templateName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "BlazorIdentity", $"{templateName}.tt"));
    }

    /// <summary>
    /// Verifies all BlazorIdentity/Pages .tt templates exist on disk for net8.0.
    /// Net 8 has 17 Pages templates (no AccessDenied compared to net9's 18).
    /// </summary>
    [Theory]
    [InlineData("_Imports")]
    [InlineData("ConfirmEmail")]
    [InlineData("ConfirmEmailChange")]
    [InlineData("ExternalLogin")]
    [InlineData("ForgotPassword")]
    [InlineData("ForgotPasswordConfirmation")]
    [InlineData("InvalidPasswordReset")]
    [InlineData("InvalidUser")]
    [InlineData("Lockout")]
    [InlineData("Login")]
    [InlineData("LoginWith2fa")]
    [InlineData("LoginWithRecoveryCode")]
    [InlineData("Register")]
    [InlineData("RegisterConfirmation")]
    [InlineData("ResendEmailConfirmation")]
    [InlineData("ResetPassword")]
    [InlineData("ResetPasswordConfirmation")]
    public void Net8_BlazorIdentity_PagesTemplates_ExistOnDisk(string templateName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "BlazorIdentity", "Pages", $"{templateName}.tt"));
    }

    /// <summary>
    /// Verifies all BlazorIdentity/Pages/Manage .tt templates exist on disk for net8.0.
    /// Same 13 Manage templates as net9.
    /// </summary>
    [Theory]
    [InlineData("_Imports")]
    [InlineData("ChangePassword")]
    [InlineData("DeletePersonalData")]
    [InlineData("Disable2fa")]
    [InlineData("Email")]
    [InlineData("EnableAuthenticator")]
    [InlineData("ExternalLogins")]
    [InlineData("GenerateRecoveryCodes")]
    [InlineData("Index")]
    [InlineData("PersonalData")]
    [InlineData("ResetAuthenticator")]
    [InlineData("SetPassword")]
    [InlineData("TwoFactorAuthentication")]
    public void Net8_BlazorIdentity_ManageTemplates_ExistOnDisk(string templateName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "BlazorIdentity", "Pages", "Manage", $"{templateName}.tt"));
    }

    /// <summary>
    /// Verifies all BlazorIdentity/Shared .tt templates exist on disk for net8.0.
    /// Same 7 Shared templates as net9 (AccountLayout, no PasskeySubmit).
    /// </summary>
    [Theory]
    [InlineData("AccountLayout")]
    [InlineData("ExternalLoginPicker")]
    [InlineData("ManageLayout")]
    [InlineData("ManageNavMenu")]
    [InlineData("RedirectToLogin")]
    [InlineData("ShowRecoveryCodes")]
    [InlineData("StatusMessage")]
    public void Net8_BlazorIdentity_SharedTemplates_ExistOnDisk(string templateName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "BlazorIdentity", "Shared", $"{templateName}.tt"));
    }

    /// <summary>
    /// Verifies net8 does NOT have AccessDenied in Pages templates.
    /// </summary>
    [Fact]
    public void Net8_BlazorIdentity_PagesTemplates_DoNotIncludeAccessDenied()
    {
        var basePath = GetActualTemplatesBasePath();
        var pagesDir = Path.Combine(basePath, TargetFramework, "BlazorIdentity", "Pages");
        if (!Directory.Exists(pagesDir))
        {
            return;
        }

        var pagesFiles = Directory.EnumerateFiles(pagesDir, "*.tt", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToList();
        Assert.DoesNotContain("AccessDenied.tt", pagesFiles);
    }

    /// <summary>
    /// Verifies net8 does NOT have passkey-related Manage templates.
    /// </summary>
    [Fact]
    public void Net8_BlazorIdentity_ManageTemplates_DoNotIncludePasskeys()
    {
        var basePath = GetActualTemplatesBasePath();
        var manageDir = Path.Combine(basePath, TargetFramework, "BlazorIdentity", "Pages", "Manage");
        if (!Directory.Exists(manageDir))
        {
            return;
        }

        var manageFiles = Directory.EnumerateFiles(manageDir, "*.tt").Select(Path.GetFileName).ToList();
        Assert.DoesNotContain("Passkeys.tt", manageFiles);
        Assert.DoesNotContain("RenamePasskey.tt", manageFiles);
    }

    /// <summary>
    /// Verifies net8 does NOT have PasskeySubmit in Shared templates.
    /// </summary>
    [Fact]
    public void Net8_BlazorIdentity_SharedTemplates_DoNotIncludePasskeySubmit()
    {
        var basePath = GetActualTemplatesBasePath();
        var sharedDir = Path.Combine(basePath, TargetFramework, "BlazorIdentity", "Shared");
        if (!Directory.Exists(sharedDir))
        {
            return;
        }

        var sharedFiles = Directory.EnumerateFiles(sharedDir, "*.tt").Select(Path.GetFileName).ToList();
        Assert.DoesNotContain("PasskeySubmit.tt", sharedFiles);
        Assert.Contains("AccountLayout.tt", sharedFiles);
    }

    #endregion

    #region End-to-End: BlazorIdentityHelper generates properties for all templates

    /// <summary>
    /// Verifies that BlazorIdentityHelper.GetTextTemplatingProperties generates text
    /// templating properties for net8.0 BlazorIdentity T4 templates with correct extensions.
    /// </summary>
    [Fact]
    public void BlazorIdentityHelper_GetTextTemplatingProperties_GeneratesPropertiesForAllTemplates()
    {
        // Arrange
        var templatesBasePath = GetActualTemplatesBasePath();
        var blazorIdentityDir = Path.Combine(templatesBasePath, TargetFramework, "BlazorIdentity");
        if (!Directory.Exists(blazorIdentityDir))
        {
            return;
        }

        var allTtFiles = Directory.EnumerateFiles(blazorIdentityDir, "*.tt", SearchOption.AllDirectories).ToList();
        Assert.NotEmpty(allTtFiles);

        var identityModel = CreateTestIdentityModel();

        // Act
        var properties = BlazorIdentityHelper.GetTextTemplatingProperties(allTtFiles, identityModel).ToList();

        // Assert
        Assert.NotNull(properties);

        foreach (var prop in properties)
        {
            Assert.NotNull(prop.OutputPath);
            Assert.NotNull(prop.TemplatePath);
            Assert.EndsWith(".tt", prop.TemplatePath);

            if (prop.TemplatePath.Contains($"Pages{Path.DirectorySeparatorChar}") ||
                prop.TemplatePath.Contains($"Shared{Path.DirectorySeparatorChar}"))
            {
                Assert.EndsWith(".razor", prop.OutputPath);
            }
            else
            {
                Assert.EndsWith(".cs", prop.OutputPath);
            }
        }
    }

    #endregion

    #region Net8-specific template count validation

    /// <summary>
    /// Validates the exact expected template counts for net8.0 BlazorIdentity.
    /// Root: 5, Pages: 17, Manage: 13, Shared: 7 = 42 total (one less Pages than net9).
    /// </summary>
    [Fact]
    public void Net8_BlazorIdentity_HasExpectedTemplateCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var blazorIdentityDir = Path.Combine(basePath, TargetFramework, "BlazorIdentity");
        if (!Directory.Exists(blazorIdentityDir))
        {
            return;
        }

        var allTtFiles = Directory.EnumerateFiles(blazorIdentityDir, "*.tt", SearchOption.AllDirectories).ToList();
        Assert.Equal(42, allTtFiles.Count);

        // Root templates
        var rootFiles = Directory.EnumerateFiles(blazorIdentityDir, "*.tt", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(5, rootFiles.Count);

        // Pages templates (17 — no AccessDenied)
        var pagesDir = Path.Combine(blazorIdentityDir, "Pages");
        var pagesFiles = Directory.EnumerateFiles(pagesDir, "*.tt", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(17, pagesFiles.Count);

        // Manage templates
        var manageDir = Path.Combine(pagesDir, "Manage");
        var manageFiles = Directory.EnumerateFiles(manageDir, "*.tt", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(13, manageFiles.Count);

        // Shared templates
        var sharedDir = Path.Combine(blazorIdentityDir, "Shared");
        var sharedFiles = Directory.EnumerateFiles(sharedDir, "*.tt", SearchOption.TopDirectoryOnly).ToList();
        Assert.Equal(7, sharedFiles.Count);
    }

    /// <summary>
    /// Validates the exact expected file count in the net8.0 Files folder (12 files).
    /// </summary>
    [Fact]
    public void Net8_FilesFolder_HasExpectedFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var filesDir = Path.Combine(basePath, TargetFramework, "Files");
        if (!Directory.Exists(filesDir))
        {
            return;
        }

        var allFiles = Directory.EnumerateFiles(filesDir, "*", SearchOption.AllDirectories).ToList();
        Assert.Equal(12, allFiles.Count);
    }

    #endregion

    #region Regression Guard: GetAllFilesForTargetFramework vs GetAllT4TemplatesForTargetFramework

    /// <summary>
    /// Regression test: verifies that GetAllFilesForTargetFramework returns a
    /// superset of what GetAllT4TemplatesForTargetFramework returns for the Files folder.
    /// </summary>
    [Fact]
    public void GetAllFilesForTargetFramework_IsSuperset_OfT4Templates()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateFilesTemplateFolder(
            "_Layout.cshtml",
            "Error.cshtml",
            "IdentityDbContext.tt",
            "IdentityDbContext.cs",
            "IdentityApplicationUser.tt",
            "IdentityApplicationUser.cs");

        // Act
        var allFiles = utilities.GetAllFilesForTargetFramework(["Files"], null).ToList();
        var ttOnly = utilities.GetAllT4TemplatesForTargetFramework(["Files"], null).ToList();

        // Assert
        Assert.True(allFiles.Count > ttOnly.Count, "GetAllFilesForTargetFramework should return more files than GetAllT4TemplatesForTargetFramework");
        foreach (var tt in ttOnly)
        {
            Assert.Contains(allFiles, f => f == tt);
        }
    }

    /// <summary>
    /// Regression test: verifies that the net8.0 Files folder contains both
    /// T4 templates and non-T4 static files, and that our methods handle both correctly.
    /// </summary>
    [Fact]
    public void Net8_FilesFolder_ContainsBothT4AndStaticFiles()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateFilesTemplateFolder(
            "_Layout.cshtml",
            "Error.cshtml",
            "IdentityDbContext.tt",
            "IdentityDbContext.cs",
            "IdentityApplicationUser.tt",
            "IdentityApplicationUser.cs");

        // Act
        var allFiles = utilities.GetAllFilesForTargetFramework(["Files"], null).ToList();

        // Assert
        var ttFiles = allFiles.Where(f => f.EndsWith(".tt")).ToList();
        var nonTtFiles = allFiles.Where(f => !f.EndsWith(".tt")).ToList();

        Assert.NotEmpty(ttFiles);
        Assert.NotEmpty(nonTtFiles);

        Assert.Contains(nonTtFiles, f => f.EndsWith("_Layout.cshtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonTtFiles, f => f.EndsWith("Error.cshtml", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Helper Methods

    private TemplateFoldersUtilitiesTestable CreateTestableUtilities()
    {
        return new TemplateFoldersUtilitiesTestable(_testDirectory, TargetFramework);
    }

    private void CreateFilesTemplateFolder(params string[] fileNames)
    {
        var filesFolder = Path.Combine(_templatesDirectory, TargetFramework, "Files");
        Directory.CreateDirectory(filesFolder);
        foreach (var fileName in fileNames)
        {
            File.WriteAllText(Path.Combine(filesFolder, fileName), $"// {fileName} content");
        }
    }

    private void CreateBlazorIdentityTemplateFolder()
    {
        string baseDir = Path.Combine(_templatesDirectory, TargetFramework, "BlazorIdentity");

        // Root-level templates (same as net9)
        var rootTemplates = new[]
        {
            "IdentityComponentsEndpointRouteBuilderExtensions",
            "IdentityNoOpEmailSender",
            "IdentityRedirectManager",
            "IdentityRevalidatingAuthenticationStateProvider",
            "IdentityUserAccessor"
        };

        Directory.CreateDirectory(baseDir);
        foreach (var name in rootTemplates)
        {
            File.WriteAllText(Path.Combine(baseDir, $"{name}.tt"), $"// {name} template");
        }

        // Pages templates (17 — no AccessDenied)
        var pagesDir = Path.Combine(baseDir, "Pages");
        Directory.CreateDirectory(pagesDir);
        var pageTemplates = new[] { "Login", "Register", "_Imports", "ConfirmEmail", "ExternalLogin" };
        foreach (var name in pageTemplates)
        {
            File.WriteAllText(Path.Combine(pagesDir, $"{name}.tt"), $"// {name} template");
        }

        // Manage templates (same as net9)
        var manageDir = Path.Combine(pagesDir, "Manage");
        Directory.CreateDirectory(manageDir);
        var manageTemplates = new[] { "Index", "_Imports", "ChangePassword" };
        foreach (var name in manageTemplates)
        {
            File.WriteAllText(Path.Combine(manageDir, $"{name}.tt"), $"// {name} template");
        }

        // Shared templates (same as net9: AccountLayout)
        var sharedDir = Path.Combine(baseDir, "Shared");
        Directory.CreateDirectory(sharedDir);
        var sharedTemplates = new[] { "AccountLayout", "StatusMessage", "ManageNavMenu", "ExternalLoginPicker", "RedirectToLogin" };
        foreach (var name in sharedTemplates)
        {
            File.WriteAllText(Path.Combine(sharedDir, $"{name}.tt"), $"// {name} template");
        }
    }

    private static string GetActualTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

    private static string GetBlazorIdentityChangesConfigPath()
    {
        var basePath = GetActualTemplatesBasePath();
        return Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "blazorIdentityChanges.json");
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
            IdentityNamespace = "TestProject.Components.Account",
            BaseOutputPath = "Components\\Account",
            UserClassName = "ApplicationUser",
            UserClassNamespace = "TestProject.Data",
            DbContextInfo = new DbContextInfo()
        };
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
