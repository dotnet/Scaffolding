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
/// Shared base class for Minimal API integration tests across .NET versions.
/// </summary>
public abstract class MinimalApiIntegrationTestsBase : IDisposable
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

    protected MinimalApiIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.Api.MinimalApiDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.Api.MinimalApi);
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

    #region ValidateMinimalApiStep — Validation Logic

    [Fact]
    public async Task ValidateMinimalApiStep_FailsWithNullProject()
    {
        var step = CreateValidateMinimalApiStep();
        step.Project = null;
        step.Model = "Product";
        step.Endpoints = "ProductEndpoints";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateMinimalApiStep_FailsWithEmptyProject()
    {
        var step = CreateValidateMinimalApiStep();
        step.Project = string.Empty;
        step.Model = "Product";
        step.Endpoints = "ProductEndpoints";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateMinimalApiStep_FailsWithNonExistentProject()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateMinimalApiStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Model = "Product";
        step.Endpoints = "ProductEndpoints";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateMinimalApiStep_FailsWithNullModel()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateMinimalApiStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Endpoints = "ProductEndpoints";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateMinimalApiStep_FailsWithEmptyModel()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateMinimalApiStep();
        step.Project = _testProjectPath;
        step.Model = string.Empty;
        step.Endpoints = "ProductEndpoints";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateMinimalApiStep_AllFieldsEmpty_FailsValidation()
    {
        var step = CreateValidateMinimalApiStep();
        step.Project = null;
        step.Model = null;
        step.Endpoints = null;
        step.DataContext = null;
        step.DatabaseProvider = null;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region Telemetry

    [Fact]
    public async Task ValidateMinimalApiStep_TracksTelemetry_OnNullProjectFailure()
    {
        var step = CreateValidateMinimalApiStep();
        step.Project = null;
        step.Model = "Product";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateMinimalApiStep_TracksTelemetry_OnNullModelFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateMinimalApiStep();
        step.Project = _testProjectPath;
        step.Model = null;

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateMinimalApiStep_SingleEventPerValidation()
    {
        var step = CreateValidateMinimalApiStep();
        step.Project = null;

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region Validation Theories

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateMinimalApiStep_FailsWithInvalidModel(string? model)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateMinimalApiStep();
        step.Project = _testProjectPath;
        step.Model = model;
        step.Endpoints = "ProductEndpoints";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region EF Providers — Structure Tests

    [Fact]
    public void EfPackagesDict_ContainsAllFourProviders()
    {
        Assert.Equal(4, PackageConstants.EfConstants.EfPackagesDict.Count);
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.SqlServer));
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.SQLite));
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.CosmosDb));
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.Postgres));
    }

    [Fact]
    public void UseDatabaseMethods_HasAllFourProviders()
    {
        Assert.Equal(4, PackageConstants.EfConstants.UseDatabaseMethods.Count);
    }

    #endregion

    #region MinimalApi Templates

    [Fact]
    public void MinimalApiTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var minimalApiDir = Path.Combine(basePath, TargetFramework, "MinimalApi");
        Assert.True(Directory.Exists(minimalApiDir),
            $"MinimalApi template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void MinimalApiTemplates_AllFilesAreCshtml()
    {
        var basePath = GetActualTemplatesBasePath();
        var minimalApiDir = Path.Combine(basePath, TargetFramework, "MinimalApi");
        var files = Directory.GetFiles(minimalApiDir, "*", SearchOption.AllDirectories);
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

    #region Helper Methods

    private ValidateMinimalApiStep CreateValidateMinimalApiStep()
    {
        return new ValidateMinimalApiStep(
            _mockFileSystem.Object,
            NullLogger<ValidateMinimalApiStep>.Instance,
            _testTelemetryService);
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
