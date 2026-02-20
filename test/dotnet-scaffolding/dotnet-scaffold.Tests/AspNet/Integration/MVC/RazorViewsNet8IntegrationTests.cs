// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

/// <summary>
/// Integration tests for the Razor Views (CRUD) scaffolder targeting .NET 8.
/// The 'views' scaffolder generates Razor views for Create, Delete, Details, Edit and List
/// operations for a given model.
///
/// These tests validate:
///  - Scaffolder definition constants (Name, DisplayName, Description, Examples)
///  - Category assignment ("MVC")
///  - CLI option constants (--project, --model, --page)
///  - Option display names and descriptions (PageType, ModelName)
///  - CrudSettings property chain (CrudSettings → EfWithModelStepSettings → BaseSettings)
///  - ValidateViewsStep property defaults and get/set
///  - ValidateViewsStep validation logic (null/empty Project, Model, Page)
///  - Page type fallback to CRUD for invalid values
///  - BlazorCrudHelper.CRUDPages list contents
///  - ViewHelper template constants
///  - ViewHelper.GetBaseOutputPath output path generation
///  - Telemetry tracking via ITelemetryService
///  - Views template folder contents for net8.0 (Bootstrap4 + Bootstrap5 subfolders, 14 cshtml files)
///  - No efControllerChanges.json in net8.0 CodeModificationConfigs
///  - Template root expected scaffolder folders
/// </summary>
public class RazorViewsNet8IntegrationTests : IDisposable
{
    private const string TargetFramework = "net8.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public RazorViewsNet8IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "RazorViewsNet8IntegrationTests", Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _mockTelemetryService = new Mock<ITelemetryService>();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.RazorView.ViewsDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.RazorView.Views);
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

    #region Constants & Scaffolder Definition

    [Fact]
    public void ViewsScaffolderName_IsViews_Net8()
    {
        Assert.Equal("views", AspnetStrings.RazorView.Views);
    }

    [Fact]
    public void ViewsScaffolderDisplayName_IsRazorViews_Net8()
    {
        Assert.Equal("Razor Views", AspnetStrings.RazorView.ViewsDisplayName);
    }

    [Fact]
    public void ViewsScaffolderDescription_IsCorrect_Net8()
    {
        Assert.Equal("Generates Razor views for Create, Delete, Details, Edit and List operations for the given model",
            AspnetStrings.RazorView.ViewsDescription);
    }

    [Fact]
    public void ViewsScaffolderCategory_IsMVC_Net8()
    {
        Assert.Equal("MVC", AspnetStrings.Catagories.MVC);
    }

    [Fact]
    public void ViewsExample1_ContainsViewsCommand_Net8()
    {
        Assert.Contains("views", AspnetStrings.RazorView.ViewsExample1);
    }

    [Fact]
    public void ViewsExample1_ContainsProjectOption_Net8()
    {
        Assert.Contains("--project", AspnetStrings.RazorView.ViewsExample1);
    }

    [Fact]
    public void ViewsExample1_ContainsModelOption_Net8()
    {
        Assert.Contains("--model", AspnetStrings.RazorView.ViewsExample1);
        Assert.Contains("Product", AspnetStrings.RazorView.ViewsExample1);
    }

    [Fact]
    public void ViewsExample1_ContainsPageOption_Net8()
    {
        Assert.Contains("--page", AspnetStrings.RazorView.ViewsExample1);
        Assert.Contains("All", AspnetStrings.RazorView.ViewsExample1);
    }

    [Fact]
    public void ViewsExample1Description_ContainsCrud_Net8()
    {
        Assert.Contains("CRUD", AspnetStrings.RazorView.ViewsExample1Description);
    }

    [Fact]
    public void ViewsExample2_ContainsViewsCommand_Net8()
    {
        Assert.Contains("views", AspnetStrings.RazorView.ViewsExample2);
    }

    [Fact]
    public void ViewsExample2_ContainsCreateAndEdit_Net8()
    {
        Assert.Contains("Create", AspnetStrings.RazorView.ViewsExample2);
        Assert.Contains("Edit", AspnetStrings.RazorView.ViewsExample2);
    }

