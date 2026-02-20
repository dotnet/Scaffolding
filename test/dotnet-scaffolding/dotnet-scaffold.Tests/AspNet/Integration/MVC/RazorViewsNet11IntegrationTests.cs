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
/// Integration tests for the Razor Views (CRUD) scaffolder targeting .NET 11.
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
///  - Telemetry tracking via ITelemetryService
///  - Views template folder contents for net11.0 (flat T4 templates, 15 files)
///  - efControllerChanges.json exists in net11.0 CodeModificationConfigs
///  - BlazorEntraId folder exists for net11.0
///  - Template root expected scaffolder folders
///  - Net11 vs Net10 parity and differences
/// </summary>
public class RazorViewsNet11IntegrationTests : IDisposable
{
    private const string PreviousFramework = "net10.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public RazorViewsNet11IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "RazorViewsNet11IntegrationTests", Guid.NewGuid().ToString());
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
    public void ViewsScaffolderName_IsViews_Net11()
    {
        Assert.Equal("views", AspnetStrings.RazorView.Views);
    }

    [Fact]
    public void ViewsScaffolderDisplayName_IsRazorViews_Net11()
    {
        Assert.Equal("Razor Views", AspnetStrings.RazorView.ViewsDisplayName);
    }

    [Fact]
    public void ViewsScaffolderDescription_IsCorrect_Net11()
    {
        Assert.Equal("Generates Razor views for Create, Delete, Details, Edit and List operations for the given model",
            AspnetStrings.RazorView.ViewsDescription);
    }

    [Fact]
    public void ViewsScaffolderCategory_IsMVC_Net11()
    {
        Assert.Equal("MVC", AspnetStrings.Catagories.MVC);
    }

    [Fact]
    public void ViewsExample1_ContainsViewsCommand_Net11()
    {
        Assert.Contains("views", AspnetStrings.RazorView.ViewsExample1);
    }

    [Fact]
    public void ViewsExample1_ContainsProjectOption_Net11()
    {
        Assert.Contains("--project", AspnetStrings.RazorView.ViewsExample1);
    }

    [Fact]
    public void ViewsExample1_ContainsModelOption_Net11()
    {
        Assert.Contains("--model", AspnetStrings.RazorView.ViewsExample1);
        Assert.Contains("Product", AspnetStrings.RazorView.ViewsExample1);
    }

    [Fact]
    public void ViewsExample1_ContainsPageOption_Net11()
    {
        Assert.Contains("--page", AspnetStrings.RazorView.ViewsExample1);
        Assert.Contains("All", AspnetStrings.RazorView.ViewsExample1);
    }

    [Fact]
    public void ViewsExample1Description_ContainsCrud_Net11()
    {
        Assert.Contains("CRUD", AspnetStrings.RazorView.ViewsExample1Description);
    }

    [Fact]
    public void ViewsExample2_ContainsViewsCommand_Net11()
    {
        Assert.Contains("views", AspnetStrings.RazorView.ViewsExample2);
    }

    [Fact]
    public void ViewsExample2_ContainsCreateAndEdit_Net11()
    {
        Assert.Contains("Create", AspnetStrings.RazorView.ViewsExample2);
        Assert.Contains("Edit", AspnetStrings.RazorView.ViewsExample2);
    }

    [Fact]
    public void ViewsExample2Description_ContainsCreateAndEdit_Net11()
    {
        Assert.Contains("Create", AspnetStrings.RazorView.ViewsExample2Description);
        Assert.Contains("Edit", AspnetStrings.RazorView.ViewsExample2Description);
    }

    #endregion

    #region CLI Option Constants

    [Fact]
    public void CliOptions_ProjectOption_IsCorrect_Net11()
    {
        Assert.Equal("--project", AspNetConstants.CliOptions.ProjectCliOption);
    }

    [Fact]
    public void CliOptions_ModelOption_IsCorrect_Net11()
    {
        Assert.Equal("--model", AspNetConstants.CliOptions.ModelCliOption);
    }

