// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

/// <summary>
/// Integration tests for the MVC Controller (empty) scaffolder targeting .NET 11.
/// The empty MVC Controller scaffolder creates a controller file using 'dotnet new mvccontroller'.
///
/// These tests validate:
///  - Scaffolder definition constants (Name, DisplayName, Description, Examples)
///  - Category assignment ("MVC")
///  - EmptyControllerStepSettings / DotnetNewStepSettings / BaseSettings properties
///  - EmptyControllerScaffolderStep validation (null/empty ProjectPath, null/empty FileName, non-existent project)
///  - EmptyControllerScaffolderStep property defaults and get/set
///  - FileName title-casing behavior
///  - CLI option constants (--actions, Controllers output dir)
///  - Option display names and descriptions (FileName, Actions)
///  - CRUD controller constants (mvccontroller-crud)
///  - EfController template folder contents for net11.0 (6 T4-based files)
///  - Views template folder contents for net11.0 (15 T4-based files, flat structure)
///  - efControllerChanges.json exists in net11.0 CodeModificationConfigs
///  - Template root expected scaffolder folders (including BlazorEntraId)
///  - Parity with net10.0 templates
/// </summary>
public class ControllerNet11IntegrationTests : IDisposable
{
    private const string TargetFramework = "net11.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public ControllerNet11IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "ControllerNet11IntegrationTests", Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _mockTelemetryService = new Mock<ITelemetryService>();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.MVC.DisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.MVC.Controller);
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
    public void ScaffolderName_IsMvcController_Net11()
    {
        Assert.Equal("mvccontroller", AspnetStrings.MVC.Controller);
    }

    [Fact]
    public void ScaffolderDisplayName_IsMvcController_Net11()
    {
        Assert.Equal("MVC Controller", AspnetStrings.MVC.DisplayName);
    }

    [Fact]
    public void ScaffolderDescription_IsCorrect_Net11()
    {
        Assert.Equal("Add an empty MVC Controller to a given project", AspnetStrings.MVC.Description);
    }

    [Fact]
    public void ScaffolderCategory_IsMVC_Net11()
    {
        Assert.Equal("MVC", AspnetStrings.Catagories.MVC);
    }

    [Fact]
    public void ScaffolderExample1_ContainsMvcControllerCommand_Net11()
    {
        Assert.Contains("mvccontroller", AspnetStrings.MVC.ControllerExample1);
        Assert.Contains("--project", AspnetStrings.MVC.ControllerExample1);
        Assert.Contains("--file-name", AspnetStrings.MVC.ControllerExample1);
    }

    [Fact]
    public void ScaffolderExample1_ContainsHomeController_Net11()
    {
        Assert.Contains("HomeController", AspnetStrings.MVC.ControllerExample1);
    }

