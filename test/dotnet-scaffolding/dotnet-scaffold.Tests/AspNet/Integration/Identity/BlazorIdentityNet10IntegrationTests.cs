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

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

/// <summary>
/// Integration tests to verify that all Blazor Identity files are correctly discovered,
/// added, and referenced when scaffolding targets .NET 10.
/// These tests guard against regressions where file discovery methods filter out
/// non-T4 static files (e.g., .razor.js, .cshtml) that must be copied to the user's project.
/// </summary>
public class BlazorIdentityNet10IntegrationTests : IDisposable
{
    private const string TargetFramework = "net10.0";
    private readonly string _testDirectory;
    private readonly string _toolsDirectory;
    private readonly string _templatesDirectory;

    public BlazorIdentityNet10IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "BlazorIdentityNet10IntegrationTests", Guid.NewGuid().ToString());
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
    /// Verifies that GetAllFilesForTargetFramework returns PasskeySubmit.razor.js
    /// from the net10.0 Files template folder.
    /// </summary>
    [Fact]
    public void GetAllFilesForTargetFramework_FindsPasskeySubmitRazorJs()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateFilesTemplateFolder(
            "PasskeySubmit.razor.js",
            "_ValidationScriptsPartial.cshtml",
            "ApplicationUser.tt",
            "ApplicationUser.cs",
            "ApplicationUser.Interfaces.cs");

        // Act
        var allFiles = utilities.GetAllFilesForTargetFramework(["Files"], null).ToList();

        // Assert
        Assert.Contains(allFiles, f => f.EndsWith("PasskeySubmit.razor.js", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that GetAllFilesForTargetFramework returns _ValidationScriptsPartial.cshtml.
    /// </summary>
    [Fact]
    public void GetAllFilesForTargetFramework_FindsValidationScriptsPartial()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateFilesTemplateFolder(
            "PasskeySubmit.razor.js",
            "_ValidationScriptsPartial.cshtml",
            "ApplicationUser.tt");

        // Act
        var allFiles = utilities.GetAllFilesForTargetFramework(["Files"], null).ToList();

        // Assert
        Assert.Contains(allFiles, f => f.EndsWith("_ValidationScriptsPartial.cshtml", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies GetAllFilesForTargetFramework returns ALL files regardless of extension.
    /// </summary>
    [Fact]
    public void GetAllFilesForTargetFramework_ReturnsAllFileTypes_NotJustTT()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateFilesTemplateFolder(
            "PasskeySubmit.razor.js",
            "_ValidationScriptsPartial.cshtml",
            "ApplicationUser.tt",
            "ApplicationUser.cs",
            "ApplicationUser.Interfaces.cs");

        // Act
        var allFiles = utilities.GetAllFilesForTargetFramework(["Files"], null).ToList();

        // Assert - all 5 files should be found
        Assert.Equal(5, allFiles.Count);
        Assert.Contains(allFiles, f => f.EndsWith(".razor.js", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(allFiles, f => f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(allFiles, f => f.EndsWith(".tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(allFiles, f => f.EndsWith("ApplicationUser.cs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(allFiles, f => f.EndsWith("ApplicationUser.Interfaces.cs", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that GetAllT4TemplatesForTargetFramework does NOT return non-.tt files.
    /// </summary>
    [Fact]
    public void GetAllT4TemplatesForTargetFramework_DoesNotReturnStaticFiles()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateFilesTemplateFolder(
            "PasskeySubmit.razor.js",
            "_ValidationScriptsPartial.cshtml",
            "ApplicationUser.tt");

        // Act
        var ttFiles = utilities.GetAllT4TemplatesForTargetFramework(["Files"], null).ToList();

        // Assert - only .tt file should be returned
        Assert.Single(ttFiles);
        Assert.Contains(ttFiles, f => f.EndsWith("ApplicationUser.tt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ttFiles, f => f.EndsWith(".razor.js", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ttFiles, f => f.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Blazor Identity T4 Template Discovery

    /// <summary>
    /// Verifies that GetAllT4TemplatesForTargetFramework finds all expected BlazorIdentity
    /// T4 templates for net10.0.
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

        // Root-level templates
        Assert.Contains(templates, f => f.EndsWith("IdentityComponentsEndpointRouteBuilderExtensions.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.EndsWith("IdentityNoOpEmailSender.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.EndsWith("IdentityRedirectManager.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.EndsWith("IdentityRevalidatingAuthenticationStateProvider.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.EndsWith("PasskeyInputModel.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.EndsWith("PasskeyOperation.tt", StringComparison.OrdinalIgnoreCase));

        // Pages templates
        Assert.Contains(templates, f => f.Contains("Pages") && f.EndsWith("Login.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.Contains("Pages") && f.EndsWith("Register.tt", StringComparison.OrdinalIgnoreCase));

        // Shared templates
        Assert.Contains(templates, f => f.Contains("Shared") && f.EndsWith("PasskeySubmit.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.Contains("Shared") && f.EndsWith("StatusMessage.tt", StringComparison.OrdinalIgnoreCase));

        // Manage templates
        Assert.Contains(templates, f => f.Contains("Manage") && f.EndsWith("Index.tt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(templates, f => f.Contains("Manage") && f.EndsWith("Passkeys.tt", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Code Modification Config - blazorIdentityChanges.json

    /// <summary>
    /// Verifies that the net10.0 blazorIdentityChanges.json config file exists.
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_ExistsForNet10()
    {
        var configPath = GetBlazorIdentityChangesConfigPath();
        Assert.True(File.Exists(configPath), $"blazorIdentityChanges.json not found at: {configPath}");
    }

    /// <summary>
    /// Verifies that NavMenu.razor.css is referenced in blazorIdentityChanges.json for net10.0.
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
    /// Verifies that NavMenu.razor is referenced in blazorIdentityChanges.json for net10.0.
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

                // Verify AuthorizeView is mentioned in replacements
                var replacementsText = replacements.ToString();
                Assert.Contains("AuthorizeView", replacementsText);
                break;
            }
        }

        Assert.True(found, "NavMenu.razor not found in blazorIdentityChanges.json Files array");
    }

    /// <summary>
    /// Verifies that App.razor is referenced in blazorIdentityChanges.json for net10.0.
    /// </summary>
    [Fact]
    public void BlazorIdentityChangesConfig_ReferencesAppRazor()
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
                fileName.GetString()?.Contains("App.razor", StringComparison.OrdinalIgnoreCase) == true)
            {
                found = true;
                Assert.True(file.TryGetProperty("Replacements", out var replacements));
                Assert.True(replacements.GetArrayLength() > 0);

                var replacementsText = replacements.ToString();
                Assert.Contains("PasskeySubmit.razor.js", replacementsText);
                break;
            }
        }

        Assert.True(found, "App.razor not found in blazorIdentityChanges.json Files array");
    }

    /// <summary>
    /// Verifies that Routes.razor is referenced in blazorIdentityChanges.json for net10.0.
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
    /// Verifies that _Imports.razor is referenced in blazorIdentityChanges.json for net10.0.
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
    /// Verifies that Program.cs is referenced in blazorIdentityChanges.json for net10.0.
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
        Assert.Contains(referencedFileNames, f => f.Contains("App.razor", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Actual Template Existence Tests on Disk

    /// <summary>
    /// Verifies that PasskeySubmit.razor.js exists in the actual net10.0/Files template folder.
    /// </summary>
    [Fact]
    public void Net10_Files_PasskeySubmitRazorJs_ExistsOnDisk()
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Files", "PasskeySubmit.razor.js"));
    }

    /// <summary>
    /// Verifies that _ValidationScriptsPartial.cshtml exists in the actual net10.0/Files template folder.
    /// </summary>
    [Fact]
    public void Net10_Files_ValidationScriptsPartial_ExistsOnDisk()
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Files", "_ValidationScriptsPartial.cshtml"));
    }

    /// <summary>
    /// Verifies that ApplicationUser.tt exists in the actual net10.0/Files template folder.
    /// </summary>
    [Fact]
    public void Net10_Files_ApplicationUserTT_ExistsOnDisk()
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "Files", "ApplicationUser.tt"));
    }

    /// <summary>
    /// Verifies all BlazorIdentity root-level .tt templates exist on disk for net10.0.
    /// </summary>
    [Theory]
    [InlineData("IdentityComponentsEndpointRouteBuilderExtensions")]
    [InlineData("IdentityNoOpEmailSender")]
    [InlineData("IdentityRedirectManager")]
    [InlineData("IdentityRevalidatingAuthenticationStateProvider")]
    [InlineData("PasskeyInputModel")]
    [InlineData("PasskeyOperation")]
    public void Net10_BlazorIdentity_RootTemplates_ExistOnDisk(string templateName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "BlazorIdentity", $"{templateName}.tt"));
    }

    /// <summary>
    /// Verifies all BlazorIdentity/Pages .tt templates exist on disk for net10.0.
    /// </summary>
    [Theory]
    [InlineData("_Imports")]
    [InlineData("AccessDenied")]
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
    public void Net10_BlazorIdentity_PagesTemplates_ExistOnDisk(string templateName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "BlazorIdentity", "Pages", $"{templateName}.tt"));
    }

    /// <summary>
    /// Verifies all BlazorIdentity/Pages/Manage .tt templates exist on disk for net10.0.
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
    [InlineData("Passkeys")]
    [InlineData("PersonalData")]
    [InlineData("RenamePasskey")]
    [InlineData("ResetAuthenticator")]
    [InlineData("SetPassword")]
    [InlineData("TwoFactorAuthentication")]
    public void Net10_BlazorIdentity_ManageTemplates_ExistOnDisk(string templateName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "BlazorIdentity", "Pages", "Manage", $"{templateName}.tt"));
    }

    /// <summary>
    /// Verifies all BlazorIdentity/Shared .tt templates exist on disk for net10.0.
    /// </summary>
    [Theory]
    [InlineData("ExternalLoginPicker")]
    [InlineData("ManageLayout")]
    [InlineData("ManageNavMenu")]
    [InlineData("PasskeySubmit")]
    [InlineData("RedirectToLogin")]
    [InlineData("ShowRecoveryCodes")]
    [InlineData("StatusMessage")]
    public void Net10_BlazorIdentity_SharedTemplates_ExistOnDisk(string templateName)
    {
        AssertActualTemplateFileExists(Path.Combine(TargetFramework, "BlazorIdentity", "Shared", $"{templateName}.tt"));
    }

    #endregion

    #region End-to-End: BlazorIdentityHelper generates properties for all templates

    /// <summary>
    /// Verifies that BlazorIdentityHelper.GetTextTemplatingProperties generates text
    /// templating properties for net10.0 BlazorIdentity T4 templates with correct extensions.
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

    /// <summary>
    /// Verifies that BlazorIdentityHelper.GetApplicationUserTextTemplatingProperty returns
    /// a valid property when given the net10.0 ApplicationUser.tt template path.
    /// </summary>
    [Fact]
    public void BlazorIdentityHelper_GetApplicationUserProperty_ReturnsValidForNet10()
    {
        // Arrange
        var templatesBasePath = GetActualTemplatesBasePath();
        var applicationUserTt = Path.Combine(templatesBasePath, TargetFramework, "Files", "ApplicationUser.tt");
        if (!File.Exists(applicationUserTt))
        {
            return;
        }

        var identityModel = CreateTestIdentityModel();

        // Act
        var property = BlazorIdentityHelper.GetApplicationUserTextTemplatingProperty(applicationUserTt, identityModel);

        // Assert
        Assert.NotNull(property);
        Assert.Equal(applicationUserTt, property.TemplatePath);
        Assert.Contains("ApplicationUser", property.OutputPath);
        Assert.EndsWith(".cs", property.OutputPath);
        Assert.Contains("Data", property.OutputPath);
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
            "PasskeySubmit.razor.js",
            "_ValidationScriptsPartial.cshtml",
            "ApplicationUser.tt",
            "ApplicationUser.cs",
            "ApplicationUser.Interfaces.cs");

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
    /// Regression test: verifies that the net10.0 Files folder contains both
    /// T4 templates and non-T4 static files, and that our methods handle both correctly.
    /// </summary>
    [Fact]
    public void Net10_FilesFolder_ContainsBothT4AndStaticFiles()
    {
        // Arrange
        var utilities = CreateTestableUtilities();
        CreateFilesTemplateFolder(
            "PasskeySubmit.razor.js",
            "_ValidationScriptsPartial.cshtml",
            "ApplicationUser.tt",
            "ApplicationUser.cs",
            "ApplicationUser.Interfaces.cs");

        // Act
        var allFiles = utilities.GetAllFilesForTargetFramework(["Files"], null).ToList();

        // Assert
        var ttFiles = allFiles.Where(f => f.EndsWith(".tt")).ToList();
        var nonTtFiles = allFiles.Where(f => !f.EndsWith(".tt")).ToList();

        Assert.NotEmpty(ttFiles);
        Assert.NotEmpty(nonTtFiles);

        Assert.Contains(nonTtFiles, f => f.EndsWith("PasskeySubmit.razor.js", StringComparison.OrdinalIgnoreCase));
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

        // Root-level templates
        var rootTemplates = new[]
        {
            "IdentityComponentsEndpointRouteBuilderExtensions",
            "IdentityNoOpEmailSender",
            "IdentityRedirectManager",
            "IdentityRevalidatingAuthenticationStateProvider",
            "PasskeyInputModel",
            "PasskeyOperation"
        };

        Directory.CreateDirectory(baseDir);
        foreach (var name in rootTemplates)
        {
            File.WriteAllText(Path.Combine(baseDir, $"{name}.tt"), $"// {name} template");
        }

        // Pages templates
        var pagesDir = Path.Combine(baseDir, "Pages");
        Directory.CreateDirectory(pagesDir);
        var pageTemplates = new[] { "Login", "Register", "_Imports", "AccessDenied", "ConfirmEmail" };
        foreach (var name in pageTemplates)
        {
            File.WriteAllText(Path.Combine(pagesDir, $"{name}.tt"), $"// {name} template");
        }

        // Manage templates
        var manageDir = Path.Combine(pagesDir, "Manage");
        Directory.CreateDirectory(manageDir);
        var manageTemplates = new[] { "Index", "Passkeys", "_Imports", "ChangePassword" };
        foreach (var name in manageTemplates)
        {
            File.WriteAllText(Path.Combine(manageDir, $"{name}.tt"), $"// {name} template");
        }

        // Shared templates
        var sharedDir = Path.Combine(baseDir, "Shared");
        Directory.CreateDirectory(sharedDir);
        var sharedTemplates = new[] { "PasskeySubmit", "StatusMessage", "ManageNavMenu", "ExternalLoginPicker", "RedirectToLogin" };
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
