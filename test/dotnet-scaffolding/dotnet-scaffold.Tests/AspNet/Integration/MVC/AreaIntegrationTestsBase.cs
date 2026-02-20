// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
/// Shared base class for MVC Area integration tests across .NET versions.
/// The Area scaffolder creates a directory structure (Areas/{Name}/Controllers, Models, Data, Views)
/// with no templates, no NuGet packages, and no code modifications.
/// Subclasses provide the target framework via <see cref="TargetFramework"/>.
/// </summary>
public abstract class AreaIntegrationTestsBase : IDisposable
{
    protected abstract string TargetFramework { get; }
    protected abstract string TestClassName { get; }

    protected readonly string _testDirectory;
    protected readonly string _testProjectDir;
    protected readonly string _testProjectPath;
    protected readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IEnvironmentService> _mockEnvironmentService;
    protected readonly Mock<IScaffolder> _mockScaffolder;
    protected readonly ScaffolderContext _context;

    protected AreaIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
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

    protected string ProjectContent => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
  </PropertyGroup>
</Project>";

    #region AreaScaffolderStep — Directory Creation

    [Fact]
    public async Task AreaScaffolderStep_CreatesAreasDirectory()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith("Areas"));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesNamedAreaDirectory()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith(Path.Combine("Areas", "Admin")));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesControllersFolder()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith(Path.Combine("Admin", "Controllers")));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesModelsFolder()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith(Path.Combine("Admin", "Models")));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesDataFolder()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith(Path.Combine("Admin", "Data")));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesViewsFolder()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.EndsWith(Path.Combine("Admin", "Views")));
    }

    [Fact]
    public async Task AreaScaffolderStep_CreatesExactly6Directories()
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
    public async Task AreaScaffolderStep_ReturnsTrue_OnSuccess()
    {
        SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "Admin";

        var result = await step.ExecuteAsync(_context);
        Assert.True(result);
    }

    [Fact]
    public async Task AreaScaffolderStep_UsesProjectDirectory_WhenExists()
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
    public async Task AreaScaffolderStep_FallsBackToCurrentDirectory_WhenProjectDirMissing()
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
    public async Task AreaScaffolderStep_SupportsCustomAreaName()
    {
        var createdDirs = SetupFileSystemForSuccess();

        var step = CreateAreaScaffolderStep();
        step.Project = _testProjectPath;
        step.Name = "MyCustomArea";

        await step.ExecuteAsync(_context);

        Assert.Contains(createdDirs, d => d.Contains("MyCustomArea"));
    }

    #endregion

    #region No Area Templates

    [Fact]
    public void Templates_NoAreaFolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var areaDir = Path.Combine(basePath, TargetFramework, "Area");
        Assert.False(Directory.Exists(areaDir),
            $"Area template folder should NOT exist for {TargetFramework} (Area scaffolder creates directories only)");
    }

    [Fact]
    public void Templates_NoAreasFolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var areasDir = Path.Combine(basePath, TargetFramework, "Areas");
        Assert.False(Directory.Exists(areasDir),
            $"Areas template folder should NOT exist for {TargetFramework}");
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

    #region Multiple Area Names

    [Theory]
    [InlineData("Admin")]
    [InlineData("Blog")]
    [InlineData("Dashboard")]
    [InlineData("Api")]
    [InlineData("Reporting")]
    public async Task AreaScaffolderStep_CreatesCorrectStructure_ForVariousNames(string areaName)
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

    #region Helper Methods

    private AreaScaffolderStep CreateAreaScaffolderStep()
    {
        return new AreaScaffolderStep(
            _mockFileSystem.Object,
            NullLogger<AreaScaffolderStep>.Instance,
            _mockEnvironmentService.Object);
    }

    protected List<string> SetupFileSystemForSuccess()
    {
        var createdDirs = new List<string>();
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(Path.GetDirectoryName(_testProjectPath)!)).Returns(true);
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