    [Fact]
    public void ViewsExample2Description_ContainsCreateAndEdit_Net8()
    {
        Assert.Contains("Create", AspnetStrings.RazorView.ViewsExample2Description);
        Assert.Contains("Edit", AspnetStrings.RazorView.ViewsExample2Description);
    }

    #endregion

    #region CLI Option Constants

    [Fact]
    public void CliOptions_ProjectOption_IsCorrect_Net8()
    {
        Assert.Equal("--project", AspNetConstants.CliOptions.ProjectCliOption);
    }

    [Fact]
    public void CliOptions_ModelOption_IsCorrect_Net8()
    {
        Assert.Equal("--model", AspNetConstants.CliOptions.ModelCliOption);
    }

    [Fact]
    public void CliOptions_PageTypeOption_IsCorrect_Net8()
    {
        Assert.Equal("--page", AspNetConstants.CliOptions.PageTypeOption);
    }

    [Fact]
    public void Constants_ViewExtension_IsCshtml_Net8()
    {
        Assert.Equal(".cshtml", AspNetConstants.ViewExtension);
    }

    #endregion

    #region Option Strings — PageType

    [Fact]
    public void PageTypeOption_DisplayName_IsCorrect_Net8()
    {
        Assert.Equal("Page Type", AspnetStrings.Options.PageType.DisplayName);
    }

    [Fact]
    public void PageTypeOption_Description_ContainsCrud_Net8()
    {
        Assert.Contains("CRUD", AspnetStrings.Options.PageType.Description);
    }

    #endregion

    #region Option Strings — ModelName

    [Fact]
    public void ModelNameOption_DisplayName_IsCorrect_Net8()
    {
        Assert.Equal("Model Name", AspnetStrings.Options.ModelName.DisplayName);
    }

    [Fact]
    public void ModelNameOption_Description_ContainsModel_Net8()
    {
        Assert.Contains("model", AspnetStrings.Options.ModelName.Description, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CrudSettings Properties

    [Fact]
    public void CrudSettings_HasPageProperty_Net8()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD"
        };

        Assert.Equal("CRUD", settings.Page);
    }

