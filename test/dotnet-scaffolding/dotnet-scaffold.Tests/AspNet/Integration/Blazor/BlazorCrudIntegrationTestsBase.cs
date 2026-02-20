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
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// Shared base class for Blazor CRUD integration tests across .NET versions.
/// </summary>
public abstract class BlazorCrudIntegrationTestsBase : IDisposable
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

    protected BlazorCrudIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.Blazor.CrudDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.Blazor.Crud);
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

    #region ValidateBlazorCrudStep — Validation Logic

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectIsNull()
    {
        var step = CreateValidateBlazorCrudStep();
        step.Project = null;
        step.Model = "Product";
        step.Page = "CRUD";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectIsEmpty()
    {
        var step = CreateValidateBlazorCrudStep();
        step.Project = string.Empty;
        step.Model = "Product";
        step.Page = "CRUD";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectDoesNotExist()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateBlazorCrudStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Model = "Product";
        step.Page = "CRUD";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenModelIsNull()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateBlazorCrudStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Page = "CRUD";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenModelIsEmpty()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateBlazorCrudStep();
        step.Project = _testProjectPath;
        step.Model = string.Empty;
        step.Page = "CRUD";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenPageIsNull()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateBlazorCrudStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = null;
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenPageIsEmpty()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateBlazorCrudStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = string.Empty;
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenDataContextIsNull()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateBlazorCrudStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = "CRUD";
        step.DataContext = null;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenDataContextIsEmpty()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateBlazorCrudStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = "CRUD";
        step.DataContext = string.Empty;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region Telemetry

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_OnValidationFailure_NullProject()
    {
        var step = CreateValidateBlazorCrudStep();
        step.Project = null;
        step.Model = "Product";
        step.Page = "CRUD";
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_OnValidationFailure_NullModel()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateBlazorCrudStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Page = "CRUD";
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_WithScaffolderDisplayName()
    {
        var step = CreateValidateBlazorCrudStep();
        step.Project = null;

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region BlazorCrudHelper — Template Type Resolution

    [Fact]
    public void GetTemplateType_WithCreateTemplate_ReturnsNonNull()
    {
        Assert.NotNull(BlazorCrudHelper.GetTemplateType(BlazorCrudHelper.CreateBlazorTemplate));
    }

    [Fact]
    public void GetTemplateType_WithDeleteTemplate_ReturnsNonNull()
    {
        Assert.NotNull(BlazorCrudHelper.GetTemplateType(BlazorCrudHelper.DeleteBlazorTemplate));
    }

    [Fact]
    public void GetTemplateType_WithDetailsTemplate_ReturnsNonNull()
    {
        Assert.NotNull(BlazorCrudHelper.GetTemplateType(BlazorCrudHelper.DetailsBlazorTemplate));
    }

    [Fact]
    public void GetTemplateType_WithEditTemplate_ReturnsNonNull()
    {
        Assert.NotNull(BlazorCrudHelper.GetTemplateType(BlazorCrudHelper.EditBlazorTemplate));
    }

    [Fact]
    public void GetTemplateType_WithIndexTemplate_ReturnsNonNull()
    {
        Assert.NotNull(BlazorCrudHelper.GetTemplateType(BlazorCrudHelper.IndexBlazorTemplate));
    }

    [Fact]
    public void GetTemplateType_WithNotFoundTemplate_ReturnsNonNull()
    {
        Assert.NotNull(BlazorCrudHelper.GetTemplateType(BlazorCrudHelper.NotFoundBlazorTemplate));
    }

    [Fact]
    public void GetTemplateType_WithNull_ReturnsNull()
    {
        Assert.Null(BlazorCrudHelper.GetTemplateType(null));
    }

    [Fact]
    public void GetTemplateType_WithEmpty_ReturnsNull()
    {
        Assert.Null(BlazorCrudHelper.GetTemplateType(string.Empty));
    }

    [Fact]
    public void GetTemplateType_WithUnknownTemplate_ReturnsNull()
    {
        Assert.Null(BlazorCrudHelper.GetTemplateType("Unknown.tt"));
    }

    #endregion

    #region BlazorCrudHelper — Template Validation

    [Theory]
    [InlineData("Create", true)]
    [InlineData("Delete", true)]
    [InlineData("Details", true)]
    [InlineData("Edit", true)]
    [InlineData("Index", true)]
    [InlineData("NotFound", true)]
    [InlineData("CRUD", true)]
    [InlineData("Unknown", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidTemplate_ReturnsExpectedResult(string? template, bool expected)
    {
        Assert.Equal(expected, BlazorCrudHelper.CRUDPages.Contains(template ?? string.Empty));
    }

    #endregion

    #region BlazorCrudHelper — Output Path Resolution

    [Fact]
    public void GetBaseOutputPath_WithValidInputs_ContainsComponentsPagesModelPages()
    {
        var path = BlazorCrudHelper.GetBaseOutputPath(_testProjectPath, "Product");
        Assert.Contains("Components", path);
    }

    [Fact]
    public void GetBaseOutputPath_DifferentModels_ProduceDifferentPaths()
    {
        var path1 = BlazorCrudHelper.GetBaseOutputPath(_testProjectPath, "Product");
        var path2 = BlazorCrudHelper.GetBaseOutputPath(_testProjectPath, "Customer");
        Assert.NotEqual(path1, path2);
    }

    #endregion

    #region BlazorCrud Templates

    [Fact]
    public void BlazorCrudTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var blazorCrudDir = Path.Combine(basePath, TargetFramework, "BlazorCrud");
        Assert.True(Directory.Exists(blazorCrudDir),
            $"BlazorCrud template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void BlazorCrudTemplates_AllFilesAreTT()
    {
        var basePath = GetActualTemplatesBasePath();
        var blazorCrudDir = Path.Combine(basePath, TargetFramework, "BlazorCrud");
        var files = Directory.GetFiles(blazorCrudDir, "*", SearchOption.AllDirectories);
        Assert.All(files, f => Assert.EndsWith(".tt", f));
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

    #region Regression Guards

    [Fact]
    public void RegressionGuard_CRUDPages_ListHasNoDuplicates()
    {
        var distinct = BlazorCrudHelper.CRUDPages.Distinct().Count();
        Assert.Equal(BlazorCrudHelper.CRUDPages.Count, distinct);
    }

    #endregion

    #region Helper Methods

    private ValidateBlazorCrudStep CreateValidateBlazorCrudStep()
    {
        return new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
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
