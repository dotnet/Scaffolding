// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

/// <summary>
/// Integration tests for the Razor Component (blazor-empty) scaffolder targeting .NET 10.
/// Validates DotnetNewScaffolderStep validation logic, output folder mapping, title casing,
/// scaffolder definition, and end-to-end file generation via 'dotnet new razorcomponent'.
/// </summary>
public class RazorComponentNet10IntegrationTests : IDisposable
{
    private const string TargetFramework = "net10.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public RazorComponentNet10IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "RazorComponentNet10IntegrationTests", Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns("Razor Component");
        _mockScaffolder.Setup(s => s.Name).Returns("blazor-empty");
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
    public void RazorComponentCommandName_IsRazorComponent()
    {
        Assert.Equal("razorcomponent", Constants.DotnetCommands.RazorComponentCommandName);
    }

    [Fact]
    public void RazorComponentCommandOutput_IsComponents()
    {
        Assert.Equal("Components", Constants.DotnetCommands.RazorComponentCommandOutput);
    }

    [Fact]
    public void ScaffolderName_IsBlazorEmpty()
    {
        Assert.Equal("blazor-empty", AspnetStrings.Blazor.Empty);
    }

    [Fact]
    public void ScaffolderDisplayName_IsRazorComponent()
    {
        Assert.Equal("Razor Component", AspnetStrings.Blazor.EmptyDisplayName);
    }

    [Fact]
    public void ScaffolderDescription_DescribesEmptyRazorComponent()
    {
        Assert.Equal("Add an empty razor component to a given project", AspnetStrings.Blazor.EmptyDescription);
    }

    [Fact]
    public void ScaffolderExample_ContainsBlazorEmptyCommand()
    {
        Assert.Contains("blazor-empty", AspnetStrings.Blazor.EmptyExample);
    }

    [Fact]
    public void ScaffolderExample_ContainsProjectOption()
    {
        Assert.Contains("--project", AspnetStrings.Blazor.EmptyExample);
    }

    [Fact]
    public void ScaffolderExample_ContainsFileNameOption()
    {
        Assert.Contains("--file-name", AspnetStrings.Blazor.EmptyExample);
    }

    [Fact]
    public void ScaffolderExample_ContainsSampleFileName()
    {
        Assert.Contains("ProductCard", AspnetStrings.Blazor.EmptyExample);
    }

    [Fact]
    public void ScaffolderExampleDescription_IsNotEmpty()
    {
        Assert.False(string.IsNullOrEmpty(AspnetStrings.Blazor.EmptyExampleDescription));
    }

    #endregion

    #region DotnetNewScaffolderStep — Validation

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectPathIsNull()
    {
        // Arrange
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = null,
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectPathIsEmpty()
    {
        // Arrange
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = string.Empty,
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectPathDoesNotExist()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = Path.Combine(_testProjectDir, "NonExistent.csproj"),
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenFileNameIsNull()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = null,
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenFileNameIsEmpty()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = string.Empty,
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DotnetNewScaffolderStep — Property Initialization

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Act
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Assert
        Assert.NotNull(step);
        Assert.Equal(Constants.DotnetCommands.RazorComponentCommandName, step.CommandName);
    }

    [Fact]
    public void ProjectPath_DefaultsToNull()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        Assert.Null(step.NamespaceName);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange & Act
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "ProductCard",
            NamespaceName = "MyApp.Components",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Assert
        Assert.Equal(_testProjectPath, step.ProjectPath);
        Assert.Equal("ProductCard", step.FileName);
        Assert.Equal("MyApp.Components", step.NamespaceName);
        Assert.Equal(Constants.DotnetCommands.RazorComponentCommandName, step.CommandName);
    }

    [Fact]
    public void RazorComponent_DoesNotSetNamespace()
    {
        // The blazor-empty scaffolder in AspNetCommandService does NOT set NamespaceName
        // (unlike razorpage-empty which sets NamespaceName = projectName).
        // This test documents this intentional behavior.
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
            // Note: NamespaceName is not set — mirrors AspNetCommandService behavior
        };

