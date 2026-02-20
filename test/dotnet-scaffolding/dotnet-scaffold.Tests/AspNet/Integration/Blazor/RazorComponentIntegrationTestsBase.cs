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
/// Shared base class for Razor Component (blazor-empty) integration tests across .NET versions.
/// Subclasses provide the target framework via <see cref="TargetFramework"/>.
/// </summary>
public abstract class RazorComponentIntegrationTestsBase : IDisposable
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

    protected RazorComponentIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
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
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
            ProjectPath = @"C:\NonExistent\Project.csproj",
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
    public void RazorComponent_DoesNotSetNamespace()
    {
        var step = new DotnetNewScaffolderStep(
            NullLogger<DotnetNewScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            ProjectPath = _testProjectPath,
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        Assert.Null(step.NamespaceName);
    }

    #endregion

    #region DotnetNewScaffolderStep  Output Folder Mapping

    [Fact]
    public async Task ExecuteAsync_CreatesComponentsDirectory_WhenProjectExists()
    {
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

        await step.ExecuteAsync(_context, CancellationToken.None);

        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(expectedComponentsDir), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OutputFolder_IsComponents_ForRazorComponent()
    {
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

        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(componentsDir), Times.Once);
        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(pagesDir), Times.Never);
        _mockFileSystem.Verify(fs => fs.CreateDirectoryIfNotExists(viewsDir), Times.Never);
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
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(telemetry.TrackedEvents);
        Assert.True(telemetry.TrackedEvents[0].Properties.ContainsKey("SettingsValidationResult"));
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_WhenSettingsAreValid()
    {
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
            FileName = "MyComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
    public async Task ExecuteAsync_GeneratesRazorFile_WhenProjectIsValid()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result, $"dotnet new razorcomponent should succeed for a valid {TargetFramework} project.");
        string expectedFile = Path.Combine(_testProjectDir, "Components", $"{step.FileName}.razor");
        Assert.True(File.Exists(expectedFile), $"Expected file '{expectedFile}' was not created.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedRazorFile_ContainsValidContent()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Components", $"{step.FileName}.razor");
        string content = File.ReadAllText(expectedFile);
        Assert.False(string.IsNullOrWhiteSpace(content), "Generated .razor file should not be empty.");
    }

    [Fact]
    public async Task ExecuteAsync_CreatesComponentsSubdirectory()
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string componentsDir = Path.Combine(_testProjectDir, "Components");
        Assert.True(Directory.Exists(componentsDir), "Components subdirectory should be created.");
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
            FileName = "widget",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Components", "Widget.razor");
        Assert.True(File.Exists(expectedFile), $"Expected file 'Widget.razor' (title-cased) was not created. FileName was '{step.FileName}'.");
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
            FileName = "TelemetryComponent",
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.Single(telemetry.TrackedEvents);
        Assert.True(telemetry.TrackedEvents[0].Properties.ContainsKey("SettingsValidationResult"));
        Assert.True(telemetry.TrackedEvents[0].Properties.ContainsKey("Result"));
    }

    [Fact]
    public async Task ExecuteAsync_OnlyGeneratesSingleRazorFile()
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string componentsDir = Path.Combine(_testProjectDir, "Components");
        string[] generatedFiles = Directory.GetFiles(componentsDir);
        Assert.Single(generatedFiles);
        Assert.EndsWith(".razor", generatedFiles[0]);
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        string pagesDir = Path.Combine(_testProjectDir, "Pages");
        Assert.False(Directory.Exists(pagesDir), "Pages directory should not be created for Razor components.");
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotCreateViewsDirectory()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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

        await step.ExecuteAsync(_context, CancellationToken.None);

        string viewsDir = Path.Combine(_testProjectDir, "Views");
        Assert.False(Directory.Exists(viewsDir), "Views directory should not be created for Razor components.");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedRazorFile_ContainsH3Heading()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Components", $"{step.FileName}.razor");
        string content = File.ReadAllText(expectedFile);
        Assert.Contains("<h3>", content);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedRazorFile_ContainsCodeBlock()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Components", $"{step.FileName}.razor");
        string content = File.ReadAllText(expectedFile);
        Assert.Contains("@code", content);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedRazorFile_DoesNotContainPageDirective()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string expectedFile = Path.Combine(_testProjectDir, "Components", $"{step.FileName}.razor");
        string content = File.ReadAllText(expectedFile);
        Assert.DoesNotContain("@page", content);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratedFile_HasRazorExtension()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);

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

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        string componentsDir = Path.Combine(_testProjectDir, "Components");
        string[] files = Directory.GetFiles(componentsDir);
        Assert.All(files, f => Assert.EndsWith(".razor", f));
        Assert.Empty(Directory.GetFiles(componentsDir, "*.cshtml"));
        Assert.Empty(Directory.GetFiles(componentsDir, "*.cs"));
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
            CommandName = Constants.DotnetCommands.RazorComponentCommandName
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
