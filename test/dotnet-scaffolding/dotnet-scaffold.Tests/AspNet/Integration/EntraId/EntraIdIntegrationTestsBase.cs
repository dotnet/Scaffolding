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
/// Shared base class for Entra ID integration tests across .NET versions (net10+).
/// </summary>
public abstract class EntraIdIntegrationTestsBase : IDisposable
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

    protected EntraIdIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.EntraId.DisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.EntraId.Name);
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

    #region ValidateEntraIdStep — Validation Logic

    [Fact]
    public async Task ValidateEntraIdStep_FailsWhenProjectMissing()
    {
        var step = CreateValidateEntraIdStep();
        step.Project = null;
        step.Username = "user@contoso.com";
        step.TenantId = "tenant-123";

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEntraIdStep_FailsWhenUsernameMissing()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEntraIdStep();
        step.Project = _testProjectPath;
        step.Username = null;
        step.TenantId = "tenant-123";

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEntraIdStep_FailsWhenTenantIdMissing()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEntraIdStep();
        step.Project = _testProjectPath;
        step.Username = "user@contoso.com";
        step.TenantId = null;

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEntraIdStep_FailsWhenUseExistingTrueButApplicationMissing()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEntraIdStep();
        step.Project = _testProjectPath;
        step.Username = "user@contoso.com";
        step.TenantId = "tenant-123";
        step.UseExistingApplication = true;
        step.Application = null;

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEntraIdStep_FailsWhenProjectFileDoesNotExist()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateEntraIdStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Username = "user@contoso.com";
        step.TenantId = "tenant-123";

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public void ValidateEntraIdStep_IsScaffoldStep()
    {
        var step = CreateValidateEntraIdStep();
        Assert.IsAssignableFrom<Scaffolding.Core.Steps.ScaffoldStep>(step);
    }

    [Fact]
    public void ValidateEntraIdStep_HasUsernameProperty()
    {
        var step = CreateValidateEntraIdStep();
        step.Username = "test@example.com";
        Assert.NotNull(step.Username);
    }

    [Fact]
    public void ValidateEntraIdStep_HasProjectProperty()
    {
        var step = CreateValidateEntraIdStep();
        step.Project = _testProjectPath;
        Assert.NotNull(step.Project);
    }

    [Fact]
    public void ValidateEntraIdStep_HasTenantIdProperty()
    {
        var step = CreateValidateEntraIdStep();
        step.TenantId = "tenant-123";
        Assert.NotNull(step.TenantId);
    }

    [Fact]
    public void ValidateEntraIdStep_HasApplicationProperty()
    {
        var step = CreateValidateEntraIdStep();
        step.Application = "app-456";
        Assert.NotNull(step.Application);
    }

    [Fact]
    public void ValidateEntraIdStep_HasUseExistingApplicationProperty()
    {
        var step = CreateValidateEntraIdStep();
        step.UseExistingApplication = true;
        Assert.True(step.UseExistingApplication);
    }

    #endregion

    #region Telemetry

    [Fact]
    public async Task ValidateEntraIdStep_TracksTelemetry_OnProjectMissingFailure()
    {
        var step = CreateValidateEntraIdStep();
        step.Project = null;
        step.Username = "user@contoso.com";
        step.TenantId = "tenant-123";

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEntraIdStep_TracksTelemetry_OnUsernameMissingFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEntraIdStep();
        step.Project = _testProjectPath;
        step.Username = null;
        step.TenantId = "tenant-123";

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEntraIdStep_SingleEventPerValidation()
    {
        var step = CreateValidateEntraIdStep();
        step.Project = null;

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region Code Modification Configs

    [Fact]
    public void BlazorEntraChangesConfig_ExistsForTargetFramework()
    {
        var basePath = GetActualTemplatesBasePath();
        var configPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "blazorEntraChanges.json");
        Assert.True(File.Exists(configPath),
            $"blazorEntraChanges.json should exist for {TargetFramework}");
    }

    [Fact]
    public void BlazorWasmEntraChangesConfig_ExistsForTargetFramework()
    {
        var basePath = GetActualTemplatesBasePath();
        var configPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "blazorWasmEntraChanges.json");
        Assert.True(File.Exists(configPath),
            $"blazorWasmEntraChanges.json should exist for {TargetFramework}");
    }

    #endregion

    #region Validation Combination Tests

    [Fact]
    public async Task ValidateEntraIdStep_AllNullInputs_DoNotThrow()
    {
        var step = CreateValidateEntraIdStep();
        step.Project = null;
        step.Username = null;
        step.TenantId = null;
        step.Application = null;

        var result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
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

    private ValidateEntraIdStep CreateValidateEntraIdStep()
    {
        return new ValidateEntraIdStep(
            _mockFileSystem.Object,
            NullLogger<ValidateEntraIdStep>.Instance,
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