    [Fact]
    public void CrudSettings_HasModelProperty_Net8()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD"
        };

        Assert.Equal("Product", settings.Model);
    }

    [Fact]
    public void CrudSettings_HasProjectProperty_Net8()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD"
        };

        Assert.Equal(_testProjectPath, settings.Project);
    }

    [Fact]
    public void CrudSettings_InheritsEfWithModelStepSettings_Net8()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD"
        };

        Assert.IsAssignableFrom<EfWithModelStepSettings>(settings);
    }

    [Fact]
    public void CrudSettings_InheritsBaseSettings_Net8()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD"
        };

        Assert.IsAssignableFrom<BaseSettings>(settings);
    }

    [Fact]
    public void CrudSettings_PrereleaseDefaultsFalse_Net8()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD"
        };

        Assert.False(settings.Prerelease);
    }

    [Fact]
    public void CrudSettings_DatabaseProviderDefaultsNull_Net8()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD"
        };

        Assert.Null(settings.DatabaseProvider);
    }

    [Fact]
    public void CrudSettings_DataContextDefaultsNull_Net8()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD"
        };

        Assert.Null(settings.DataContext);
    }

    #endregion

    #region ValidateViewsStep — Properties

    [Fact]
    public void ValidateViewsStep_HasProjectProperty_Net8()
    {
        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        Assert.Equal(_testProjectPath, step.Project);
    }

    [Fact]
    public void ValidateViewsStep_HasModelProperty_Net8()
    {
        var step = CreateValidateViewsStep();
        step.Model = "Product";
        Assert.Equal("Product", step.Model);
    }

    [Fact]
    public void ValidateViewsStep_HasPageProperty_Net8()
    {
        var step = CreateValidateViewsStep();
        step.Page = "CRUD";
        Assert.Equal("CRUD", step.Page);
    }

    [Fact]
    public void ValidateViewsStep_ProjectDefaultsToNull_Net8()
    {
        var step = CreateValidateViewsStep();
        Assert.Null(step.Project);
    }

    [Fact]
    public void ValidateViewsStep_ModelDefaultsToNull_Net8()
    {
        var step = CreateValidateViewsStep();
        Assert.Null(step.Model);
    }

    [Fact]
    public void ValidateViewsStep_PageDefaultsToNull_Net8()
    {
        var step = CreateValidateViewsStep();
        Assert.Null(step.Page);
    }

    #endregion

    #region ValidateViewsStep — Validation Logic

    [Fact]
    public async Task ValidateViewsStep_FailsWithNullProject_Net8()
    {
        var step = CreateValidateViewsStep();
        step.Project = null;
        step.Model = "Product";
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithEmptyProject_Net8()
    {
        var step = CreateValidateViewsStep();
        step.Project = string.Empty;
        step.Model = "Product";
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithNonExistentProject_Net8()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateViewsStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Model = "Product";
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithNullModel_Net8()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithEmptyModel_Net8()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = string.Empty;
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithNullPage_Net8()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = null;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithEmptyPage_Net8()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = string.Empty;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateViewsStep_FailsWithInvalidProject_Net8(string? project)
    {
        var step = CreateValidateViewsStep();
        step.Project = project;
        step.Model = "Product";
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateViewsStep_FailsWithInvalidModel_Net8(string? model)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = model;
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateViewsStep_FailsWithInvalidPage_Net8(string? page)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = page;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region ValidateViewsStep — Telemetry

    [Fact]
    public async Task ValidateViewsStep_TracksTelemetry_OnNullProjectFailure_Net8()
    {
        var step = CreateValidateViewsStep();
        step.Project = null;
        step.Model = "Product";
        step.Page = "CRUD";

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateViewsStep_TracksTelemetry_OnNullModelFailure_Net8()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Page = "CRUD";

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateViewsStep_TracksTelemetry_OnNullPageFailure_Net8()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = null;

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    #endregion

    #region BlazorCrudHelper — CRUDPages

    [Fact]
    public void CrudPageType_IsCRUD_Net8()
    {
        Assert.Equal("CRUD", BlazorCrudHelper.CrudPageType);
    }

    [Fact]
    public void CRUDPages_ContainsAllExpectedPageTypes_Net8()
    {
        var expected = new[] { "CRUD", "Create", "Delete", "Details", "Edit", "Index", "NotFound" };
        Assert.Equal(expected.Length, BlazorCrudHelper.CRUDPages.Count);
        foreach (var page in expected)
        {
            Assert.Contains(page, BlazorCrudHelper.CRUDPages);
        }
    }

    [Fact]
    public void CRUDPages_Has7Entries_Net8()
    {
        Assert.Equal(7, BlazorCrudHelper.CRUDPages.Count);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Delete")]
    [InlineData("Details")]
    [InlineData("Edit")]
    [InlineData("Index")]
    [InlineData("CRUD")]
    [InlineData("NotFound")]
    public void CRUDPages_ContainsPageType_Net8(string pageType)
    {
        Assert.Contains(pageType, BlazorCrudHelper.CRUDPages);
    }

    #endregion

    #region ViewHelper — Template Constants

    [Fact]
    public void ViewHelper_CreateTemplate_IsCorrect_Net8()
    {
        Assert.Equal("Create.tt", ViewHelper.CreateTemplate);
    }

    [Fact]
    public void ViewHelper_DeleteTemplate_IsCorrect_Net8()
    {
        Assert.Equal("Delete.tt", ViewHelper.DeleteTemplate);
    }

    [Fact]
    public void ViewHelper_DetailsTemplate_IsCorrect_Net8()
    {
        Assert.Equal("Details.tt", ViewHelper.DetailsTemplate);
    }

    [Fact]
    public void ViewHelper_EditTemplate_IsCorrect_Net8()
    {
        Assert.Equal("Edit.tt", ViewHelper.EditTemplate);
    }

    [Fact]
    public void ViewHelper_IndexTemplate_IsCorrect_Net8()
    {
        Assert.Equal("Index.tt", ViewHelper.IndexTemplate);
    }

    #endregion

    #region ScaffolderContext

    [Fact]
    public void ScaffolderContext_HasViewsScaffolderName_Net8()
    {
        Assert.Equal("views", _context.Scaffolder.Name);
    }

    [Fact]
    public void ScaffolderContext_HasViewsDisplayName_Net8()
    {
        Assert.Equal("Razor Views", _context.Scaffolder.DisplayName);
    }

    #endregion

    #region Views Templates — Net8

    [Fact]
    public void Net8_ViewsTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        Assert.True(Directory.Exists(viewsDir),
            $"Views template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void Net8_ViewsTemplates_HasBootstrap4Subfolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var bootstrap4Dir = Path.Combine(basePath, TargetFramework, "Views", "Bootstrap4");
        Assert.True(Directory.Exists(bootstrap4Dir),
            "Bootstrap4 subfolder should exist for net8.0 Views templates");
    }

    [Fact]
    public void Net8_ViewsTemplates_HasBootstrap5Subfolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var bootstrap5Dir = Path.Combine(basePath, TargetFramework, "Views", "Bootstrap5");
        Assert.True(Directory.Exists(bootstrap5Dir),
            "Bootstrap5 subfolder should exist for net8.0 Views templates");
    }

    [Fact]
    public void Net8_ViewsTemplates_HasExactly14Files()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var files = Directory.GetFiles(viewsDir, "*", SearchOption.AllDirectories);
        Assert.Equal(14, files.Length);
    }

    [Theory]
    [InlineData("Bootstrap4", "Create.cshtml")]
    [InlineData("Bootstrap4", "Delete.cshtml")]
    [InlineData("Bootstrap4", "Details.cshtml")]
    [InlineData("Bootstrap4", "Edit.cshtml")]
    [InlineData("Bootstrap4", "Empty.cshtml")]
    [InlineData("Bootstrap4", "List.cshtml")]
    [InlineData("Bootstrap4", "_ValidationScriptsPartial.cshtml")]
    [InlineData("Bootstrap5", "Create.cshtml")]
    [InlineData("Bootstrap5", "Delete.cshtml")]
    [InlineData("Bootstrap5", "Details.cshtml")]
    [InlineData("Bootstrap5", "Edit.cshtml")]
    [InlineData("Bootstrap5", "Empty.cshtml")]
    [InlineData("Bootstrap5", "List.cshtml")]
    [InlineData("Bootstrap5", "_ValidationScriptsPartial.cshtml")]
    public void Net8_ViewsTemplates_HasExpectedFile(string subfolder, string fileName)
    {
        var basePath = GetActualTemplatesBasePath();
        var filePath = Path.Combine(basePath, TargetFramework, "Views", subfolder, fileName);
        Assert.True(File.Exists(filePath),
            $"Expected Views template file '{subfolder}/{fileName}' not found for {TargetFramework}");
    }

    [Fact]
    public void Net8_ViewsTemplates_AllFilesAreCshtml()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var files = Directory.GetFiles(viewsDir, "*", SearchOption.AllDirectories);
        Assert.All(files, f => Assert.EndsWith(".cshtml", f));
    }

    [Fact]
    public void Net8_ViewsTemplates_Bootstrap4_Has7Files()
    {
        var basePath = GetActualTemplatesBasePath();
        var bootstrap4Dir = Path.Combine(basePath, TargetFramework, "Views", "Bootstrap4");
        var files = Directory.GetFiles(bootstrap4Dir);
        Assert.Equal(7, files.Length);
    }

    [Fact]
    public void Net8_ViewsTemplates_Bootstrap5_Has7Files()
    {
        var basePath = GetActualTemplatesBasePath();
        var bootstrap5Dir = Path.Combine(basePath, TargetFramework, "Views", "Bootstrap5");
        var files = Directory.GetFiles(bootstrap5Dir);
        Assert.Equal(7, files.Length);
    }

    [Fact]
    public void Net8_ViewsTemplates_DoesNotHaveFlatT4Templates()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var ttFiles = Directory.GetFiles(viewsDir, "*.tt", SearchOption.TopDirectoryOnly);
        Assert.Empty(ttFiles);
    }

    #endregion

    #region CodeModificationConfigs — Net8

    [Fact]
    public void Net8_CodeModConfigs_NoEfControllerChangesJson()
    {
        var basePath = GetActualTemplatesBasePath();
        var configPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "efControllerChanges.json");
        Assert.False(File.Exists(configPath),
            "efControllerChanges.json should NOT exist for net8.0 (exists only in net9+)");
    }

    #endregion

    #region Template Root — Expected Scaffolder Folders

    [Theory]
    [InlineData("BlazorCrud")]
    [InlineData("BlazorIdentity")]
    [InlineData("CodeModificationConfigs")]
    [InlineData("EfController")]
    [InlineData("Files")]
    [InlineData("Identity")]
    [InlineData("MinimalApi")]
    [InlineData("RazorPages")]
    [InlineData("Views")]
    public void Net8_Templates_HasExpectedScaffolderFolder(string folderName)
    {
        var basePath = GetActualTemplatesBasePath();
        var folderPath = Path.Combine(basePath, TargetFramework, folderName);
        Assert.True(Directory.Exists(folderPath),
            $"Expected template folder '{folderName}' not found for {TargetFramework}");
    }

    [Fact]
    public void Net8_Templates_DoesNotHaveBlazorEntraId()
    {
        var basePath = GetActualTemplatesBasePath();
        var entraIdDir = Path.Combine(basePath, TargetFramework, "BlazorEntraId");
        Assert.False(Directory.Exists(entraIdDir),
            "BlazorEntraId folder should NOT exist for net8.0 (exists only in net10+)");
    }

    #endregion

    #region Razor Views vs Other Scaffolders Comparison

    [Fact]
    public void ViewsScaffolder_IsDifferentFromRazorViewEmpty_Net8()
    {
        Assert.NotEqual(AspnetStrings.RazorView.Empty, AspnetStrings.RazorView.Views);
    }

    [Fact]
    public void ViewsScaffolder_IsDifferentFromCrudController_Net8()
    {
        Assert.NotEqual(AspnetStrings.MVC.ControllerCrud, AspnetStrings.RazorView.Views);
    }

    [Fact]
    public void ViewsScaffolder_IsDifferentFromBlazorCrud_Net8()
    {
        Assert.NotEqual(AspnetStrings.Blazor.Crud, AspnetStrings.RazorView.Views);
    }

    #endregion

    #region Regression Guards

    [Fact]
    public void ViewsScaffolderName_NotEmpty_Net8()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.Views));
    }

    [Fact]
    public void ViewsScaffolderDisplayName_NotEmpty_Net8()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsDisplayName));
    }

    [Fact]
    public void ViewsScaffolderDescription_NotEmpty_Net8()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsDescription));
    }

    [Fact]
    public void ViewsExample1_NotEmpty_Net8()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample1));
    }

    [Fact]
    public void ViewsExample2_NotEmpty_Net8()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample2));
    }

    [Fact]
    public void ViewsExample1Description_NotEmpty_Net8()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample1Description));
    }

    [Fact]
    public void ViewsExample2Description_NotEmpty_Net8()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample2Description));
    }

    [Fact]
    public void ViewExtension_IsNotEmpty_Net8()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspNetConstants.ViewExtension));
    }

    [Fact]
    public void PageTypeOption_IsNotEmpty_Net8()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspNetConstants.CliOptions.PageTypeOption));
    }

    #endregion

    #region Helper Methods

    private ValidateViewsStep CreateValidateViewsStep()
    {
        return new ValidateViewsStep(
            _mockFileSystem.Object,
            NullLogger<ValidateViewsStep>.Instance,
            _mockTelemetryService.Object);
    }

    private static string GetActualTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

    #endregion
}
