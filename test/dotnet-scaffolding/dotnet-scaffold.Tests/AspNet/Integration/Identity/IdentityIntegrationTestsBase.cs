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
/// Shared base class for ASP.NET Core Identity integration tests across .NET versions.
/// </summary>
public abstract class IdentityIntegrationTestsBase : IDisposable
{
    protected abstract string TargetFramework { get; }
    protected abstract string TestClassName { get; }

    protected readonly string _testDirectory;
    protected readonly string _testProjectDir;
    protected readonly string _testProjectPath;
    protected readonly string _templatesDirectory;
    protected readonly Mock<IFileSystem> _mockFileSystem;
    protected readonly TestTelemetryService _testTelemetryService;
    protected readonly Mock<IScaffolder> _mockScaffolder;
    protected readonly ScaffolderContext _context;

    protected IdentityIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        _templatesDirectory = Path.Combine(_testDirectory, "Templates");
        Directory.CreateDirectory(_testProjectDir);
        Directory.CreateDirectory(_templatesDirectory);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.Identity.DisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.Identity.Name);
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

    #region ValidateIdentityStep — Validation Logic

    [Fact]
    public async Task ValidateIdentityStep_FailsWithNullProject()
    {
        var step = CreateValidateIdentityStep();
        step.Project = null;
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateIdentityStep_FailsWithEmptyProject()
    {
        var step = CreateValidateIdentityStep();
        step.Project = string.Empty;
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateIdentityStep_FailsWithNonExistentProject()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateIdentityStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateIdentityStep_FailsWithNullDataContext()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateIdentityStep();
        step.Project = _testProjectPath;
        step.DataContext = null;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateIdentityStep_FailsWithEmptyDataContext()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateIdentityStep();
        step.Project = _testProjectPath;
        step.DataContext = string.Empty;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public void ValidateIdentityStep_HasOverwriteProperty()
    {
        var step = CreateValidateIdentityStep();
        step.Overwrite = true;
        Assert.True(step.Overwrite);
    }

    [Fact]
    public void ValidateIdentityStep_HasBlazorScenarioProperty()
    {
        var step = CreateValidateIdentityStep();
        step.BlazorScenario = false;
        Assert.False(step.BlazorScenario);
    }

    [Fact]
    public void ValidateIdentityStep_HasPrereleaseProperty()
    {
        var step = CreateValidateIdentityStep();
        step.Prerelease = true;
        Assert.True(step.Prerelease);
    }

    #endregion

    #region Telemetry

    [Fact]
    public async Task ValidateIdentityStep_TracksTelemetry_OnFailure()
    {
        var step = CreateValidateIdentityStep();
        step.Project = null;
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region Identity — EF Provider Structure

    [Fact]
    public void IdentityEfPackagesDict_ContainsSqlServer()
    {
        Assert.True(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(PackageConstants.EfConstants.SqlServer));
    }

    [Fact]
    public void IdentityEfPackagesDict_ContainsSqlite()
    {
        Assert.True(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(PackageConstants.EfConstants.SQLite));
    }

    [Fact]
    public void IdentityEfPackagesDict_DoesNotContainCosmos()
    {
        Assert.False(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(PackageConstants.EfConstants.CosmosDb));
    }

    [Fact]
    public void IdentityEfPackagesDict_DoesNotContainPostgres()
    {
        Assert.False(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(PackageConstants.EfConstants.Postgres));
    }

    [Fact]
    public void IdentityEfPackagesDict_HasExactlyTwoProviders()
    {
        Assert.Equal(2, PackageConstants.EfConstants.IdentityEfPackagesDict.Count);
    }

    [Theory]
    [InlineData("sqlserver-efcore")]
    [InlineData("sqlite-efcore")]
    public void IdentityEfPackagesDict_SupportsProvider(string provider)
    {
        Assert.True(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(provider));
    }

    [Theory]
    [InlineData("cosmos-efcore")]
    [InlineData("npgsql-efcore")]
    [InlineData("mysql")]
    [InlineData("")]
    public void IdentityEfPackagesDict_DoesNotSupportProvider(string provider)
    {
        Assert.False(PackageConstants.EfConstants.IdentityEfPackagesDict.ContainsKey(provider));
    }

    #endregion

    #region Identity — Template Folder Structure

    [Fact]
    public void Identity_Bootstrap5_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var bs5Dir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5");
        Assert.True(Directory.Exists(bs5Dir),
            $"Identity/Bootstrap5 should exist for {TargetFramework}");
    }

    [Fact]
    public void Identity_Bootstrap4_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var bs4Dir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4");
        Assert.True(Directory.Exists(bs4Dir),
            $"Identity/Bootstrap4 should exist for {TargetFramework}");
    }

    [Fact]
    public void Identity_Bootstrap5_HasFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var bs5Dir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5");
        var files = Directory.GetFiles(bs5Dir, "*", SearchOption.AllDirectories);
        Assert.True(files.Length > 0, $"Identity/Bootstrap5 should have files for {TargetFramework}");
    }

    [Fact]
    public void Identity_Bootstrap4_HasFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var bs4Dir = Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4");
        var files = Directory.GetFiles(bs4Dir, "*", SearchOption.AllDirectories);
        Assert.True(files.Length > 0, $"Identity/Bootstrap4 should have files for {TargetFramework}");
    }

    [Fact]
    public void Identity_Bootstrap5_HasMoreOrEqualFilesThanBootstrap4()
    {
        var basePath = GetActualTemplatesBasePath();
        var bs5Files = Directory.GetFiles(Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap5"), "*", SearchOption.AllDirectories);
        var bs4Files = Directory.GetFiles(Path.Combine(basePath, TargetFramework, "Identity", "Bootstrap4"), "*", SearchOption.AllDirectories);
        Assert.True(bs5Files.Length >= bs4Files.Length,
            $"Bootstrap5 should have >= files than Bootstrap4 for {TargetFramework}");
    }

    #endregion

    #region Identity — Code Modification Config

    [Fact]
    public void IdentityMinimalHostingChangesConfig_ExistsForTargetFramework()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        Assert.True(File.Exists(configPath),
            $"identityMinimalHostingChanges.json should exist for {TargetFramework}");
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_IsNotEmpty()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        var content = File.ReadAllText(configPath);
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public void IdentityMinimalHostingChangesConfig_ReferencesProgramCs()
    {
        var configPath = GetIdentityMinimalHostingChangesConfigPath();
        var content = File.ReadAllText(configPath);
        Assert.Contains("Program.cs", content);
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

    #region Helper Methods

    private ValidateIdentityStep CreateValidateIdentityStep()
    {
        return new ValidateIdentityStep(
            _mockFileSystem.Object,
            NullLogger<ValidateIdentityStep>.Instance,
            _testTelemetryService);
    }

    protected static string GetActualTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

    protected string GetIdentityMinimalHostingChangesConfigPath()
    {
        var basePath = GetActualTemplatesBasePath();
        return Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "identityMinimalHostingChanges.json");
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
