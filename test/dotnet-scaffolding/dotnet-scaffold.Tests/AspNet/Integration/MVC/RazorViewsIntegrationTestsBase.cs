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
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration;

/// <summary>
/// Shared base class for Razor Views (CRUD) integration tests across .NET versions.
/// The 'views' scaffolder generates Razor views for Create, Delete, Details, Edit and List
/// operations for a given model.
/// Subclasses provide the target framework via <see cref="TargetFramework"/>.
/// </summary>
public abstract class RazorViewsIntegrationTestsBase : IDisposable
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

    protected RazorViewsIntegrationTestsBase()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), TestClassName, Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.RazorView.ViewsDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.RazorView.Views);
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

    #region ValidateViewsStep — Validation Logic

    [Fact]
    public async Task ValidateViewsStep_FailsWithNullProject()
    {
        var step = CreateValidateViewsStep();
        step.Project = null;
        step.Model = "Product";
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithEmptyProject()
    {
        var step = CreateValidateViewsStep();
        step.Project = string.Empty;
        step.Model = "Product";
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithNonExistentProject()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateViewsStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Model = "Product";
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithNullModel()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithEmptyModel()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = string.Empty;
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithNullPage()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = null;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateViewsStep_FailsWithEmptyPage()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = string.Empty;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateViewsStep_FailsWithInvalidProject(string? project)
    {
        var step = CreateValidateViewsStep();
        step.Project = project;
        step.Model = "Product";
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateViewsStep_FailsWithInvalidModel(string? model)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = model;
        step.Page = "CRUD";

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateViewsStep_FailsWithInvalidPage(string? page)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = page;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region ValidateViewsStep — Telemetry

    [Fact]
    public async Task ValidateViewsStep_TracksTelemetry_OnNullProjectFailure()
    {
        var step = CreateValidateViewsStep();
        step.Project = null;
        step.Model = "Product";
        step.Page = "CRUD";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateViewsStep_TracksTelemetry_OnNullModelFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Page = "CRUD";

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateViewsStep_TracksTelemetry_OnNullPageFailure()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateViewsStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.Page = null;

        await step.ExecuteAsync(_context);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region BlazorCrudHelper — CRUDPages

    [Fact]
    public void CRUDPages_Has7Entries()
    {
        Assert.Equal(7, BlazorCrudHelper.CRUDPages.Count);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Delete")]
    [InlineData("Details")]
    [InlineData("Edit")]
    [InlineData("Index")]
    [InlineData("CRUD")]
    [InlineData("NotFound")]
    public void CRUDPages_ContainsPageType(string pageType)
    {
        Assert.Contains(pageType, BlazorCrudHelper.CRUDPages);
    }

    #endregion

    #region ViewHelper — Template Constants (non-string-comparison)

    [Fact]
    public void ViewHelper_CreateTemplate_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(ViewHelper.CreateTemplate));
    }

    [Fact]
    public void ViewHelper_DeleteTemplate_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(ViewHelper.DeleteTemplate));
    }

    [Fact]
    public void ViewHelper_DetailsTemplate_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(ViewHelper.DetailsTemplate));
    }

    [Fact]
    public void ViewHelper_EditTemplate_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(ViewHelper.EditTemplate));
    }

    [Fact]
    public void ViewHelper_IndexTemplate_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(ViewHelper.IndexTemplate));
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

    [Fact]
    public void ViewsTemplates_DoesNotHaveFlatT4Templates()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var ttFiles = Directory.GetFiles(viewsDir, "*.tt", SearchOption.TopDirectoryOnly);
        Assert.Empty(ttFiles);
    }

    [Theory]
    [InlineData("Bootstrap4", "Create.cshtml")]
    [InlineData("Bootstrap4", "Delete.cshtml")]
    [InlineData("Bootstrap4", "Details.cshtml")]
    [InlineData("Bootstrap4", "Edit.cshtml")]
    [InlineData("Bootstrap4", "Empty.cshtml")]
    [InlineData("Bootstrap4", "List.cshtml")]
    [InlineData("Bootstrap4", "_ValidationScriptsPartial.cshtml")]
    [InlineData("Bootstrap5", "Create.cshtml")]
    [InlineData("Bootstrap5", "Delete.cshtml")]
    [InlineData("Bootstrap5", "Details.cshtml")]
    [InlineData("Bootstrap5", "Edit.cshtml")]
    [InlineData("Bootstrap5", "Empty.cshtml")]
    [InlineData("Bootstrap5", "List.cshtml")]
    [InlineData("Bootstrap5", "_ValidationScriptsPartial.cshtml")]
    public void ViewsTemplates_HasExpectedFile(string subfolder, string fileName)
    {
        var basePath = GetActualTemplatesBasePath();
        var filePath = Path.Combine(basePath, TargetFramework, "Views", subfolder, fileName);
        Assert.True(File.Exists(filePath),
            $"Expected Views template file '{subfolder}/{fileName}' not found for {TargetFramework}");
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
    public void ViewsScaffolderName_NotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.Views));
    }

    [Fact]
    public void ViewsScaffolderDisplayName_NotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsDisplayName));
    }

    [Fact]
    public void ViewsScaffolderDescription_NotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsDescription));
    }

    [Fact]
    public void ViewsExample1_NotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample1));
    }

    [Fact]
    public void ViewsExample2_NotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample2));
    }

    [Fact]
    public void ViewsExample1Description_NotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample1Description));
    }

    [Fact]
    public void ViewsExample2Description_NotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspnetStrings.RazorView.ViewsExample2Description));
    }

    [Fact]
    public void ViewExtension_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspNetConstants.ViewExtension));
    }

    [Fact]
    public void PageTypeOption_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AspNetConstants.CliOptions.PageTypeOption));
    }

    #endregion

    #region Helper Methods

    private ValidateViewsStep CreateValidateViewsStep()
    {
        return new ValidateViewsStep(
            _mockFileSystem.Object,
            NullLogger<ValidateViewsStep>.Instance,
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
