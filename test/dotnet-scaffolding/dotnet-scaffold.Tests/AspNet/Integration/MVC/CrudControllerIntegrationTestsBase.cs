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
/// Shared base class for MVC Controller with EF (CRUD) integration tests across .NET versions.
/// Subclasses provide the target framework via <see cref="TargetFramework"/>.
/// </summary>
public abstract class CrudControllerIntegrationTestsBase : IDisposable
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

    protected CrudControllerIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.MVC.CrudDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.MVC.ControllerCrud);
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

    #region ValidateEfControllerStep — Validation Logic

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNullProject()
    {
        var step = CreateValidateEfControllerStep();
        step.Project = null;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithEmptyProject()
    {
        var step = CreateValidateEfControllerStep();
        step.Project = string.Empty;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNonExistentProject()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateEfControllerStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNullModel()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithEmptyModel()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = string.Empty;
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNullControllerName()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = null;
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithEmptyControllerName()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = string.Empty;
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNullControllerType()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = null;
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithEmptyControllerType()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = string.Empty;
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNullDataContext()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = null;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithEmptyDataContext()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = string.Empty;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region ValidateEfControllerStep — Telemetry

    [Fact]
    public async Task ValidateEfControllerStep_TracksTelemetry_OnNullProjectFailure()
    {
        var step = CreateValidateEfControllerStep();
        step.Project = null;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEfControllerStep_TracksTelemetry_OnNullModelFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEfControllerStep_TracksTelemetry_OnNullControllerNameFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = null;
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEfControllerStep_TracksTelemetry_OnNullControllerTypeFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = null;
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEfControllerStep_TracksTelemetry_OnNullDataContextFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = null;

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region Multiple Validation Failure Theories

    [Theory]
    [InlineData("ProductsController")]
    [InlineData("OrdersController")]
    [InlineData("CustomersController")]
    public async Task ValidateEfControllerStep_FailsValidation_ForVariousNames_WhenProjectMissing(string controllerName)
    {
        var step = CreateValidateEfControllerStep();
        step.Project = null;
        step.Model = "Product";
        step.ControllerName = controllerName;
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateEfControllerStep_FailsWithInvalidModel(string? model)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = model;
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateEfControllerStep_FailsWithInvalidControllerName(string? controllerName)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = controllerName;
        step.ControllerType = "MVC";
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

    #region Helper Methods

    private ValidateEfControllerStep CreateValidateEfControllerStep()
    {
        return new ValidateEfControllerStep(
            _mockFileSystem.Object,
            NullLogger<ValidateEfControllerStep>.Instance,
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