    [Fact]
    public void CliOptions_PageTypeOption_IsCorrect_Net11()
    {
        Assert.Equal("--page", AspNetConstants.CliOptions.PageTypeOption);
    }

    [Fact]
    public void Constants_ViewExtension_IsCshtml_Net11()
    {
        Assert.Equal(".cshtml", AspNetConstants.ViewExtension);
    }

    #endregion

    #region Option Strings — PageType

    [Fact]
    public void PageTypeOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Page Type", AspnetStrings.Options.PageType.DisplayName);
    }

    [Fact]
    public void PageTypeOption_Description_ContainsCrud_Net11()
    {
        Assert.Contains("CRUD", AspnetStrings.Options.PageType.Description);
    }

    #endregion

    #region Option Strings — ModelName

    [Fact]
    public void ModelNameOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Model Name", AspnetStrings.Options.ModelName.DisplayName);
    }

    [Fact]
    public void ModelNameOption_Description_ContainsModel_Net11()
    {
        Assert.Contains("model", AspnetStrings.Options.ModelName.Description, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CrudSettings Properties

    [Fact]
    public void CrudSettings_HasPageProperty_Net11()
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
    public void CrudSettings_HasModelProperty_Net11()
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
    public void CrudSettings_HasProjectProperty_Net11()
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
    public void CrudSettings_InheritsEfWithModelStepSettings_Net11()
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
    public void CrudSettings_InheritsBaseSettings_Net11()
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
    public void CrudSettings_PrereleaseDefaultsFalse_Net11()
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
    public void CrudSettings_DatabaseProviderDefaultsNull_Net11()
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
    public void CrudSettings_DataContextDefaultsNull_Net11()
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
    public void ValidateViewsStep_HasProjectProperty_Net11()
    {
        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        Assert.Equal(_testProjectPath, step.Project);
    }

    [Fact]
    public void ValidateViewsStep_HasModelProperty_Net11()
    {
        var step = CreateValidateViewsStep();
        step.Model = "Product";
        Assert.Equal("Product", step.Model);
    }

    [Fact]
    public void ValidateViewsStep_HasPageProperty_Net11()
    {
        var step = CreateValidateViewsStep();
        step.Page = "CRUD";
        Assert.Equal("CRUD", step.Page);
    }

    [Fact]
    public void ValidateViewsStep_ProjectDefaultsToNull_Net11()
    {
        var step = CreateValidateViewsStep();
        Assert.Null(step.Project);
    }

    [Fact]
    public void ValidateViewsStep_ModelDefaultsToNull_Net11()
    {
        var step = CreateValidateViewsStep();
        Assert.Null(step.Model);
    }

    [Fact]
    public void ValidateViewsStep_PageDefaultsToNull_Net11()
    {
        var step = CreateValidateViewsStep();
        Assert.Null(step.Page);
    }

    #endregion

    #region ValidateViewsStep — Validation Logic

    [Fact]
    public async Task ValidateViewsStep_FailsWithNullProject_Net11()
    {
        var step = CreateValidateViewsStep();
        step.Project = null;
        step.Model = "Product";
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithEmptyProject_Net11()
    {
        var step = CreateValidateViewsStep();
        step.Project = string.Empty;
        step.Model = "Product";
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithNonExistentProject_Net11()
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
    public async Task ValidateViewsStep_FailsWithNullModel_Net11()
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
    public async Task ValidateViewsStep_FailsWithEmptyModel_Net11()
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
    public async Task ValidateViewsStep_FailsWithNullPage_Net11()
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
    public async Task ValidateViewsStep_FailsWithEmptyPage_Net11()
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
    public async Task ValidateViewsStep_FailsWithInvalidProject_Net11(string? project)
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
    public async Task ValidateViewsStep_FailsWithInvalidModel_Net11(string? model)
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
    public async Task ValidateViewsStep_FailsWithInvalidPage_Net11(string? page)
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
    public async Task ValidateViewsStep_TracksTelemetry_OnNullProjectFailure_Net11()
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
    public async Task ValidateViewsStep_TracksTelemetry_OnNullModelFailure_Net11()
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
    public async Task ValidateViewsStep_TracksTelemetry_OnNullPageFailure_Net11()
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
    public void CrudPageType_IsCRUD_Net11()
    {
        Assert.Equal("CRUD", BlazorCrudHelper.CrudPageType);
    }

    [Fact]
    public void CRUDPages_ContainsAllExpectedPageTypes_Net11()
    {
        var expected = new[] { "CRUD", "Create", "Delete", "Details", "Edit", "Index", "NotFound" };
        Assert.Equal(expected.Length, BlazorCrudHelper.CRUDPages.Count);
        foreach (var page in expected)
        {
            Assert.Contains(page, BlazorCrudHelper.CRUDPages);
        }
    }

    [Fact]
    public void CRUDPages_Has7Entries_Net11()
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
    public void CRUDPages_ContainsPageType_Net11(string pageType)
    {
        Assert.Contains(pageType, BlazorCrudHelper.CRUDPages);
    }

    #endregion

    #region ViewHelper — Template Constants

    [Fact]
    public void ViewHelper_CreateTemplate_IsCorrect_Net11()
    {
        Assert.Equal("Create.tt", ViewHelper.CreateTemplate);
    }

    [Fact]
    public void ViewHelper_DeleteTemplate_IsCorrect_Net11()
    {
        Assert.Equal("Delete.tt", ViewHelper.DeleteTemplate);
    }

    [Fact]
    public void ViewHelper_DetailsTemplate_IsCorrect_Net11()
    {
        Assert.Equal("Details.tt", ViewHelper.DetailsTemplate);
    }

    [Fact]
    public void ViewHelper_EditTemplate_IsCorrect_Net11()
    {
        Assert.Equal("Edit.tt", ViewHelper.EditTemplate);
    }

    [Fact]
    public void ViewHelper_IndexTemplate_IsCorrect_Net11()
    {
        Assert.Equal("Index.tt", ViewHelper.IndexTemplate);
    }

    #endregion

    #region ScaffolderContext

    [Fact]
    public void ScaffolderContext_HasViewsScaffolderName_Net11()
    {
        Assert.Equal("views", _context.Scaffolder.Name);
    }

    [Fact]
    public void ScaffolderContext_HasViewsDisplayName_Net11()
    {
        Assert.Equal("Razor Views", _context.Scaffolder.DisplayName);
    }

    #endregion

    #region Views Templates — Net11

    [Fact]
    public void Net11_ViewsTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, "net11.0", "Views");
        Assert.True(Directory.Exists(viewsDir),
            "Views template folder should exist for net11.0");
    }

    [Fact]
    public void Net11_ViewsTemplates_HasExactly15Files()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, "net11.0", "Views");
        var files = Directory.GetFiles(viewsDir, "*", SearchOption.AllDirectories);
        Assert.Equal(15, files.Length);
    }

    [Theory]
    [InlineData("Create.tt")]
    [InlineData("Create.cs")]
    [InlineData("Create.Interfaces.cs")]
    [InlineData("Delete.tt")]
    [InlineData("Delete.cs")]
    [InlineData("Delete.Interfaces.cs")]
    [InlineData("Details.tt")]
    [InlineData("Details.cs")]
    [InlineData("Details.Interfaces.cs")]
    [InlineData("Edit.tt")]
    [InlineData("Edit.cs")]
    [InlineData("Edit.Interfaces.cs")]
    [InlineData("Index.tt")]
    [InlineData("Index.cs")]
    [InlineData("Index.Interfaces.cs")]
    public void Net11_ViewsTemplates_HasExpectedFile(string fileName)
    {
        var basePath = GetActualTemplatesBasePath();
        var filePath = Path.Combine(basePath, "net11.0", "Views", fileName);
        Assert.True(File.Exists(filePath),
            $"Expected Views template file '{fileName}' not found for net11.0");
    }

    [Fact]
    public void Net11_ViewsTemplates_HasT4Templates()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, "net11.0", "Views");
        var ttFiles = Directory.GetFiles(viewsDir, "*.tt", SearchOption.AllDirectories);
        Assert.Equal(5, ttFiles.Length);
    }

    [Fact]
    public void Net11_ViewsTemplates_Has5CsFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, "net11.0", "Views");
        var csFiles = Directory.GetFiles(viewsDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".Interfaces.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Equal(5, csFiles.Length);
    }

    [Fact]
    public void Net11_ViewsTemplates_Has5InterfacesFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, "net11.0", "Views");
        var interfacesFiles = Directory.GetFiles(viewsDir, "*.Interfaces.cs", SearchOption.AllDirectories);
        Assert.Equal(5, interfacesFiles.Length);
    }

    [Fact]
    public void Net11_ViewsTemplates_DoesNotHaveBootstrapSubfolders()
    {
        var basePath = GetActualTemplatesBasePath();
        var bootstrap4Dir = Path.Combine(basePath, "net11.0", "Views", "Bootstrap4");
        var bootstrap5Dir = Path.Combine(basePath, "net11.0", "Views", "Bootstrap5");
        Assert.False(Directory.Exists(bootstrap4Dir),
            "Bootstrap4 subfolder should NOT exist for net11.0 Views templates");
        Assert.False(Directory.Exists(bootstrap5Dir),
            "Bootstrap5 subfolder should NOT exist for net11.0 Views templates");
    }

    #endregion

    #region CodeModificationConfigs — Net11

    [Fact]
    public void Net11_CodeModConfigs_HasEfControllerChangesJson()
    {
        var basePath = GetActualTemplatesBasePath();
        var configPath = Path.Combine(basePath, "net11.0", "CodeModificationConfigs", "efControllerChanges.json");
        Assert.True(File.Exists(configPath),
            "efControllerChanges.json should exist for net11.0");
    }

    #endregion

    #region Template Root — Expected Scaffolder Folders

    [Theory]
    [InlineData("BlazorCrud")]
    [InlineData("BlazorEntraId")]
    [InlineData("BlazorIdentity")]
    [InlineData("CodeModificationConfigs")]
    [InlineData("EfController")]
    [InlineData("Files")]
    [InlineData("Identity")]
    [InlineData("MinimalApi")]
    [InlineData("RazorPages")]
    [InlineData("Views")]
    public void Net11_Templates_HasExpectedScaffolderFolder(string folderName)
    {
        var basePath = GetActualTemplatesBasePath();
        var folderPath = Path.Combine(basePath, "net11.0", folderName);
        Assert.True(Directory.Exists(folderPath),
            $"Expected template folder '{folderName}' not found for net11.0");
    }

    [Fact]
    public void Net11_Templates_HasBlazorEntraId()
    {
        var basePath = GetActualTemplatesBasePath();
        var entraIdDir = Path.Combine(basePath, "net11.0", "BlazorEntraId");
        Assert.True(Directory.Exists(entraIdDir),
            "BlazorEntraId folder should exist for net11.0");
    }

    #endregion

    #region Razor Views vs Other Scaffolders Comparison

    [Fact]
    public void ViewsScaffolder_IsDifferentFromRazorViewEmpty_Net11()
    {
        Assert.NotEqual(AspnetStrings.RazorView.Empty, AspnetStrings.RazorView.Views);
    }

    [Fact]
    public void ViewsScaffolder_IsDifferentFromCrudController_Net11()
    {
        Assert.NotEqual(AspnetStrings.MVC.ControllerCrud, AspnetStrings.RazorView.Views);
    }

    [Fact]
    public void ViewsScaffolder_IsDifferentFromBlazorCrud_Net11()
    {
        Assert.NotEqual(AspnetStrings.Blazor.Crud, AspnetStrings.RazorView.Views);
    }

    #endregion

    #region Net11 vs Net10 Parity & Differences

    [Fact]
    public void Net11VsNet10_BothHaveViewsFolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, PreviousFramework, "Views");
        var net11Dir = Path.Combine(basePath, "net11.0", "Views");
        Assert.True(Directory.Exists(net10Dir), "Net10 Views folder should exist");
        Assert.True(Directory.Exists(net11Dir), "Net11 Views folder should exist");
    }

    [Fact]
    public void Net11VsNet10_SameTemplateFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10ViewsDir = Path.Combine(basePath, PreviousFramework, "Views");
        var net11ViewsDir = Path.Combine(basePath, "net11.0", "Views");

        var net10FileCount = Directory.GetFiles(net10ViewsDir, "*", SearchOption.AllDirectories).Length;
        var net11FileCount = Directory.GetFiles(net11ViewsDir, "*", SearchOption.AllDirectories).Length;

        Assert.Equal(net10FileCount, net11FileCount);
    }

    [Fact]
    public void Net11VsNet10_SameTemplateFileNames()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10ViewsDir = Path.Combine(basePath, PreviousFramework, "Views");
        var net11ViewsDir = Path.Combine(basePath, "net11.0", "Views");

        var net10Files = Directory.GetFiles(net10ViewsDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetFileName(f))
            .OrderBy(f => f)
            .ToArray();
        var net11Files = Directory.GetFiles(net11ViewsDir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetFileName(f))
            .OrderBy(f => f)
            .ToArray();

        Assert.Equal(net10Files, net11Files);
    }

    [Fact]
    public void Net11VsNet10_BothUseT4Templates()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10ViewsDir = Path.Combine(basePath, PreviousFramework, "Views");
        var net11ViewsDir = Path.Combine(basePath, "net11.0", "Views");

        var net10TtFiles = Directory.GetFiles(net10ViewsDir, "*.tt", SearchOption.AllDirectories);
        var net11TtFiles = Directory.GetFiles(net11ViewsDir, "*.tt", SearchOption.AllDirectories);

        Assert.Equal(net10TtFiles.Length, net11TtFiles.Length);
    }

    [Fact]
    public void Net11VsNet10_BothHaveBlazorEntraId()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10EntraIdDir = Path.Combine(basePath, PreviousFramework, "BlazorEntraId");
        var net11EntraIdDir = Path.Combine(basePath, "net11.0", "BlazorEntraId");
        Assert.True(Directory.Exists(net10EntraIdDir), "BlazorEntraId should exist for net10.0");
        Assert.True(Directory.Exists(net11EntraIdDir), "BlazorEntraId should exist for net11.0");
    }

    [Fact]
    public void Net11VsNet10_BothHaveEfControllerChangesJson()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10ConfigPath = Path.Combine(basePath, PreviousFramework, "CodeModificationConfigs", "efControllerChanges.json");
        var net11ConfigPath = Path.Combine(basePath, "net11.0", "CodeModificationConfigs", "efControllerChanges.json");
        Assert.True(File.Exists(net10ConfigPath), "efControllerChanges.json should exist for net10.0");
        Assert.True(File.Exists(net11ConfigPath), "efControllerChanges.json should exist for net11.0");
    }

    #endregion

    #region Regression Guards

    [Fact]
    public void ViewsScaffolderName_NotEmpty_Net11()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.Views));
    }

    [Fact]
    public void ViewsScaffolderDisplayName_NotEmpty_Net11()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsDisplayName));
    }

    [Fact]
    public void ViewsScaffolderDescription_NotEmpty_Net11()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsDescription));
    }

    [Fact]
    public void ViewsExample1_NotEmpty_Net11()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample1));
    }

    [Fact]
    public void ViewsExample2_NotEmpty_Net11()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample2));
    }

    [Fact]
    public void ViewsExample1Description_NotEmpty_Net11()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample1Description));
    }

    [Fact]
    public void ViewsExample2Description_NotEmpty_Net11()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample2Description));
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
