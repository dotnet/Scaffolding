// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
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
/// Shared base class for Razor Pages CRUD integration tests across .NET versions.
/// Subclasses provide the target framework via <see cref="TargetFramework"/>.
/// </summary>
public abstract class RazorPagesCrudIntegrationTestsBase : IDisposable
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

    protected RazorPagesCrudIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.RazorPage.CrudDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.RazorPage.Crud);
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
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>";

    #region ValidateRazorPagesStep — Validation Logic

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithNullProject()
    {
        var step = CreateValidateRazorPagesStep();
        step.Project = null;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithEmptyProject()
    {
        var step = CreateValidateRazorPagesStep();
        step.Project = string.Empty;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithNonExistentProject()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateRazorPagesStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithNullModel()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithEmptyModel()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = string.Empty;
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithNullPage()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = null;
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithEmptyPage()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = string.Empty;
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithNullDataContext()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = null;
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithEmptyDataContext()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = string.Empty;
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region ValidateRazorPagesStep — Telemetry

    [Fact]
    public async Task ValidateRazorPagesStep_TracksTelemetry_OnNullProjectFailure()
    {
        var step = CreateValidateRazorPagesStep();
        step.Project = null;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_TracksTelemetry_OnNullModelFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_TracksTelemetry_OnNullPageFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = null;
        step.DataContext = "ShopDbContext";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_TracksTelemetry_OnNullDataContextFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = null;

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region Multiple Validation Failure Theories

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateRazorPagesStep_FailsWithInvalidModel(string? model)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = model;
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateRazorPagesStep_FailsWithInvalidPage(string? page)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = page;
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateRazorPagesStep_FailsWithInvalidDataContext(string? dataContext)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = dataContext;
        step.DatabaseProvider = PackageConstants.EfConstants.SQLite;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region CRUD Pages — Collection Tests

    [Fact]
    public void CrudPages_ContainsExpectedPageTypes()
    {
        var pages = BlazorCrudHelper.CRUDPages;
        Assert.Contains("CRUD", pages);
        Assert.Contains("Create", pages);
        Assert.Contains("Delete", pages);
        Assert.Contains("Details", pages);
        Assert.Contains("Edit", pages);
        Assert.Contains("Index", pages);
    }

    [Fact]
    public void CrudPages_ContainsNotFound()
    {
        Assert.Contains("NotFound", BlazorCrudHelper.CRUDPages);
    }

    #endregion

    #region EF Providers — Structure Tests

    [Fact]
    public void EfPackagesDict_ContainsAllFourProviders()
    {
        Assert.Equal(4, PackageConstants.EfConstants.EfPackagesDict.Count);
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.SQLite));
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

    #region Scaffolder Differentiation

    [Fact]
    public void RazorPagesCrud_IsDifferentFromMvcControllerCrud()
    {
        Assert.NotEqual(AspnetStrings.RazorPage.Crud, AspnetStrings.MVC.ControllerCrud);
    }

    [Fact]
    public void RazorPagesCrud_IsDifferentFromBlazorCrud()
    {
        Assert.NotEqual(AspnetStrings.RazorPage.Crud, AspnetStrings.Blazor.Crud);
    }

    [Fact]
    public void RazorPagesCrud_IsDifferentFromRazorPageEmpty()
    {
        Assert.NotEqual(AspnetStrings.RazorPage.Crud, AspnetStrings.RazorPage.Empty);
    }

    [Fact]
    public void RazorPagesCrud_Category_DiffersFromMvcCategory()
    {
        Assert.NotEqual(AspnetStrings.Catagories.RazorPages, AspnetStrings.Catagories.MVC);
    }

    #endregion

    #region RazorPages Templates

    [Fact]
    public void RazorPagesTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var razorPagesDir = Path.Combine(basePath, TargetFramework, "RazorPages");
        Assert.True(Directory.Exists(razorPagesDir),
            $"RazorPages template folder should exist for {TargetFramework}");
    }

    protected void AssertRazorPagesTemplateFileExists(string subfolder, string fileName)
    {
        var basePath = GetActualTemplatesBasePath();
        var filePath = Path.Combine(basePath, TargetFramework, "RazorPages", subfolder, fileName);
        Assert.True(File.Exists(filePath),
            $"Expected RazorPages template file '{subfolder}/{fileName}' not found for {TargetFramework}");
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

    private ValidateRazorPagesStep CreateValidateRazorPagesStep()
    {
        return new ValidateRazorPagesStep(
            _mockFileSystem.Object,
            NullLogger<ValidateRazorPagesStep>.Instance,
            _testTelemetryService);
    }

    protected static string GetActualTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

    protected Task<(int ExitCode, string Output, string Error)> RunBuildAsync(string workingDirectory)
        => ScaffoldCliHelper.RunBuildForFrameworkAsync(workingDirectory, TargetFramework);

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
