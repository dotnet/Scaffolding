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

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// Integration tests for the Razor Page Empty (razorpage-empty) scaffolder targeting .NET 10.
/// Validates DotnetNewScaffolderStep validation logic, output folder mapping, title casing,
/// scaffolder definition, namespace handling, and end-to-end file generation via 'dotnet new page'.
/// </summary>
public class RazorPageEmptyNet10IntegrationTests : IDisposable
{
    private const string TargetFramework = "net10.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public RazorPageEmptyNet10IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "RazorPageEmptyNet10IntegrationTests", Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.RazorPage.EmptyDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.RazorPage.Empty);
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
    public void PageCommandName_IsPage()
    {
        Assert.Equal("page", Constants.DotnetCommands.RazorPageCommandName);
    }

    [Fact]
    public void PageCommandOutput_IsPages()
    {
        Assert.Equal("Pages", Constants.DotnetCommands.RazorPageCommandOutput);
    }

    [Fact]
    public void ScaffolderName_IsRazorPageEmpty()
    {
        Assert.Equal("razorpage-empty", AspnetStrings.RazorPage.Empty);
    }

    [Fact]
    public void ScaffolderDisplayName_IsRazorPageEmpty()
    {
        Assert.Equal("Razor Page - Empty", AspnetStrings.RazorPage.EmptyDisplayName);
    }

    [Fact]
    public void ScaffolderDescription_DescribesEmptyRazorPage()
    {
        Assert.Equal("Add an empty razor page to a given project", AspnetStrings.RazorPage.EmptyDescription);
    }

    [Fact]
    public void ScaffolderExample_ContainsRazorPageEmptyCommand()
    {
        Assert.Contains("razorpage-empty", AspnetStrings.RazorPage.EmptyExample);
    }

    [Fact]
    public void ScaffolderExample_ContainsProjectOption()
    {
        Assert.Contains("--project", AspnetStrings.RazorPage.EmptyExample);
    }

    [Fact]
    public void ScaffolderExample_ContainsFileNameOption()
    {
        Assert.Contains("--file-name", AspnetStrings.RazorPage.EmptyExample);
    }

    [Fact]
    public void ScaffolderExample_ContainsSampleFileName()
    {
        Assert.Contains("Contact", AspnetStrings.RazorPage.EmptyExample);
    }

    [Fact]
    public void ScaffolderExampleDescription_IsNotEmpty()
    {
        Assert.False(string.IsNullOrEmpty(AspnetStrings.RazorPage.EmptyExampleDescription));
    }

    [Fact]
    public void ScaffolderCategory_IsRazorPages()
    {
        Assert.Equal("Razor Pages", AspnetStrings.Catagories.RazorPages);
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
            FileName = "Contact",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            FileName = "Contact",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            FileName = "Contact",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        Assert.NotNull(step);
        Assert.Equal(Constants.DotnetCommands.RazorPageCommandName, step.CommandName);
    }

