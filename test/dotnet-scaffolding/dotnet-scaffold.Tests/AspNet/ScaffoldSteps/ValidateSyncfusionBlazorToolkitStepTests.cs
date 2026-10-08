// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class ValidateSyncfusionBlazorToolkitStepTests : IDisposable
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ILogger<ValidateSyncfusionBlazorToolkitStep>> _mockLogger;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;
    private readonly string _tempDir;
    private readonly string _projectPath;

    public ValidateSyncfusionBlazorToolkitStepTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sfbt-validate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _projectPath = Path.Combine(_tempDir, "Test.csproj");
        File.WriteAllText(_projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");

        _mockFileSystem = new Mock<IFileSystem>();
        _mockLogger = new Mock<ILogger<ValidateSyncfusionBlazorToolkitStep>>();
        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns("Syncfusion Blazor Toolkit");
        _mockScaffolder.Setup(s => s.Name).Returns("syncfusion-blazor-toolkit");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
        GC.SuppressFinalize(this);
    }

    private ValidateSyncfusionBlazorToolkitStep CreateStep()
    {
        return new ValidateSyncfusionBlazorToolkitStep(
            _mockFileSystem.Object,
            _mockLogger.Object,
            _testTelemetryService)
        {
            Project = _projectPath,
            Prerelease = false,
        };
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectIsEmpty()
    {
        var step = new ValidateSyncfusionBlazorToolkitStep(
            _mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = string.Empty,
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectDoesNotExist()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_projectPath)).Returns(false);
        var step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectDirectoryMissing()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_projectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_tempDir)).Returns(false);
        var step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    private static string OsComponentsImportsRazor =>
        "Components" + System.IO.Path.DirectorySeparatorChar + "_Imports.razor";

    [Fact]
    public async Task ExecuteAsync_PrefersComponents_ImportsFile()
    {
        string componentsImports = Path.Combine(_tempDir, "Components", "_Imports.razor");
        string rootImports = Path.Combine(_tempDir, "_Imports.razor");
        Directory.CreateDirectory(Path.GetDirectoryName(componentsImports)!);
        File.WriteAllText(componentsImports, "@using Foo");
        File.WriteAllText(rootImports, "@using Bar");

        _mockFileSystem.Setup(fs => fs.FileExists(_projectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_tempDir)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(componentsImports)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(rootImports)).Returns(true);

        var step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);
        Assert.True(_context.Properties.TryGetValue(
            nameof(SyncfusionBlazorToolkitSettings), out var settingsObj));
        var settings = Assert.IsType<SyncfusionBlazorToolkitSettings>(settingsObj);
        // ImportsFile must use OS-native separators (backslash on
        // Windows, forward slash on Linux) so the CodeModifier's
        // EndsWith lookup against MSBuildWorkspace's AdditionalDocument
        // paths succeeds.
        Assert.Equal(OsComponentsImportsRazor, settings.ImportsFile);
        Assert.False(settings.ImportsFileSkipped);
    }

    [Fact]
    public async Task ExecuteAsync_FallsBackToRoot_ImportsFile()
    {
        string rootImports = Path.Combine(_tempDir, "_Imports.razor");
        File.WriteAllText(rootImports, "@using Foo");

        _mockFileSystem.Setup(fs => fs.FileExists(_projectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_tempDir)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(rootImports)).Returns(true);
        // Components/_Imports.razor does not exist
        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(_tempDir, "Components", "_Imports.razor"))).Returns(false);

        var step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);
        Assert.True(_context.Properties.TryGetValue(
            nameof(SyncfusionBlazorToolkitSettings), out var settingsObj));
        var settings = Assert.IsType<SyncfusionBlazorToolkitSettings>(settingsObj);
        Assert.Equal("_Imports.razor", settings.ImportsFile);
        Assert.False(settings.ImportsFileSkipped);
    }

    [Fact]
    public async Task ExecuteAsync_SoftFails_WhenNoImportsFileFound()
    {
        // No _Imports.razor at any level; no nested directories; no csproj.
        // NOTE: Moq applies the most-recently-added setup last, so the
        // generic It.IsAny<string> mock for FileExists must come BEFORE
        // the specific _projectPath mock — otherwise the specific one
        // is overridden.
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
        _mockFileSystem.Setup(fs => fs.FileExists(_projectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateDirectories(It.IsAny<string>(), "*", SearchOption.TopDirectoryOnly))
            .Returns(Enumerable.Empty<string>());
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>(), "*.csproj", SearchOption.TopDirectoryOnly))
            .Returns(Enumerable.Empty<string>());

        var step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);
        Assert.True(_context.Properties.TryGetValue(
            nameof(SyncfusionBlazorToolkitSettings), out var settingsObj));
        var settings = Assert.IsType<SyncfusionBlazorToolkitSettings>(settingsObj);
        Assert.Null(settings.ImportsFile);
        Assert.True(settings.ImportsFileSkipped);
    }

    [Fact]
    public async Task ExecuteAsync_DetectsExistingPackageReference()
    {
        // Write the .csproj to disk so the implementation can read it
        // via File.ReadAllText.
        File.WriteAllText(_projectPath, "<Project><ItemGroup><PackageReference Include=\"Syncfusion.Blazor.Toolkit\" Version=\"1.0.2\" /></ItemGroup></Project>");
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
        _mockFileSystem.Setup(fs => fs.FileExists(_projectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_tempDir)).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(_tempDir, "*.csproj", SearchOption.TopDirectoryOnly))
            .Returns(new[] { _projectPath });
        _mockFileSystem.Setup(fs => fs.EnumerateDirectories(_tempDir, "*", SearchOption.TopDirectoryOnly))
            .Returns(Enumerable.Empty<string>());

        var step = CreateStep();
        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(_context.Properties.TryGetValue(
            nameof(SyncfusionBlazorToolkitSettings), out var settingsObj));
        var settings = Assert.IsType<SyncfusionBlazorToolkitSettings>(settingsObj);
        Assert.True(settings.PackageAlreadyReferenced);
    }

    [Fact]
    public async Task ExecuteAsync_DetectsExistingServiceRegistration()
    {
        // Write a Program.cs on disk that the implementation can read.
        File.WriteAllText(Path.Combine(_tempDir, "Program.cs"), "builder.Services.AddSyncfusionBlazorToolkit();");
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
        _mockFileSystem.Setup(fs => fs.FileExists(_projectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(_tempDir, "Program.cs"))).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_tempDir)).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(_tempDir, "*.csproj", SearchOption.TopDirectoryOnly))
            .Returns(Enumerable.Empty<string>());
        _mockFileSystem.Setup(fs => fs.EnumerateDirectories(_tempDir, "*", SearchOption.TopDirectoryOnly))
            .Returns(Enumerable.Empty<string>());

        var step = CreateStep();
        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(_context.Properties.TryGetValue(
            nameof(SyncfusionBlazorToolkitSettings), out var settingsObj));
        var settings = Assert.IsType<SyncfusionBlazorToolkitSettings>(settingsObj);
        Assert.True(settings.ServicesAlreadyRegistered);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsExcludedDirectories_WhenSearchingForImports()
    {
        string binImports = Path.Combine(_tempDir, "bin", "_Imports.razor");
        string nestedImports = Path.Combine(_tempDir, "Pages", "_Imports.razor");
        Directory.CreateDirectory(Path.GetDirectoryName(binImports)!);
        Directory.CreateDirectory(Path.GetDirectoryName(nestedImports)!);
        File.WriteAllText(binImports, "@using Bin");
        File.WriteAllText(nestedImports, "@using Pages");

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
        _mockFileSystem.Setup(fs => fs.FileExists(_projectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(binImports)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(nestedImports)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(It.IsAny<string>(), "*.csproj", SearchOption.TopDirectoryOnly))
            .Returns(Enumerable.Empty<string>());
        _mockFileSystem.Setup(fs => fs.EnumerateDirectories(_tempDir, "*", SearchOption.TopDirectoryOnly))
            .Returns(new[] { Path.Combine(_tempDir, "bin"), Path.Combine(_tempDir, "Pages") });

        var step = CreateStep();
        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(_context.Properties.TryGetValue(
            nameof(SyncfusionBlazorToolkitSettings), out var settingsObj));
        var settings = Assert.IsType<SyncfusionBlazorToolkitSettings>(settingsObj);
        // Should have skipped bin/ and discovered the Pages/ one.
        // ImportsFile uses OS-native separators.
        Assert.Equal("Pages" + System.IO.Path.DirectorySeparatorChar + "_Imports.razor", settings.ImportsFile);
    }

    private class TestTelemetryService : ITelemetryService
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
}
