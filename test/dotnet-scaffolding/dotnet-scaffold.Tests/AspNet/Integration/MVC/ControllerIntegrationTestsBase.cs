// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// Shared base class for MVC Controller (empty) integration tests across .NET versions.
/// Subclasses provide the target framework via <see cref="TargetFramework"/>.
/// </summary>
public abstract class ControllerIntegrationTestsBase : IDisposable
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

    protected ControllerIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();

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

    protected string ProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";

    #region EmptyControllerScaffolderStep — Validation

    [Fact]
    public async Task EmptyControllerScaffolderStep_FailsWithNullProjectPath()
    {
        var step = CreateEmptyControllerStep();
        step.ProjectPath = null;
        step.FileName = "HomeController";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_FailsWithEmptyProjectPath()
    {
        var step = CreateEmptyControllerStep();
        step.ProjectPath = string.Empty;
        step.FileName = "HomeController";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_FailsWithNonExistentProject()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateEmptyControllerStep();
        step.ProjectPath = @"C:\NonExistent\Project.csproj";
        step.FileName = "HomeController";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_FailsWithNullFileName()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = null;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_FailsWithEmptyFileName()
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
    public async Task EmptyControllerScaffolderStep_TracksTelemetry_OnValidationFailure()
    {
        var step = CreateEmptyControllerStep();
        step.ProjectPath = null;
        step.FileName = "HomeController";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_TracksTelemetry_OnFileNameValidationFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = null;

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region EmptyControllerScaffolderStep — Controllers Directory

    [Fact]
    public async Task EmptyControllerScaffolderStep_CreatesControllersDirectory()
    {
        var createdDirs = SetupFileSystemForDotnetNew();

        var step = CreateEmptyControllerStep();
        step.ProjectPath = _testProjectPath;
        step.FileName = "HomeController";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith("Controllers"));
    }

    [Fact]
    public async Task EmptyControllerScaffolderStep_ControllersDir_IsUnderProjectDir()
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

    #region Multiple Controller Names

    [Theory]
    [InlineData("HomeController")]
    [InlineData("ProductController")]
    [InlineData("AccountController")]
    [InlineData("OrderController")]
    [InlineData("DashboardController")]
    public async Task EmptyControllerScaffolderStep_FailsValidation_ForVariousNames_WhenProjectMissing(string controllerName)
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
    public async Task EmptyControllerScaffolderStep_CreatesControllersDir_ForVariousNames(string controllerName)
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
    public async Task EmptyControllerScaffolderStep_WithActionsTrue_CreatesControllersDir()
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
    public async Task EmptyControllerScaffolderStep_WithActionsFalse_CreatesControllersDir()
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

    #region EfController Templates

    [Fact]
    public void EfControllerTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var efControllerDir = Path.Combine(basePath, TargetFramework, "EfController");
        Assert.True(Directory.Exists(efControllerDir),
            $"EfController template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void EfControllerTemplates_HasExactly2Files()
    {
        var basePath = GetActualTemplatesBasePath();
        var efControllerDir = Path.Combine(basePath, TargetFramework, "EfController");
        var files = Directory.GetFiles(efControllerDir, "*", SearchOption.AllDirectories);
        Assert.Equal(2, files.Length);
    }

    [Fact]
    public void EfControllerTemplates_UsesCshtmlFormat()
    {
        var basePath = GetActualTemplatesBasePath();
        var efControllerDir = Path.Combine(basePath, TargetFramework, "EfController");
        var files = Directory.GetFiles(efControllerDir, "*", SearchOption.AllDirectories);
        Assert.All(files, f => Assert.EndsWith(".cshtml", f));
    }

    #endregion

    #region Views Templates

    [Fact]
    public void ViewsTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        Assert.True(Directory.Exists(viewsDir),
            $"Views template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void ViewsTemplates_HasBootstrap4Subfolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var bootstrap4Dir = Path.Combine(basePath, TargetFramework, "Views", "Bootstrap4");
        Assert.True(Directory.Exists(bootstrap4Dir),
            $"Bootstrap4 subfolder should exist for {TargetFramework} Views templates");
    }

    [Fact]
    public void ViewsTemplates_HasBootstrap5Subfolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var bootstrap5Dir = Path.Combine(basePath, TargetFramework, "Views", "Bootstrap5");
        Assert.True(Directory.Exists(bootstrap5Dir),
            $"Bootstrap5 subfolder should exist for {TargetFramework} Views templates");
    }

    [Fact]
    public void ViewsTemplates_AllFilesAreCshtml()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var files = Directory.GetFiles(viewsDir, "*", SearchOption.AllDirectories);
        Assert.All(files, f => Assert.EndsWith(".cshtml", f));
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
    public void Templates_HasExpectedScaffolderFolder(string folderName)
    {
        var basePath = GetActualTemplatesBasePath();
        var folderPath = Path.Combine(basePath, TargetFramework, folderName);
        Assert.True(Directory.Exists(folderPath),
            $"Expected template folder '{folderName}' not found for {TargetFramework}");
    }

    #endregion

    #region No Empty Controller Template Folder

    [Fact]
    public void Templates_NoMvcControllerFolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var controllerDir = Path.Combine(basePath, TargetFramework, "MvcController");
        Assert.False(Directory.Exists(controllerDir),
            $"Empty MVC Controller template folder should NOT exist for {TargetFramework} (uses dotnet new)");
    }

    [Fact]
    public void Templates_NoControllerFolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var controllerDir = Path.Combine(basePath, TargetFramework, "Controller");
        Assert.False(Directory.Exists(controllerDir),
            $"Controller template folder should NOT exist for {TargetFramework} (uses dotnet new)");
    }

    #endregion

    #region Helper Methods

    private EmptyControllerScaffolderStep CreateEmptyControllerStep()
    {
        return new EmptyControllerScaffolderStep(
            NullLogger<EmptyControllerScaffolderStep>.Instance,
            _mockFileSystem.Object,
            _testTelemetryService)
        {
            CommandName = "mvccontroller"
        };
    }

    protected List<string> SetupFileSystemForDotnetNew()
    {
        var createdDirs = new List<string>();
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.CreateDirectoryIfNotExists(It.IsAny<string>()))
            .Callback<string>(dir => createdDirs.Add(dir));
        return createdDirs;
    }

    protected static string GetActualTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

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

    #endregion

    #region Test Helpers

    protected class TestTelemetryService : ITelemetryService
    {
        public List<(string EventName, IReadOnlyDictionary<string, string> Properties, IReadOnlyDictionary<string, double> Measures)> TrackedEvents { get; } = new();
        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string>? properties = null, IReadOnlyDictionary<string, double>? measures = null)
        {
            TrackedEvents.Add((eventName, properties ?? new Dictionary<string, string>(), measures ?? new Dictionary<string, double>()));
        }

        public void Flush() { }
    }

    #endregion
}