        Assert.Null(step.NamespaceName);
    }

    #endregion

    #region DotnetNewScaffolderStep — Output Folder Mapping

    [Fact]
    public async Task ExecuteAsync_CreatesComponentsDirectory_WhenProjectExists()
    {
        // Arrange
        string expectedComponentsDir = Path.Combine(_testProjectDir, "Components");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(expectedComponentsDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act — the step will try to run 'dotnet new' which may fail, but the directory creation should happen
        await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert — verify CreateDirectoryIfNotExists was called for the Components folder
        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(expectedComponentsDir), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OutputFolder_IsComponents_ForRazorComponent()
    {
        // Arrange — verify the output folder is "Components" (not "Pages" or "Views")
        string componentsDir = Path.Combine(_testProjectDir, "Components");
        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        string viewsDir = Path.Combine(_testProjectDir, "Views");

        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(componentsDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "TestComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        // Components directory should be created
        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(componentsDir), Times.Once);
        // Pages and Views should NOT be created
        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(pagesDir), Times.Never);
        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(viewsDir), Times.Never);
    }

    #endregion

    #region DotnetNewScaffolderStep — Title Casing

    [Theory]
    [InlineData("product", "Product")]
    [InlineData("productCard", "Productcard")]
    [InlineData("UPPERCASE", "UPPERCASE")]
    [InlineData("a", "A")]
    public void TitleCase_CapitalizesFirstLetter(string input, string expected)
    {
        // The step uses CultureInfo.CurrentCulture.TextInfo.ToTitleCase to capitalize the first letter
        string result = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ExecuteAsync_TitleCasesFileName_WhenLowercaseProvided()
    {
        // Arrange
        string componentsDir = Path.Combine(_testProjectDir, "Components");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(componentsDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "myComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert — after validation, FileName should be title-cased
        string expected = CultureInfo.CurrentCulture.TextInfo.ToTitleCase("myComponent");
        Assert.Equal(expected, step.FileName);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesTitleCase_WhenAlreadyCapitalized()
    {
        // Arrange
        string componentsDir = Path.Combine(_testProjectDir, "Components");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(componentsDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "ProductCard",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert — ToTitleCase treats "ProductCard" as a single word, lowering inner capitalization
        string expected = CultureInfo.CurrentCulture.TextInfo.ToTitleCase("ProductCard");
        Assert.Equal(expected, step.FileName);
    }

    #endregion

    #region DotnetNewScaffolderStep — Telemetry

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_OnValidationFailure()
    {
        // Arrange
        var telemetry = new TestTelemetryService();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            telemetry)
        {
            ProjectPath = null,
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert — telemetry event should have been tracked
        Assert.Single(telemetry.TrackedEvents);
        var (eventName, properties, _) = telemetry.TrackedEvents[0];
        Assert.Equal("DotnetNewScaffolderStep", eventName);
        Assert.Equal("Failure", properties["SettingsValidationResult"]);
        Assert.Equal("Failure", properties["Result"]);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_WithScaffolderName()
    {
        // Arrange
        var telemetry = new TestTelemetryService();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            telemetry)
        {
            ProjectPath = null,
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("Razor Component", telemetry.TrackedEvents[0].Properties["ScaffolderName"]);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_OnValidFileNameFailure()
    {
        // Arrange — project exists but FileName is empty → validation failure
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        var telemetry = new TestTelemetryService();

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            telemetry)
        {
            ProjectPath = _testProjectPath,
            FileName = string.Empty,
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("Failure", telemetry.TrackedEvents[0].Properties["SettingsValidationResult"]);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_WhenSettingsAreValid()
    {
        // Arrange — project exists, fileName is valid but dotnet new will fail (no real project)
        string componentsDir = Path.Combine(_testProjectDir, "Components");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(componentsDir)).Returns(true);
        var telemetry = new TestTelemetryService();

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            telemetry)
        {
            ProjectPath = _testProjectPath,
            FileName = "ValidComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert — settings validated OK, but dotnet new may fail
        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("Success", telemetry.TrackedEvents[0].Properties["SettingsValidationResult"]);
    }

    #endregion

    #region DotnetNewScaffolderStep — Cancellation Token

    [Fact]
    public async Task ExecuteAsync_AcceptsCancellationToken()
    {
        // Arrange
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = null,
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        using var cts = new CancellationTokenSource();

        // Act — should not throw even with a cancellation token
        bool result = await step.ExecuteAsync(_context, cts.Token);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Output Folder Mapping — All Commands

    [Fact]
    public void OutputFolders_RazorComponent_MapsToComponents()
    {
        // Verify the constant mapping: razorcomponent → Components
        Assert.Equal("razorcomponent", Constants.DotnetCommands.RazorComponentCommandName);
        Assert.Equal("Components", Constants.DotnetCommands.RazorComponentCommandOutput);
    }

    [Fact]
    public void OutputFolders_RazorPage_MapsToPages()
    {
        // Contrast: razorpage maps to Pages
        Assert.Equal("page", Constants.DotnetCommands.RazorPageCommandName);
        Assert.Equal("Pages", Constants.DotnetCommands.RazorPageCommandOutput);
    }

    [Fact]
    public void OutputFolders_View_MapsToViews()
    {
        // Contrast: view maps to Views
        Assert.Equal("view", Constants.DotnetCommands.ViewCommandName);
        Assert.Equal("Views", Constants.DotnetCommands.ViewCommandOutput);
    }

    #endregion

    #region Scaffolder Registration Differentiation

    [Fact]
    public void BlazorEmpty_IsDifferentFromBlazorIdentity()
    {
        Assert.NotEqual(AspnetStrings.Blazor.Empty, AspnetStrings.Blazor.Identity);
    }

    [Fact]
    public void BlazorEmpty_IsDifferentFromBlazorCrud()
    {
        Assert.NotEqual(AspnetStrings.Blazor.Empty, AspnetStrings.Blazor.Crud);
    }

    [Fact]
    public void BlazorEmpty_IsDifferentFromRazorPageEmpty()
    {
        Assert.NotEqual(AspnetStrings.Blazor.Empty, AspnetStrings.RazorPage.Empty);
    }

    [Fact]
    public void BlazorEmpty_IsDifferentFromRazorViewEmpty()
    {
        Assert.NotEqual(AspnetStrings.Blazor.Empty, AspnetStrings.RazorView.Empty);
    }

    #endregion

    #region GetScaffoldSteps Registration

    [Fact]
    public void GetScaffoldSteps_ContainsDotnetNewScaffolderStep()
    {
        // Arrange
        var mockBuilder = new Mock<Scaffolding.Core.Builder.IScaffoldRunnerBuilder>();
        var service = new AspNetCommandService(mockBuilder.Object);

        // Act
        Type[] stepTypes = service.GetScaffoldSteps();

        // Assert — DotnetNewScaffolderStep should be registered
        Assert.Contains(typeof(DotnetNewScaffolderStep), stepTypes);
    }

    #endregion

    #region End-to-End File Generation (net10.0)

    [Fact]
    public async Task ExecuteAsync_GeneratesRazorFile_WhenNet10ProjectIsValid()
    {
        // Arrange — create a real minimal .NET 10 project on disk for dotnet new
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        // Use a real file system for end-to-end
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "ProductCard",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result, $"dotnet new razorcomponent should succeed for a valid {TargetFramework} project.");
        string expectedFile = Path.Combine(_testProjectDir, "Components", $"{step.FileName}.razor");
        Assert.True(File.Exists(expectedFile), $"Expected file '{expectedFile}' was not created.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedRazorFile_ContainsValidContent_Net10()
    {
        // Arrange
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "ProductCard",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Components", $"{step.FileName}.razor");
        string content = File.ReadAllText(expectedFile);
        Assert.False(string.IsNullOrWhiteSpace(content), "Generated .razor file should not be empty.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedRazorFile_ContainsH3Heading_Net10()
    {
        // Arrange — the default razorcomponent template typically includes an <h3> heading
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "HeadingComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Components", $"{step.FileName}.razor");
        string content = File.ReadAllText(expectedFile);
        Assert.Contains("<h3>", content);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedRazorFile_ContainsCodeBlock_Net10()
    {
        // Arrange — the default razorcomponent template contains an @code block
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "CodeBlockComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Components", $"{step.FileName}.razor");
        string content = File.ReadAllText(expectedFile);
        Assert.Contains("@code", content);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedRazorFile_DoesNotContainPageDirective_Net10()
    {
        // Arrange — a Razor component (not a page) should NOT contain @page
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "NonPageComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Components", $"{step.FileName}.razor");
        string content = File.ReadAllText(expectedFile);
        Assert.DoesNotContain("@page", content);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesComponentsSubdirectory_Net10()
    {
        // Arrange
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Widget",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        string componentsDir = Path.Combine(_testProjectDir, "Components");
        Assert.True(Directory.Exists(componentsDir), "Components subdirectory should be created.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesCorrectFileName_WhenLowercaseInput_Net10()
    {
        // Arrange — 'widget' should be title-cased to 'Widget'
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "widget",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Components", "Widget.razor");
        Assert.True(File.Exists(expectedFile), $"Expected file 'Widget.razor' (title-cased) was not created. FileName was '{step.FileName}'.");
    }

    [Fact]
    public async Task ExecuteAsync_TracksSuccessTelemetry_WhenNet10GenerationSucceeds()
    {
        // Arrange
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var telemetry = new TestTelemetryService();
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            telemetry)
        {
            ProjectPath = _testProjectPath,
            FileName = "TelemetryComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("Success", telemetry.TrackedEvents[0].Properties["SettingsValidationResult"]);
        Assert.Equal("Success", telemetry.TrackedEvents[0].Properties["Result"]);
    }

    [Fact]
    public async Task ExecuteAsync_OnlyGeneratesSingleRazorFile_Net10()
    {
        // Arrange — ensure only one .razor file is created (no .cs code-behind, no .css)
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "SingleFile",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        string componentsDir = Path.Combine(_testProjectDir, "Components");
        string[] generatedFiles = Directory.GetFiles(componentsDir);
        Assert.Single(generatedFiles);
        Assert.EndsWith(".razor", generatedFiles[0]);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreatePagesDirectory_Net10()
    {
        // Arrange — Razor component should create Components, not Pages
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "NoPages",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        Assert.False(Directory.Exists(pagesDir), "Pages directory should not be created for Razor components.");
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateViewsDirectory_Net10()
    {
        // Arrange — Razor component should create Components, not Views
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "NoViews",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        string viewsDir = Path.Combine(_testProjectDir, "Views");
        Assert.False(Directory.Exists(viewsDir), "Views directory should not be created for Razor components.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedFile_HasRazorExtension_Net10()
    {
        // Arrange — verify the generated file has .razor extension (not .cshtml or .cs)
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "ExtensionCheck",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
        string componentsDir = Path.Combine(_testProjectDir, "Components");
        string[] files = Directory.GetFiles(componentsDir);
        Assert.All(files, f => Assert.EndsWith(".razor", f));
        // No .cshtml files should be generated
        Assert.Empty(Directory.GetFiles(componentsDir, "*.cshtml"));
        // No .cs files should be generated
        Assert.Empty(Directory.GetFiles(componentsDir, "*.cs"));
    }

    #endregion

    #region Razor Component vs Razor Page Comparison

    [Fact]
    public void RazorComponent_CommandName_DiffersFromRazorPage()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.RazorComponentCommandName,
            Constants.DotnetCommands.RazorPageCommandName);
    }

    [Fact]
    public void RazorComponent_OutputFolder_DiffersFromRazorPage()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.RazorComponentCommandOutput,
            Constants.DotnetCommands.RazorPageCommandOutput);
    }

    [Fact]
    public void RazorComponent_CommandName_DiffersFromView()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.RazorComponentCommandName,
            Constants.DotnetCommands.ViewCommandName);
    }

    [Fact]
    public void RazorComponent_OutputFolder_DiffersFromView()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.RazorComponentCommandOutput,
            Constants.DotnetCommands.ViewCommandOutput);
    }

    #endregion

    #region Regression Guards

    [Fact]
    public async Task RegressionGuard_ValidationFailure_DoesNotThrow()
    {
        // Verify that validation failures are reported cleanly via return value, not exceptions
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = null,
            FileName = null,
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        // Should not throw
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
            FileName = "TestComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    #endregion

    #region Test Helpers

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

    #endregion
}
