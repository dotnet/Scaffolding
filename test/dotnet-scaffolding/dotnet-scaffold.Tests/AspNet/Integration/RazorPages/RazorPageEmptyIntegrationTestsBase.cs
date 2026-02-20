// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
/// Shared base class for Razor Page Empty (razorpage-empty) integration tests across .NET versions.
/// Subclasses provide the target framework via <see cref="TargetFramework"/>.
/// </summary>
public abstract class RazorPageEmptyIntegrationTestsBase : IDisposable
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

    protected RazorPageEmptyIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
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

    protected string ProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";

    #region DotnetNewScaffolderStep  Validation

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

    #region DotnetNewScaffolderStep  Property Initialization

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

    #region DotnetNewScaffolderStep  Output Folder Mapping

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

    #region DotnetNewScaffolderStep  Telemetry

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
        Assert.True(telemetry.TrackedEvents[0].Properties.ContainsKey("SettingsValidationResult"));
        Assert.True(telemetry.TrackedEvents[0].Properties.ContainsKey("Result"));
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
        Assert.True(telemetry.TrackedEvents[0].Properties.ContainsKey("SettingsValidationResult"));
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
        Assert.True(telemetry.TrackedEvents[0].Properties.ContainsKey("SettingsValidationResult"));
    }

    #endregion

    #region DotnetNewScaffolderStep  Cancellation Token

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
    public async Task ExecuteAsync_GeneratesPageFiles_WhenProjectIsValid()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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
    public async Task ExecuteAsync_GeneratedPageFile_ContainsValidContent()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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
    public async Task ExecuteAsync_GeneratedPageFile_HasCshtmlExtension()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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
    public async Task ExecuteAsync_CreatesPagesSubdirectory()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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
    public async Task ExecuteAsync_GeneratesCorrectFileName_WhenLowercaseInput()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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
    public async Task ExecuteAsync_TracksSuccessTelemetry_WhenGenerationSucceeds()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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
        Assert.True(telemetry.TrackedEvents[0].Properties.ContainsKey("SettingsValidationResult"));
        Assert.True(telemetry.TrackedEvents[0].Properties.ContainsKey("Result"));
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesPageAndCodeBehind()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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
    public async Task ExecuteAsync_CodeBehindContainsPageModel()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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
    public async Task ExecuteAsync_DoesNotCreateViewsDirectory()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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
    public async Task ExecuteAsync_DoesNotCreateComponentsDirectory()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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
