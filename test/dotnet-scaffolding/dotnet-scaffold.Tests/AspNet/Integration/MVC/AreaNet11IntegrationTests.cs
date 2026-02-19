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
using Microsoft.DotNet.Tools.Scaffold.AspNet;
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
/// Integration tests for the MVC Area scaffolder targeting .NET 11.
/// The Area scaffolder creates a directory structure (Areas/{Name}/Controllers,
/// Models, Data, Views) with no templates, no NuGet packages, and no code modifications.
///
/// These tests validate:
///  - Scaffolder definition constants (Name, DisplayName, Description, Example)
///  - Category assignment ("MVC")
///  - AreaStepSettings / BaseSettings properties
///  - AreaScaffolderStep validation (null/empty Name, null/empty/non-existent Project)
///  - AreaScaffolderStep directory creation behavior
///  - The 4 expected subfolders (Controllers, Models, Data, Views)
///  - CLI option constants
///  - That no Area template folder exists in the net11.0 templates
///  - That the net11.0 template root has the expected scaffolder folders (including BlazorEntraId)
///  - That net11.0 template root matches net10.0
/// </summary>
public class AreaNet11IntegrationTests : IDisposable
{
    private const string TargetFramework = "net11.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IEnvironmentService> _mockEnvironmentService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public AreaNet11IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "AreaNet11IntegrationTests", Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _mockEnvironmentService = new Mock<IEnvironmentService>();
        _mockEnvironmentService.Setup(e => e.CurrentDirectory).Returns(_testProjectDir);

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.Area.DisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.Area.Name);
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
    public void ScaffolderName_IsArea_Net11()
    {
        Assert.Equal("area", AspnetStrings.Area.Name);
    }

    [Fact]
    public void ScaffolderDisplayName_IsArea_Net11()
    {
        Assert.Equal("Area", AspnetStrings.Area.DisplayName);
    }

    [Fact]
    public void ScaffolderDescription_IsCorrect_Net11()
    {
        Assert.Equal("Creates a MVC Area folder structure.", AspnetStrings.Area.Description);
    }

    [Fact]
    public void ScaffolderCategory_IsMVC_Net11()
    {
        Assert.Equal("MVC", AspnetStrings.Catagories.MVC);
    }

    [Fact]
    public void ScaffolderExample_ContainsAreaCommand_Net11()
    {
        Assert.Contains("area", AspnetStrings.Area.AreaExample);
        Assert.Contains("--project", AspnetStrings.Area.AreaExample);
        Assert.Contains("--area-name", AspnetStrings.Area.AreaExample);
    }

    [Fact]
    public void ScaffolderExample_ContainsAdminExample_Net11()
    {
        Assert.Contains("Admin", AspnetStrings.Area.AreaExample);
    }

    [Fact]
    public void ScaffolderExampleDescription_IsCorrect_Net11()
    {
        Assert.Contains("area", AspnetStrings.Area.AreaExampleDescription, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CLI Option Constants

    [Fact]
    public void CliOptions_NameOption_IsCorrect_Net11()
    {
        Assert.Equal("--name", AspNetConstants.CliOptions.NameOption);
    }

    [Fact]
    public void CliOptions_ProjectOption_IsCorrect_Net11()
    {
        Assert.Equal("--project", AspNetConstants.CliOptions.ProjectCliOption);
    }

    #endregion

    #region AreaName Option Strings

    [Fact]
    public void AreaNameOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Area Name", AspnetStrings.Options.AreaName.DisplayName);
    }

    [Fact]
    public void AreaNameOption_Description_IsCorrect_Net11()
    {
        Assert.Equal("Name for the area being created", AspnetStrings.Options.AreaName.Description);
    }

    #endregion

    #region AreaStepSettings Properties

    [Fact]
    public void AreaStepSettings_HasProjectProperty_Net11()
    {
        var settings = new AreaStepSettings
        {
            Project = _testProjectPath,
            Name = "Admin"
        };

        Assert.Equal(_testProjectPath, settings.Project);
    }

    [Fact]
    public void AreaStepSettings_HasNameProperty_Net11()
    {
        var settings = new AreaStepSettings
        {
            Project = _testProjectPath,
            Name = "Admin"
        };

        Assert.Equal("Admin", settings.Name);
    }

    [Fact]
    public void AreaStepSettings_InheritsBaseSettings_Net11()
    {
        var settings = new AreaStepSettings
        {
            Project = _testProjectPath,
            Name = "TestArea"
        };

        Assert.IsAssignableFrom<BaseSettings>(settings);
    }

    #endregion

    #region AreaScaffolderStep — Validation Logic

    [Fact]
    public async Task AreaScaffolderStep_FailsWithNullName_Net11()
    {
        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = null;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task AreaScaffolderStep_FailsWithEmptyName_Net11()
    {
        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = string.Empty;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task AreaScaffolderStep_FailsWithNullProject_Net11()
    {
        var step = CreateAreaScaffolderStep();
        step.Project = null;
        step.Name = "Admin";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task AreaScaffolderStep_FailsWithEmptyProject_Net11()
    {
        var step = CreateAreaScaffolderStep();
        step.Project = string.Empty;
        step.Name = "Admin";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task AreaScaffolderStep_FailsWithNonExistentProject_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateAreaScaffolderStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Name = "Admin";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region AreaScaffolderStep — Properties

    [Fact]
    public void AreaScaffolderStep_HasProjectProperty_Net11()
    {
        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        Assert.Equal(_testProjectPath, step.Project);
    }

    [Fact]
    public void AreaScaffolderStep_HasNameProperty_Net11()
    {
        var step = CreateAreaScaffolderStep();
        step.Name = "Admin";
        Assert.Equal("Admin", step.Name);
    }

    [Fact]
    public void AreaScaffolderStep_ProjectDefaultsToNull_Net11()
    {
        var step = CreateAreaScaffolderStep();
        Assert.Null(step.Project);
    }

    [Fact]
    public void AreaScaffolderStep_NameDefaultsToNull_Net11()
    {
        var step = CreateAreaScaffolderStep();
        Assert.Null(step.Name);
    }

    #endregion

    #region AreaScaffolderStep — Directory Creation

    [Fact]
    public async Task AreaScaffolderStep_CreatesAreasDirectory_Net11()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith("Areas"));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesNamedAreaDirectory_Net11()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith(Path.Combine("Areas", "Admin")));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesControllersFolder_Net11()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith(Path.Combine("Admin", "Controllers")));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesModelsFolder_Net11()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith(Path.Combine("Admin", "Models")));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesDataFolder_Net11()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith(Path.Combine("Admin", "Data")));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesViewsFolder_Net11()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith(Path.Combine("Admin", "Views")));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesExactly6Directories_Net11()
    {
        // Areas + Areas/Name + Controllers + Models + Data + Views = 6
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "TestArea";

        await step.ExecuteAsync(_context);

        Assert.Equal(6, createdDirs.Count);
    }

    [Fact]
    public async Task AreaScaffolderStep_ReturnsTrue_OnSuccess_Net11()
    {
        SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        var result = await step.ExecuteAsync(_context);
        Assert.True(result);
    }

    [Fact]
    public async Task AreaScaffolderStep_UsesProjectDirectory_WhenExists_Net11()
    {
        var createdDirs = SetupFileSystemForSuccess();
        _mockFileSystem.Setup(fs => fs.DirectoryExists(Path.GetDirectoryName(_testProjectPath)!)).Returns(true);

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Sales";

        await step.ExecuteAsync(_context);

        var areasDir = createdDirs.First(d => d.EndsWith("Areas") && !d.Contains("Sales"));
        Assert.StartsWith(Path.GetDirectoryName(_testProjectPath)!, areasDir);
    }

    [Fact]
    public async Task AreaScaffolderStep_FallsBackToCurrentDirectory_WhenProjectDirMissing_Net11()
    {
        var createdDirs = SetupFileSystemForSuccess();
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);
        _mockEnvironmentService.Setup(e => e.CurrentDirectory).Returns(@"C:\FallbackDir");

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Reports";

        await step.ExecuteAsync(_context);

        var areasDir = createdDirs.First(d => d.EndsWith("Areas") && !d.Contains("Reports"));
        Assert.StartsWith(@"C:\FallbackDir", areasDir);
    }

    [Fact]
    public async Task AreaScaffolderStep_SupportsCustomAreaName_Net11()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "MyCustomArea";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.Contains("MyCustomArea"));
    }

    #endregion

    #region No Area Templates for Net11

    [Fact]
    public void Net11_Templates_NoAreaFolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var areaDir = Path.Combine(basePath, TargetFramework, "Area");
        Assert.False(Directory.Exists(areaDir),
            $"Area template folder should NOT exist for {TargetFramework} (Area scaffolder creates directories only)");
    }

    [Fact]
    public void Net11_Templates_NoAreasFolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var areasDir = Path.Combine(basePath, TargetFramework, "Areas");
        Assert.False(Directory.Exists(areasDir),
            $"Areas template folder should NOT exist for {TargetFramework}");
    }

    #endregion

    #region Net11 Template Root — Expected Scaffolder Folders

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

    #region Net11 Template Root — Matches Net10

    [Fact]
    public void Net11_Templates_HasSameTopLevelFoldersAsNet10()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0");
        var net11Dir = Path.Combine(basePath, TargetFramework);
        if (!Directory.Exists(net10Dir) || !Directory.Exists(net11Dir)) return;

        var net10Folders = Directory.GetDirectories(net10Dir)
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .ToList();
        var net11Folders = Directory.GetDirectories(net11Dir)
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .ToList();

        Assert.Equal(net10Folders, net11Folders);
    }

    #endregion

    #region Multiple Area Names

    [Theory]
    [InlineData("Admin")]
    [InlineData("Blog")]
    [InlineData("Dashboard")]
    [InlineData("Api")]
    [InlineData("Reporting")]
    public async Task AreaScaffolderStep_CreatesCorrectStructure_ForVariousNames_Net11(string areaName)
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = areaName;

        var result = await step.ExecuteAsync(_context);

        Assert.True(result);
        Assert.Contains(createdDirs, d => d.Contains(Path.Combine("Areas", areaName)));
        Assert.Contains(createdDirs, d => d.Contains(Path.Combine(areaName, "Controllers")));
        Assert.Contains(createdDirs, d => d.Contains(Path.Combine(areaName, "Models")));
        Assert.Contains(createdDirs, d => d.Contains(Path.Combine(areaName, "Data")));
        Assert.Contains(createdDirs, d => d.Contains(Path.Combine(areaName, "Views")));
    }

    #endregion

    #region ScaffolderContext

    [Fact]
    public void ScaffolderContext_HasAreaScaffolderName_Net11()
    {
        Assert.Equal("area", _context.Scaffolder.Name);
    }

    [Fact]
    public void ScaffolderContext_HasAreaDisplayName_Net11()
    {
        Assert.Equal("Area", _context.Scaffolder.DisplayName);
    }

    #endregion

    #region Helper Methods

    private AreaScaffolderStep CreateAreaScaffolderStep()
    {
        return new AreaScaffolderStep(
            _mockFileSystem.Object,
            NullLogger<AreaScaffolderStep>.Instance,
            _mockEnvironmentService.Object);
    }

    /// <summary>
    /// Sets up the mock file system to simulate a valid project and captures directory creation calls.
    /// Returns the list of directories that were "created".
    /// </summary>
    private List<string> SetupFileSystemForSuccess()
    {
        var createdDirs = new List<string>();
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(Path.GetDirectoryName(_testProjectPath)!)).Returns(true);
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
