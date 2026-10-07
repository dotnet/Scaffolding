// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class ResolveSyncfusionBlazorToolkitThemeStepTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _projectPath;
    private readonly ScaffolderContext _context;
    private readonly Scaffolding.Core.Scaffolders.IScaffolder _scaffolder;

    public ResolveSyncfusionBlazorToolkitThemeStepTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "sfbt-resolve-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _projectPath = Path.Combine(_tempDir, "Test.csproj");
        File.WriteAllText(_projectPath, "<Project />");

        // Build a real ScaffolderContext with a minimal IScaffolder mock.
        var mockScaffolder = new Moq.Mock<Scaffolding.Core.Scaffolders.IScaffolder>();
        mockScaffolder.Setup(s => s.DisplayName).Returns("Syncfusion Blazor Toolkit");
        mockScaffolder.Setup(s => s.Name).Returns("syncfusion-blazor-toolkit");
        _scaffolder = mockScaffolder.Object;
        _context = new ScaffolderContext(_scaffolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
        GC.SuppressFinalize(this);
    }

    private void SeedSettings()
    {
        _context.Properties[nameof(SyncfusionBlazorToolkitSettings)] = new SyncfusionBlazorToolkitSettings
        {
            Project = _projectPath,
        };
    }

    private static readonly string OsAppRazor =
        "Components" + System.IO.Path.DirectorySeparatorChar + "App.razor";
    private static readonly string OsIndexHtml =
        "wwwroot" + System.IO.Path.DirectorySeparatorChar + "index.html";

    [Fact]
    public async Task ExecuteAsync_ResolvesAppRazor_WhenPresent()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "Components"));
        File.WriteAllText(Path.Combine(_tempDir, "Components", "App.razor"), "<html></html>");

        SeedSettings();
        var fs = new Microsoft.DotNet.Scaffolding.Internal.Services.FileSystem();
        var step = new ResolveSyncfusionBlazorToolkitThemeStep(fs, NullLogger<ResolveSyncfusionBlazorToolkitThemeStep>.Instance);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);
        var settings = (SyncfusionBlazorToolkitSettings)_context.Properties[nameof(SyncfusionBlazorToolkitSettings)]!;
        // The stored path uses OS-native separators (backslash on
        // Windows, forward slash on Linux) so the CodeModifier's
        // EndsWith lookup against MSBuildWorkspace's AdditionalDocument
        // paths succeeds.
        Assert.Equal(OsAppRazor, settings.ThemeFile);
        Assert.False(settings.ThemeFileSkipped);
    }

    [Fact]
    public async Task ExecuteAsync_FallsBackToIndexHtml_WhenNoAppRazor()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "wwwroot"));
        File.WriteAllText(Path.Combine(_tempDir, "wwwroot", "index.html"), "<html></html>");

        SeedSettings();
        var fs = new Microsoft.DotNet.Scaffolding.Internal.Services.FileSystem();
        var step = new ResolveSyncfusionBlazorToolkitThemeStep(fs, NullLogger<ResolveSyncfusionBlazorToolkitThemeStep>.Instance);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);
        var settings = (SyncfusionBlazorToolkitSettings)_context.Properties[nameof(SyncfusionBlazorToolkitSettings)]!;
        Assert.Equal(OsIndexHtml, settings.ThemeFile);
        Assert.False(settings.ThemeFileSkipped);
    }

    [Fact]
    public async Task ExecuteAsync_StoresPathInOsNativeForm()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "Components"));
        File.WriteAllText(Path.Combine(_tempDir, "Components", "App.razor"), "<html></html>");

        SeedSettings();
        var fs = new Microsoft.DotNet.Scaffolding.Internal.Services.FileSystem();
        var step = new ResolveSyncfusionBlazorToolkitThemeStep(fs, NullLogger<ResolveSyncfusionBlazorToolkitThemeStep>.Instance);

        await step.ExecuteAsync(_context, CancellationToken.None);
        var settings = (SyncfusionBlazorToolkitSettings)_context.Properties[nameof(SyncfusionBlazorToolkitSettings)]!;
        // The path uses the OS-native directory separator (backslash on
        // Windows, forward slash on Linux). Canonical forward-slash paths
        // were a bug: the CodeModifier's EndsWith lookup would fail to
        // match MSBuildWorkspace's AdditionalDocument paths on Windows
        // and silently skip the theme stylesheet injection.
        if (Path.DirectorySeparatorChar == '\\')
        {
            Assert.Contains("\\", settings.ThemeFile);
            Assert.DoesNotContain("/", settings.ThemeFile);
        }
        else
        {
            Assert.Contains("/", settings.ThemeFile);
            Assert.DoesNotContain("\\", settings.ThemeFile);
        }
        Assert.Equal(OsAppRazor, settings.ThemeFile);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsThemeSoftly_WhenNoHostFound()
    {
        // Project directory exists but neither Components/App.razor nor wwwroot/index.html does.
        SeedSettings();
        var fs = new Microsoft.DotNet.Scaffolding.Internal.Services.FileSystem();
        var step = new ResolveSyncfusionBlazorToolkitThemeStep(fs, NullLogger<ResolveSyncfusionBlazorToolkitThemeStep>.Instance);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);
        var settings = (SyncfusionBlazorToolkitSettings)_context.Properties[nameof(SyncfusionBlazorToolkitSettings)]!;
        Assert.Null(settings.ThemeFile);
        Assert.True(settings.ThemeFileSkipped);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenSettingsMissing()
    {
        // No SyncfusionBlazorToolkitSettings in context.
        var fs = new Microsoft.DotNet.Scaffolding.Internal.Services.FileSystem();
        var step = new ResolveSyncfusionBlazorToolkitThemeStep(fs, NullLogger<ResolveSyncfusionBlazorToolkitThemeStep>.Instance);
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }
}