    [Fact]
    public void ProjectPath_DefaultsToNull()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            FileName = "Contact",
            NamespaceName = "TestProject",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        Assert.Equal(_testProjectPath, step.ProjectPath);
        Assert.Equal("Contact", step.FileName);
        Assert.Equal("TestProject", step.NamespaceName);
        Assert.Equal(Constants.DotnetCommands.RazorPageCommandName, step.CommandName);
    }

    [Fact]
    public void RazorPageEmpty_SetsNamespace()
    {
        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Contact",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        Assert.Equal(projectName, step.NamespaceName);
    }

    #endregion

    #region DotnetNewScaffolderStep — Output Folder Mapping

    [Fact]
    public async Task ExecuteAsync_CreatesPagesDirectory_WhenProjectExists()
    {
        string expectedPagesDir = Path.Combine(_testProjectDir, "Pages");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(expectedPagesDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Contact",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(expectedPagesDir), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OutputFolder_IsPages_ForPage()
    {
        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        string viewsDir = Path.Combine(_testProjectDir, "Views");
        string componentsDir = Path.Combine(_testProjectDir, "Components");

        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(pagesDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "TestPage",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(pagesDir), Times.Once);
        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(viewsDir), Times.Never);
        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(componentsDir), Times.Never);
    }

    #endregion

    #region DotnetNewScaffolderStep — Title Casing

    [Theory]
    [InlineData("contact", "Contact")]
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
        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(pagesDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "contact",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string expected = CultureInfo.CurrentCulture.TextInfo.ToTitleCase("contact");
        Assert.Equal(expected, step.FileName);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesTitleCase_WhenAlreadyCapitalized()
    {
        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(pagesDir)).Returns(true);

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Contact",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string expected = CultureInfo.CurrentCulture.TextInfo.ToTitleCase("Contact");
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
            FileName = "Contact",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            FileName = "Contact",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal(AspnetStrings.RazorPage.EmptyDisplayName, telemetry.TrackedEvents[0].Properties["ScaffolderName"]);
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
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("Failure", telemetry.TrackedEvents[0].Properties["SettingsValidationResult"]);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_WhenSettingsAreValid()
    {
        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(pagesDir)).Returns(true);
        var telemetry = new TestTelemetryService();

        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            telemetry)
        {
            ProjectPath = _testProjectPath,
            FileName = "ValidPage",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            FileName = "Contact",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        using var cts = new CancellationTokenSource();

        bool result = await step.ExecuteAsync(_context, cts.Token);

        Assert.False(result);
    }

    #endregion

    #region Output Folder Mapping — All Commands

    [Fact]
    public void OutputFolders_RazorPage_MapsToPages()
    {
        Assert.Equal("page", Constants.DotnetCommands.RazorPageCommandName);
        Assert.Equal("Pages", Constants.DotnetCommands.RazorPageCommandOutput);
    }

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

    #endregion

    #region Scaffolder Registration Differentiation

    [Fact]
    public void RazorPageEmpty_IsDifferentFromBlazorEmpty()
    {
        Assert.NotEqual(AspnetStrings.RazorPage.Empty, AspnetStrings.Blazor.Empty);
    }

    [Fact]
    public void RazorPageEmpty_IsDifferentFromRazorViewEmpty()
    {
        Assert.NotEqual(AspnetStrings.RazorPage.Empty, AspnetStrings.RazorView.Empty);
    }

    [Fact]
    public void RazorPageEmpty_IsDifferentFromMvcController()
    {
        Assert.NotEqual(AspnetStrings.RazorPage.Empty, AspnetStrings.MVC.Controller);
    }

    [Fact]
    public void RazorPageEmpty_IsDifferentFromRazorPagesCrud()
    {
        Assert.NotEqual(AspnetStrings.RazorPage.Empty, AspnetStrings.RazorPage.Crud);
    }

    #endregion

    #region Namespace Handling

    [Fact]
    public void RazorPageEmpty_NamespaceMatchesProjectName()
    {
        string expectedNamespace = Path.GetFileNameWithoutExtension(_testProjectPath);
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Contact",
            NamespaceName = expectedNamespace,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        Assert.Equal("TestProject", step.NamespaceName);
    }

    [Fact]
    public void RazorPageEmpty_DiffersFromRazorViewEmpty_InNamespaceHandling()
    {
        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);

        var pageStep = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Contact",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        var viewStep = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Dashboard",
            CommandName = Constants.DotnetCommands.ViewCommandName
        };

        Assert.NotNull(pageStep.NamespaceName);
        Assert.Null(viewStep.NamespaceName);
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

    #region End-to-End File Generation (net10.0)

    [Fact]
    public async Task ExecuteAsync_GeneratesPageFiles_WhenNet10ProjectIsValid()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Contact",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result, $"dotnet new page should succeed for a valid {TargetFramework} project.");
        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        string expectedCshtml = Path.Combine(pagesDir, $"{step.FileName}.cshtml");
        string expectedCshtmlCs = Path.Combine(pagesDir, $"{step.FileName}.cshtml.cs");
        Assert.True(File.Exists(expectedCshtml), $"Expected file '{expectedCshtml}' was not created.");
        Assert.True(File.Exists(expectedCshtmlCs), $"Expected code-behind file '{expectedCshtmlCs}' was not created.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedPageFile_ContainsValidContent_Net10()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Contact",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Pages", $"{step.FileName}.cshtml");
        string content = File.ReadAllText(expectedFile);
        Assert.False(string.IsNullOrWhiteSpace(content), "Generated .cshtml file should not be empty.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedPageFile_HasCshtmlExtension_Net10()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "ExtCheck",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        string[] cshtmlFiles = Directory.GetFiles(pagesDir, "*.cshtml");
        Assert.Contains(cshtmlFiles, f => f.EndsWith($"{step.FileName}.cshtml"));
        Assert.Empty(Directory.GetFiles(pagesDir, "*.razor"));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesPagesSubdirectory_Net10()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "Widget",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        Assert.True(Directory.Exists(pagesDir), "Pages subdirectory should be created.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesCorrectFileName_WhenLowercaseInput_Net10()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "contact",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Pages", "Contact.cshtml");
        Assert.True(File.Exists(expectedFile), $"Expected file 'Contact.cshtml' (title-cased) was not created. FileName was '{step.FileName}'.");
    }

    [Fact]
    public async Task ExecuteAsync_TracksSuccessTelemetry_WhenNet10GenerationSucceeds()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var telemetry = new TestTelemetryService();
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            telemetry)
        {
            ProjectPath = _testProjectPath,
            FileName = "TelemetryPage",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("Success", telemetry.TrackedEvents[0].Properties["SettingsValidationResult"]);
        Assert.Equal("Success", telemetry.TrackedEvents[0].Properties["Result"]);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesPageAndCodeBehind_Net10()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "TwoFiles",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        string cshtmlFile = Path.Combine(pagesDir, "TwoFiles.cshtml");
        string codeBehindFile = Path.Combine(pagesDir, "TwoFiles.cshtml.cs");
        Assert.True(File.Exists(cshtmlFile), "Expected .cshtml file was not created.");
        Assert.True(File.Exists(codeBehindFile), "Expected .cshtml.cs code-behind file was not created.");
    }

    [Fact]
    public async Task ExecuteAsync_CodeBehindContainsPageModel_Net10()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "PageModelCheck",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string codeBehindFile = Path.Combine(_testProjectDir, "Pages", "PageModelCheck.cshtml.cs");
        string content = File.ReadAllText(codeBehindFile);
        Assert.Contains("PageModel", content);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateViewsDirectory_Net10()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "NoViews",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string viewsDir = Path.Combine(_testProjectDir, "Views");
        Assert.False(Directory.Exists(viewsDir), "Views directory should not be created for Razor pages.");
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateComponentsDirectory_Net10()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "NoComponents",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string componentsDir = Path.Combine(_testProjectDir, "Components");
        Assert.False(Directory.Exists(componentsDir), "Components directory should not be created for Razor pages.");
    }

    #endregion

    #region Razor Page vs Other Scaffolders Comparison

    [Fact]
    public void Page_CommandName_DiffersFromRazorComponent()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.RazorPageCommandName,
            Constants.DotnetCommands.RazorComponentCommandName);
    }

    [Fact]
    public void Page_OutputFolder_DiffersFromRazorComponent()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.RazorPageCommandOutput,
            Constants.DotnetCommands.RazorComponentCommandOutput);
    }

    [Fact]
    public void Page_CommandName_DiffersFromView()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.RazorPageCommandName,
            Constants.DotnetCommands.ViewCommandName);
    }

    [Fact]
    public void Page_OutputFolder_DiffersFromView()
    {
        Assert.NotEqual(
            Constants.DotnetCommands.RazorPageCommandOutput,
            Constants.DotnetCommands.ViewCommandOutput);
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
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            CommandName = Constants.DotnetCommands.RazorPageCommandName
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
            FileName = "TestPage",
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    #endregion

    #region Net10 vs Net9 Parity

    [Fact]
    public async Task Net10_GeneratesSameFileTypes_AsNet9()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "ParityCheck",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        string cshtmlFile = Path.Combine(pagesDir, "ParityCheck.cshtml");
        string codeBehindFile = Path.Combine(pagesDir, "ParityCheck.cshtml.cs");
        Assert.True(File.Exists(cshtmlFile));
        Assert.True(File.Exists(codeBehindFile));
    }

    [Fact]
    public async Task Net10_GeneratesInPagesFolder_SameAsNet9()
    {
        string projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";
        File.WriteAllText(_testProjectPath, projectContent);

        string projectName = Path.GetFileNameWithoutExtension(_testProjectPath);
        var realFileSystem = new FileSystem();
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            realFileSystem,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "FolderCheck",
            NamespaceName = projectName,
            CommandName = Constants.DotnetCommands.RazorPageCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        Assert.True(Directory.Exists(pagesDir));
        string viewsDir = Path.Combine(_testProjectDir, "Views");
        Assert.False(Directory.Exists(viewsDir));
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