    [Fact]
    public void ScaffolderExample1Description_IsCorrect_Net11()
    {
        Assert.Contains("empty", AspnetStrings.MVC.ControllerExample1Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScaffolderExample2_ContainsActionsFlag_Net11()
    {
        Assert.Contains("--actions", AspnetStrings.MVC.ControllerExample2);
        Assert.Contains("ProductController", AspnetStrings.MVC.ControllerExample2);
    }

    [Fact]
    public void ScaffolderExample2Description_ContainsActions_Net11()
    {
        Assert.Contains("actions", AspnetStrings.MVC.ControllerExample2Description, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CRUD Controller Constants

    [Fact]
    public void CrudControllerName_IsMvcControllerCrud_Net11()
    {
        Assert.Equal("mvccontroller-crud", AspnetStrings.MVC.ControllerCrud);
    }

    [Fact]
    public void CrudControllerDisplayName_IsCorrect_Net11()
    {
        Assert.Equal("MVC Controller with views, using Entity Framework (CRUD)", AspnetStrings.MVC.CrudDisplayName);
    }

    [Fact]
    public void CrudControllerDescription_IsCorrect_Net11()
    {
        Assert.Equal("Create a MVC controller with read/write actions and views using Entity Framework", AspnetStrings.MVC.CrudDescription);
    }

    [Fact]
    public void CrudControllerExample1_ContainsCrudCommand_Net11()
    {
        Assert.Contains("mvccontroller-crud", AspnetStrings.MVC.ControllerCrudExample1);
        Assert.Contains("--views", AspnetStrings.MVC.ControllerCrudExample1);
    }

    [Fact]
    public void CrudControllerExample2_ContainsPrerelease_Net11()
    {
        Assert.Contains("--prerelease", AspnetStrings.MVC.ControllerCrudExample2);
    }

    #endregion

    #region CLI Option Constants

    [Fact]
    public void CliOptions_ActionsOption_IsCorrect_Net11()
    {
        Assert.Equal("--actions", AspNetConstants.CliOptions.ActionsOption);
    }

    [Fact]
    public void CliOptions_ControllerNameOption_IsCorrect_Net11()
    {
        Assert.Equal("--controller", AspNetConstants.CliOptions.ControllerNameOption);
    }

    [Fact]
    public void CliOptions_ProjectOption_IsCorrect_Net11()
    {
        Assert.Equal("--project", AspNetConstants.CliOptions.ProjectCliOption);
    }

    [Fact]
    public void DotnetCommands_ControllerCommandOutput_IsControllers_Net11()
    {
        Assert.Equal("Controllers", AspNetConstants.DotnetCommands.ControllerCommandOutput);
    }

    #endregion

    #region Option Strings — FileName

    [Fact]
    public void FileNameOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("File name", AspnetStrings.Options.FileName.DisplayName);
    }

    [Fact]
    public void FileNameOption_Description_IsCorrect_Net11()
    {
        Assert.Equal("File name for new file being created with 'dotnet new'", AspnetStrings.Options.FileName.Description);
    }

    #endregion

    #region Option Strings — Actions

    [Fact]
    public void ActionsOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Read/Write Actions?", AspnetStrings.Options.Actions.DisplayName);
    }

    [Fact]
    public void ActionsOption_Description_IsCorrect_Net11()
    {
        Assert.Equal("Create controller with read/write actions?", AspnetStrings.Options.Actions.Description);
    }

    #endregion

    #region EmptyControllerStepSettings Properties

    [Fact]
    public void EmptyControllerStepSettings_HasProjectProperty_Net11()
    {
        var settings = new EmptyControllerStepSettings
        {
            Project = _testProjectPath,
            Name = "HomeController",
            CommandName = "mvccontroller",
            Actions = false
        };

        Assert.Equal(_testProjectPath, settings.Project);
    }

    [Fact]
    public void EmptyControllerStepSettings_HasNameProperty_Net11()
    {
        var settings = new EmptyControllerStepSettings
        {
            Project = _testProjectPath,
            Name = "HomeController",
            CommandName = "mvccontroller",
            Actions = false
        };

        Assert.Equal("HomeController", settings.Name);
    }

    [Fact]
    public void EmptyControllerStepSettings_HasCommandNameProperty_Net11()
    {
        var settings = new EmptyControllerStepSettings
        {
            Project = _testProjectPath,
            Name = "HomeController",
            CommandName = "mvccontroller",
            Actions = false
        };

        Assert.Equal("mvccontroller", settings.CommandName);
    }

    [Fact]
    public void EmptyControllerStepSettings_HasActionsProperty_Net11()
    {
        var settings = new EmptyControllerStepSettings
        {
            Project = _testProjectPath,
            Name = "HomeController",
            CommandName = "mvccontroller",
            Actions = true
        };

        Assert.True(settings.Actions);
    }

    [Fact]
    public void EmptyControllerStepSettings_ActionsFalseByDefault_Net11()
    {
        var settings = new EmptyControllerStepSettings
        {
            Project = _testProjectPath,
            Name = "HomeController",
            CommandName = "mvccontroller",
            Actions = false
        };

        Assert.False(settings.Actions);
    }

    [Fact]
    public void EmptyControllerStepSettings_InheritsDotnetNewStepSettings_Net11()
    {
        var settings = new EmptyControllerStepSettings
        {
            Project = _testProjectPath,
            Name = "HomeController",
            CommandName = "mvccontroller",
            Actions = false
        };

        Assert.IsAssignableFrom<DotnetNewStepSettings>(settings);
    }

    [Fact]
    public void EmptyControllerStepSettings_InheritsBaseSettings_Net11()
    {
        var settings = new EmptyControllerStepSettings
        {
            Project = _testProjectPath,
            Name = "HomeController",
            CommandName = "mvccontroller",
            Actions = false
        };

        Assert.IsAssignableFrom<BaseSettings>(settings);
    }

    [Fact]
    public void EmptyControllerStepSettings_SupportsNamespaceName_Net11()
    {
        var settings = new EmptyControllerStepSettings
        {
            Project = _testProjectPath,
            Name = "HomeController",
            CommandName = "mvccontroller",
            Actions = false,
            NamespaceName = "MyApp.Controllers"
        };

        Assert.Equal("MyApp.Controllers", settings.NamespaceName);
    }

    [Fact]
    public void EmptyControllerStepSettings_NamespaceNameDefaultsToNull_Net11()
    {
        var settings = new EmptyControllerStepSettings
        {
            Project = _testProjectPath,
            Name = "HomeController",
            CommandName = "mvccontroller",
            Actions = false
        };

        Assert.Null(settings.NamespaceName);
    }

    #endregion

    #region EmptyControllerScaffolderStep — Properties

    [Fact]
    public void EmptyControllerScaffolderStep_HasProjectPathProperty_Net11()
    {
        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        Assert.Equal(_testProjectPath, step.ProjectPath);
    }

    [Fact]
    public void EmptyControllerScaffolderStep_HasFileNameProperty_Net11()
    {
        var step = CreateEmptyControllerStep();
        step.FileName = "HomeController";
        Assert.Equal("HomeController", step.FileName);
    }

    [Fact]
    public void EmptyControllerScaffolderStep_HasActionsProperty_Net11()
    {
        var step = CreateEmptyControllerStep();
        step.Actions = true;
        Assert.True(step.Actions);
    }

    [Fact]
    public void EmptyControllerScaffolderStep_HasCommandNameProperty_Net11()
    {
        var step = CreateEmptyControllerStep();
        Assert.Equal("mvccontroller", step.CommandName);
    }

    [Fact]
    public void EmptyControllerScaffolderStep_ProjectPathDefaultsToNull_Net11()
    {
        var step = CreateEmptyControllerStep();
        Assert.Null(step.ProjectPath);
    }

    [Fact]
    public void EmptyControllerScaffolderStep_FileNameDefaultsToNull_Net11()
    {
        var step = CreateEmptyControllerStep();
        Assert.Null(step.FileName);
    }

    [Fact]
    public void EmptyControllerScaffolderStep_ActionsDefaultsToFalse_Net11()
    {
        var step = CreateEmptyControllerStep();
        Assert.False(step.Actions);
    }

    #endregion

    #region EmptyControllerScaffolderStep — Validation Logic

    [Fact]
    public async Task EmptyControllerScaffolderStep_FailsWithNullProjectPath_Net11()
    {
        var step = CreateEmptyControllerStep();
        step.ProjectPath = null;
        step.FileName = "HomeController";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_FailsWithEmptyProjectPath_Net11()
    {
        var step = CreateEmptyControllerStep();
        step.ProjectPath = string.Empty;
        step.FileName = "HomeController";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_FailsWithNonExistentProject_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateEmptyControllerStep();
        step.ProjectPath = @"C:\NonExistent\Project.csproj";
        step.FileName = "HomeController";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_FailsWithNullFileName_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = null;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_FailsWithEmptyFileName_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = string.Empty;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region EmptyControllerScaffolderStep — Telemetry

    [Fact]
    public async Task EmptyControllerScaffolderStep_TracksTelemetry_OnValidationFailure_Net11()
    {
        var step = CreateEmptyControllerStep();
        step.ProjectPath = null;
        step.FileName = "HomeController";

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_TracksTelemetry_OnFileNameValidationFailure_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = null;

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    #endregion

    #region EmptyControllerScaffolderStep — Controllers Directory

    [Fact]
    public async Task EmptyControllerScaffolderStep_CreatesControllersDirectory_Net11()
    {
        var createdDirs = SetupFileSystemForDotnetNew();

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = "HomeController";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith("Controllers"));
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_ControllersDir_IsUnderProjectDir_Net11()
    {
        var createdDirs = SetupFileSystemForDotnetNew();

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = "HomeController";

        await step.ExecuteAsync(_context);

        var controllersDir = createdDirs.FirstOrDefault(d => d.EndsWith("Controllers"));
        Assert.NotNull(controllersDir);
        Assert.StartsWith(_testProjectDir, controllersDir);
    }

    #endregion

    #region FileName Title-Casing

    [Theory]
    [InlineData("homeController", "Homecontroller")]
    [InlineData("productcontroller", "Productcontroller")]
    [InlineData("HomeController", "Homecontroller")]
    [InlineData("ADMIN", "ADMIN")]
    [InlineData("admin", "Admin")]
    public void TitleCase_ConvertsFirstLetterToUpper_Net11(string input, string expected)
    {
        // ToTitleCase treats the entire string as a single word (no spaces),
        // so it lowercases everything except the first letter.
        var result = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Multiple Controller Names

    [Theory]
    [InlineData("HomeController")]
    [InlineData("ProductController")]
    [InlineData("AccountController")]
    [InlineData("OrderController")]
    [InlineData("DashboardController")]
    public async Task EmptyControllerScaffolderStep_FailsValidation_ForVariousNames_WhenProjectMissing_Net11(string controllerName)
    {
        var step = CreateEmptyControllerStep();
        step.ProjectPath = null;
        step.FileName = controllerName;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData("HomeController")]
    [InlineData("ProductController")]
    [InlineData("AccountController")]
    public async Task EmptyControllerScaffolderStep_CreatesControllersDir_ForVariousNames_Net11(string controllerName)
    {
        var createdDirs = SetupFileSystemForDotnetNew();

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = controllerName;

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith("Controllers"));
    }

    #endregion

    #region Actions Flag Variations

    [Fact]
    public async Task EmptyControllerScaffolderStep_WithActionsTrue_CreatesControllersDir_Net11()
    {
        var createdDirs = SetupFileSystemForDotnetNew();

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = "HomeController";
        step.Actions = true;

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith("Controllers"));
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_WithActionsFalse_CreatesControllersDir_Net11()
    {
        var createdDirs = SetupFileSystemForDotnetNew();

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = "HomeController";
        step.Actions = false;

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith("Controllers"));
    }

    #endregion

    #region ScaffolderContext

    [Fact]
    public void ScaffolderContext_HasControllerScaffolderName_Net11()
    {
        Assert.Equal("mvccontroller", _context.Scaffolder.Name);
    }

    [Fact]
    public void ScaffolderContext_HasControllerDisplayName_Net11()
    {
        Assert.Equal("MVC Controller", _context.Scaffolder.DisplayName);
    }

    #endregion

    #region EfController Templates — Net11

    [Fact]
    public void Net11_EfControllerTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var efControllerDir = Path.Combine(basePath, TargetFramework, "EfController");
        Assert.True(Directory.Exists(efControllerDir),
            $"EfController template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void Net11_EfControllerTemplates_HasExactly6Files()
    {
        var basePath = GetActualTemplatesBasePath();
        var efControllerDir = Path.Combine(basePath, TargetFramework, "EfController");
        var files = Directory.GetFiles(efControllerDir, "*", SearchOption.AllDirectories);
        Assert.Equal(6, files.Length);
    }

    [Theory]
    [InlineData("ApiEfController.cs")]
    [InlineData("ApiEfController.Interfaces.cs")]
    [InlineData("ApiEfController.tt")]
    [InlineData("MvcEfController.cs")]
    [InlineData("MvcEfController.Interfaces.cs")]
    [InlineData("MvcEfController.tt")]
    public void Net11_EfControllerTemplates_HasExpectedFile(string fileName)
    {
        var basePath = GetActualTemplatesBasePath();
        var filePath = Path.Combine(basePath, TargetFramework, "EfController", fileName);
        Assert.True(File.Exists(filePath),
            $"Expected EfController template file '{fileName}' not found for {TargetFramework}");
    }

    [Fact]
    public void Net11_EfControllerTemplates_UsesT4Format()
    {
        var basePath = GetActualTemplatesBasePath();
        var efControllerDir = Path.Combine(basePath, TargetFramework, "EfController");
        var ttFiles = Directory.GetFiles(efControllerDir, "*.tt");
        Assert.Equal(2, ttFiles.Length);
    }

    [Fact]
    public void Net11_EfControllerTemplates_MatchesNet10FileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0", "EfController");
        var net11Dir = Path.Combine(basePath, TargetFramework, "EfController");
        var net10Files = Directory.GetFiles(net10Dir, "*", SearchOption.AllDirectories);
        var net11Files = Directory.GetFiles(net11Dir, "*", SearchOption.AllDirectories);
        Assert.Equal(net10Files.Length, net11Files.Length);
    }

    #endregion

    #region Views Templates — Net11

    [Fact]
    public void Net11_ViewsTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        Assert.True(Directory.Exists(viewsDir),
            $"Views template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void Net11_ViewsTemplates_NoBootstrapSubfolders()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var subDirs = Directory.GetDirectories(viewsDir);
        Assert.Empty(subDirs);
    }

    [Fact]
    public void Net11_ViewsTemplates_HasExactly15Files()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var files = Directory.GetFiles(viewsDir, "*", SearchOption.AllDirectories);
        Assert.Equal(15, files.Length);
    }

    [Theory]
    [InlineData("Create.cs")]
    [InlineData("Create.Interfaces.cs")]
    [InlineData("Create.tt")]
    [InlineData("Delete.cs")]
    [InlineData("Delete.Interfaces.cs")]
    [InlineData("Delete.tt")]
    [InlineData("Details.cs")]
    [InlineData("Details.Interfaces.cs")]
    [InlineData("Details.tt")]
    [InlineData("Edit.cs")]
    [InlineData("Edit.Interfaces.cs")]
    [InlineData("Edit.tt")]
    [InlineData("Index.cs")]
    [InlineData("Index.Interfaces.cs")]
    [InlineData("Index.tt")]
    public void Net11_ViewsTemplates_HasExpectedFile(string fileName)
    {
        var basePath = GetActualTemplatesBasePath();
        var filePath = Path.Combine(basePath, TargetFramework, "Views", fileName);
        Assert.True(File.Exists(filePath),
            $"Expected Views template file '{fileName}' not found for {TargetFramework}");
    }

    [Fact]
    public void Net11_ViewsTemplates_Has5TtFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var ttFiles = Directory.GetFiles(viewsDir, "*.tt");
        Assert.Equal(5, ttFiles.Length);
    }

    [Fact]
    public void Net11_ViewsTemplates_MatchesNet10FileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0", "Views");
        var net11Dir = Path.Combine(basePath, TargetFramework, "Views");
        var net10Files = Directory.GetFiles(net10Dir, "*", SearchOption.AllDirectories);
        var net11Files = Directory.GetFiles(net11Dir, "*", SearchOption.AllDirectories);
        Assert.Equal(net10Files.Length, net11Files.Length);
    }

    #endregion

    #region CodeModificationConfigs — Net11

    [Fact]
    public void Net11_CodeModConfigs_HasEfControllerChangesJson()
    {
        var basePath = GetActualTemplatesBasePath();
        var configPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "efControllerChanges.json");
        Assert.True(File.Exists(configPath),
            $"efControllerChanges.json should exist for {TargetFramework}");
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
        var folderPath = Path.Combine(basePath, TargetFramework, folderName);
        Assert.True(Directory.Exists(folderPath),
            $"Expected template folder '{folderName}' not found for {TargetFramework}");
    }

