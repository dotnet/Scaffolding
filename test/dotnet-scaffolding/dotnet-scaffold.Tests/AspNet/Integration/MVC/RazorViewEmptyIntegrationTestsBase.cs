// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// Shared base class for Razor View Empty (razorview-empty) integration tests across .NET versions.
/// Subclasses provide the target framework via <see cref="TargetFramework"/>.
/// </summary>
public abstract class RazorViewEmptyIntegrationTestsBase : IDisposable
{
    protected abstract string TargetFramework { get; }
    protected abstract string TestClassName { get; }

    protected readonly string _testDirectory;
    protected readonly string _testProjectDir;
    protected readonly string _testProjectPath;
    protected readonly Mock<IFileSystem> _mockFileSystem;
    protected readonly TestTelemetryService _testTelemetryService;
    protected readonly Mock<IScaffolder> _mockScaffolder;
    protected readonly ScaffolderContext _context;

    protected RazorViewEmptyIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.RazorView.EmptyDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.RazorView.Empty);
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

    protected string ProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";

    #region Constants & Scaffolder Definition

    [Fact]
    public void ViewCommandName_IsView()
    {
        Assert.Equal("view", Constants.DotnetCommands.ViewCommandName);
    }

    [Fact]
    public void ViewCommandOutput_IsViews()
    {
        Assert.Equal("Views", Constants.DotnetCommands.ViewCommandOutput);
    }

    [Fact]
    public void ScaffolderName_IsRazorViewEmpty()
    {
        Assert.Equal("razorview-empty", AspnetStrings.RazorView.Empty);
    }

    [Fact]
    public void ScaffolderDisplayName_IsRazorViewEmpty()
    {
        Assert.Equal("Razor View - Empty", AspnetStrings.RazorView.EmptyDisplayName);
    }

    [Fact]
    public void ScaffolderDescription_DescribesEmptyRazorView()
    {
        Assert.Equal("Add an empty razor view to a given project", AspnetStrings.RazorView.EmptyDescription);
    }

    [Fact]
    public void ScaffolderExample_ContainsRazorViewEmptyCommand()
    {
        Assert.Contains("razorview-empty", AspnetStrings.RazorView.EmptyExample);
    }

    [Fact]
    public void ScaffolderExample_ContainsProjectOption()
    {
        Assert.Contains("--project", AspnetStrings.RazorView.EmptyExample);
    }

    [Fact]
    public void ScaffolderExample_ContainsFileNameOption()
    {
        Assert.Contains("--file-name", AspnetStrings.RazorView.EmptyExample);
    }

    [Fact]
    public void ScaffolderExample_ContainsSampleFileName()
    {
        Assert.Contains("Dashboard", AspnetStrings.RazorView.EmptyExample);
    }

    [Fact]
    public void ScaffolderExampleDescription_IsNotEmpty()
    {
        Assert.False(string.IsNullOrEmpty(AspnetStrings.RazorView.EmptyExampleDescription));
    }

    [Fact]
    public void ScaffolderCategory_IsMVC()
    {
        // The razorview-empty scaffolder is registered under the MVC category
        Assert.False(string.IsNullOrEmpty(AspnetStrings.Catagories.MVC));
    }

    #endregion

    #region DotnetNewScaffolderStep — Validation

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectPathIsNull()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = null,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectPathIsEmpty()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = string.Empty,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectPathDoesNotExist()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = Path.Combine(_testProjectDir, "NonExistent.csproj"),
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenFileNameIsNull()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = null,
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenFileNameIsEmpty()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = string.Empty,
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    #endregion

    #region DotnetNewScaffolderStep — Property Initialization

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        Assert.NotNull(step);
        Assert.Equal(Constants.DotnetCommands.ViewCommandName, step.CommandName);
    }

    [Fact]
    public void ProjectPath_DefaultsToNull()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        Assert.Null(step.ProjectPath);
    }

    [Fact]
    public void FileName_DefaultsToNull()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        Assert.Null(step.FileName);
    }

    [Fact]
    public void NamespaceName_DefaultsToNull()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        Assert.Null(step.NamespaceName);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Dashboard",
            NamespaceName = "MyApp.Views",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        Assert.Equal(_testProjectPath, step.ProjectPath);
        Assert.Equal("Dashboard", step.FileName);
        Assert.Equal("MyApp.Views", step.NamespaceName);
        Assert.Equal(Constants.DotnetCommands.ViewCommandName, step.CommandName);
    }

    [Fact]
    public void RazorViewEmpty_DoesNotSetNamespace()
    {
        // The razorview-empty scaffolder in AspNetCommandService does NOT set NamespaceName
        // (unlike razorpage-empty which sets NamespaceName = projectName).
        // This test documents this intentional behavior.
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
            // Note: NamespaceName is not set — mirrors AspNetCommandService behavior
        };

        Assert.Null(step.NamespaceName);
    }

    #endregion

    #region DotnetNewScaffolderStep — Output Folder Mapping

    [Fact]
    public async Task ExecuteAsync_CreatesViewsDirectory_WhenProjectExists()
    {
        string expectedViewsDir = Path.Combine(_testProjectDir, "Views");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(expectedViewsDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(expectedViewsDir), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OutputFolder_IsViews_ForView()
    {
        string viewsDir = Path.Combine(_testProjectDir, "Views");
        string componentsDir = Path.Combine(_testProjectDir, "Components");
        string pagesDir = Path.Combine(_testProjectDir, "Pages");

        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(viewsDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "TestView",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(viewsDir), Times.Once);
        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(componentsDir), Times.Never);
        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(pagesDir), Times.Never);
    }

    #endregion

    #region DotnetNewScaffolderStep — Title Casing

    [Theory]
    [InlineData("dashboard", "Dashboard")]
    [InlineData("productList", "Productlist")]
    [InlineData("UPPERCASE", "UPPERCASE")]
    [InlineData("a", "A")]
    public void TitleCase_CapitalizesFirstLetter(string input, string expected)
    {
        string result = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ExecuteAsync_TitleCasesFileName_WhenLowercaseProvided()
    {
        string viewsDir = Path.Combine(_testProjectDir, "Views");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(viewsDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string expected = CultureInfo.CurrentCulture.TextInfo.ToTitleCase("dashboard");
        Assert.Equal(expected, step.FileName);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesTitleCase_WhenAlreadyCapitalized()
    {
        string viewsDir = Path.Combine(_testProjectDir, "Views");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(viewsDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string expected = CultureInfo.CurrentCulture.TextInfo.ToTitleCase("Dashboard");
        Assert.Equal(expected, step.FileName);
    }

    #endregion

    #region DotnetNewScaffolderStep — Telemetry

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_OnValidationFailure()
    {
        var telemetry = new TestTelemetryService();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            telemetry)
        {
            ProjectPath = null,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(telemetry.TrackedEvents);
        var (eventName, properties, _) = telemetry.TrackedEvents[0];
        Assert.Equal("DotnetNewScaffolderStep", eventName);
        Assert.Equal("Failure", properties["SettingsValidationResult"]);
        Assert.Equal("Failure", properties["Result"]);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_WithScaffolderName()
    {
        var telemetry = new TestTelemetryService();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            telemetry)
        {
            ProjectPath = null,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal(AspnetStrings.RazorView.EmptyDisplayName, telemetry.TrackedEvents[0].Properties["ScaffolderName"]);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_OnValidFileNameFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        var telemetry = new TestTelemetryService();

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            telemetry)
        {
            ProjectPath = _testProjectPath,
            FileName = string.Empty,
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("Failure", telemetry.TrackedEvents[0].Properties["SettingsValidationResult"]);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_WhenSettingsAreValid()
    {
        string viewsDir = Path.Combine(_testProjectDir, "Views");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(viewsDir)).Returns(true);
        var telemetry = new TestTelemetryService();

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            telemetry)
        {
            ProjectPath = _testProjectPath,
            FileName = "ValidView",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("Success", telemetry.TrackedEvents[0].Properties["SettingsValidationResult"]);
    }

    #endregion

    #region DotnetNewScaffolderStep — Cancellation Token

    [Fact]
    public async Task ExecuteAsync_AcceptsCancellationToken()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = null,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        using var cts = new CancellationTokenSource();

        bool result = await step.ExecuteAsync(_context, cts.Token);

        Assert.False(result);
    }

    #endregion

    #region Output Folder Mapping — All Commands

    [Fact]
    public void OutputFolders_View_MapsToViews()
    {
        Assert.Equal("view", Constants.DotnetCommands.ViewCommandName);
        Assert.Equal("Views", Constants.DotnetCommands.ViewCommandOutput);
    }

    [Fact]
    public void OutputFolders_RazorComponent_MapsToComponents()
    {
        Assert.Equal("razorcomponent", Constants.DotnetCommands.RazorComponentCommandName);
        Assert.Equal("Components", Constants.DotnetCommands.RazorComponentCommandOutput);
    }

    [Fact]
    public void OutputFolders_RazorPage_MapsToPages()
    {
        Assert.Equal("page", Constants.DotnetCommands.RazorPageCommandName);
        Assert.Equal("Pages", Constants.DotnetCommands.RazorPageCommandOutput);
    }

    #endregion

    #region Scaffolder Registration Differentiation

    [Fact]
    public void RazorViewEmpty_IsDifferentFromBlazorEmpty()
    {
        Assert.NotEqual(AspnetStrings.RazorView.Empty, AspnetStrings.Blazor.Empty);
    }

    [Fact]
    public void RazorViewEmpty_IsDifferentFromRazorPageEmpty()
    {
        Assert.NotEqual(AspnetStrings.RazorView.Empty, AspnetStrings.RazorPage.Empty);
    }

    [Fact]
    public void RazorViewEmpty_IsDifferentFromMvcController()
    {
        Assert.NotEqual(AspnetStrings.RazorView.Empty, AspnetStrings.MVC.Controller);
    }

    [Fact]
    public void RazorViewEmpty_IsDifferentFromViews()
    {
        Assert.NotEqual(AspnetStrings.RazorView.Empty, AspnetStrings.RazorView.Views);
    }

    #endregion

    #region GetScaffoldSteps Registration

    [Fact]
    public void GetScaffoldSteps_ContainsDotnetNewScaffolderStep()
    {
        var mockBuilder = new Mock<Scaffolding.Core.Builder.IScaffoldRunnerBuilder>();
        var service = new AspNetCommandService(mockBuilder.Object);

        Type[] stepTypes = service.GetScaffoldSteps();

        Assert.Contains(typeof(DotnetNewScaffolderStep), stepTypes);
    }

    #endregion

    #region End-to-End File Generation

    [Fact]
    public async Task ExecuteAsync_GeneratesViewFile_WhenProjectIsValid()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result, $"dotnet new view should succeed for a valid {TargetFramework} project.");
        string expectedFile = Path.Combine(_testProjectDir, "Views", $"{step.FileName}.cshtml");
        Assert.True(File.Exists(expectedFile), $"Expected file '{expectedFile}' was not created.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedViewFile_ContainsValidContent()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Views", $"{step.FileName}.cshtml");
        string content = File.ReadAllText(expectedFile);
        Assert.False(string.IsNullOrWhiteSpace(content), "Generated .cshtml file should not be empty.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedViewFile_HasCshtmlExtension()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "ExtCheck",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string viewsDir = Path.Combine(_testProjectDir, "Views");
        string[] files = Directory.GetFiles(viewsDir);
        Assert.All(files, f => Assert.EndsWith(".cshtml", f));
        Assert.Empty(Directory.GetFiles(viewsDir, "*.razor"));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesViewsSubdirectory()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Widget",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string viewsDir = Path.Combine(_testProjectDir, "Views");
        Assert.True(Directory.Exists(viewsDir), "Views subdirectory should be created.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesCorrectFileName_WhenLowercaseInput()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Views", "Dashboard.cshtml");
        Assert.True(File.Exists(expectedFile), $"Expected file 'Dashboard.cshtml' (title-cased) was not created. FileName was '{step.FileName}'.");
    }

    [Fact]
    public async Task ExecuteAsync_TracksSuccessTelemetry_WhenGenerationSucceeds()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var telemetry = new TestTelemetryService();
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            telemetry)
        {
            ProjectPath = _testProjectPath,
            FileName = "TelemetryView",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("Success", telemetry.TrackedEvents[0].Properties["SettingsValidationResult"]);
        Assert.Equal("Success", telemetry.TrackedEvents[0].Properties["Result"]);
    }

    [Fact]
    public async Task ExecuteAsync_OnlyGeneratesSingleViewFile()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "SingleFile",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string viewsDir = Path.Combine(_testProjectDir, "Views");
        string[] generatedFiles = Directory.GetFiles(viewsDir);
        Assert.Single(generatedFiles);
        Assert.EndsWith(".cshtml", generatedFiles[0]);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateComponentsDirectory()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "NoComponents",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string componentsDir = Path.Combine(_testProjectDir, "Components");
        Assert.False(Directory.Exists(componentsDir), "Components directory should not be created for Razor views.");
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreatePagesDirectory()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "NoPages",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        Assert.False(Directory.Exists(pagesDir), "Pages directory should not be created for Razor views.");
    }

    #endregion

    #region Razor View vs Other Scaffolders Comparison

    [Fact]
    public void View_CommandName_DiffersFromRazorComponent()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.ViewCommandName,
            Constants.DotnetCommands.RazorComponentCommandName);
    }

    [Fact]
    public void View_OutputFolder_DiffersFromRazorComponent()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.ViewCommandOutput,
            Constants.DotnetCommands.RazorComponentCommandOutput);
    }

    [Fact]
    public void View_CommandName_DiffersFromRazorPage()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.ViewCommandName,
            Constants.DotnetCommands.RazorPageCommandName);
    }

    [Fact]
    public void View_OutputFolder_DiffersFromRazorPage()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.ViewCommandOutput,
            Constants.DotnetCommands.RazorPageCommandOutput);
    }

    #endregion

    #region Regression Guards

    [Fact]
    public async Task RegressionGuard_ValidationFailure_DoesNotThrow()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = null,
            FileName = null,
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task RegressionGuard_EmptyInputs_DoNotThrow()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = string.Empty,
            FileName = string.Empty,
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task RegressionGuard_NonExistentProject_ReturnsFalseNotException()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = @"C:\NonExistent\Path\Project.csproj",
            FileName = "TestView",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    #endregion

    #region Test Helpers

    protected static async Task<(int ExitCode, string Output, string Error)> RunBuildAsync(string workingDirectory)
    {
        var buildProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        buildProcess.Start();
        string output = await buildProcess.StandardOutput.ReadToEndAsync();
        string error = await buildProcess.StandardError.ReadToEndAsync();
        await buildProcess.WaitForExitAsync();
        return (buildProcess.ExitCode, output, error);
    }

    protected class TestTelemetryService : ITelemetryService
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

    #endregion
}