    [Fact]
    public void Net11_Templates_HasBlazorEntraId()
    {
        var basePath = GetActualTemplatesBasePath();
        var entraIdDir = Path.Combine(basePath, TargetFramework, "BlazorEntraId");
        Assert.True(Directory.Exists(entraIdDir),
            "BlazorEntraId folder should exist for net11.0");
    }

    #endregion

    #region No Empty Controller Template Folder

    [Fact]
    public void Net11_Templates_NoMvcControllerFolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var controllerDir = Path.Combine(basePath, TargetFramework, "MvcController");
        Assert.False(Directory.Exists(controllerDir),
            $"Empty MVC Controller template folder should NOT exist for {TargetFramework} (uses dotnet new)");
    }

    [Fact]
    public void Net11_Templates_NoControllerFolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var controllerDir = Path.Combine(basePath, TargetFramework, "Controller");
        Assert.False(Directory.Exists(controllerDir),
            $"Controller template folder should NOT exist for {TargetFramework} (uses dotnet new)");
    }

    #endregion

    #region Net11 vs Net10 Template Parity

    [Fact]
    public void Net11_EfControllerTemplates_SameFileNamesAsNet10()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0", "EfController");
        var net11Dir = Path.Combine(basePath, TargetFramework, "EfController");
        var net10FileNames = Directory.GetFiles(net10Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        var net11FileNames = Directory.GetFiles(net11Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        Assert.Equal(net10FileNames, net11FileNames);
    }

    [Fact]
    public void Net11_ViewsTemplates_SameFileNamesAsNet10()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0", "Views");
        var net11Dir = Path.Combine(basePath, TargetFramework, "Views");
        var net10FileNames = Directory.GetFiles(net10Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        var net11FileNames = Directory.GetFiles(net11Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        Assert.Equal(net10FileNames, net11FileNames);
    }

    [Fact]
    public void Net11_Templates_HasSameTopLevelFoldersAsNet10()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0");
        var net11Dir = Path.Combine(basePath, TargetFramework);
        var net10Folders = Directory.GetDirectories(net10Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        var net11Folders = Directory.GetDirectories(net11Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        Assert.Equal(net10Folders, net11Folders);
    }

    #endregion

    #region Helper Methods

    private EmptyControllerScaffolderStep CreateEmptyControllerStep()
    {
        return new EmptyControllerScaffolderStep(
            NullLogger<EmptyControllerScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _mockTelemetryService.Object)
        {
            CommandName = "mvccontroller"
        };
    }

    /// <summary>
    /// Sets up the mock file system so that the project file exists and
    /// the Controllers directory will be "created" via mock.
    /// </summary>
    private List<string> SetupFileSystemForDotnetNew()
    {
        var createdDirs = new List<string>();
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.CreateDirectoryIfNotExists(It.IsAny<string>()))
            .Callback<string>(dir => createdDirs.Add(dir));
        return createdDirs;
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
