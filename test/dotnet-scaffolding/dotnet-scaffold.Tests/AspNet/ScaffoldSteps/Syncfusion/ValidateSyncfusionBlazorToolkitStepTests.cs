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
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Extensions.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Scaffold.Tests.AspNet.ScaffoldSteps.Syncfusion;

public class ValidateSyncfusionBlazorToolkitStepTests : IDisposable
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ILogger<ValidateSyncfusionBlazorToolkitStep>> _mockLogger;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;
    private readonly string _tempDir;
    private readonly string _projectPath;

    // A project needs a TargetFramework to be evaluated by MSBuild at all.
    private const string WebProjectXml =
        "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>";

    public ValidateSyncfusionBlazorToolkitStepTests()
    {
        // The step now evaluates the project with MSBuild (no textual gate).
        if (!Microsoft.Build.Locator.MSBuildLocator.IsRegistered)
        {
            Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
        }

        _tempDir = Path.Combine(Path.GetTempPath(), "sfbt-validate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _projectPath = Path.GetFullPath(Path.Combine(_tempDir, "Test.csproj"));
        File.WriteAllText(_projectPath, WebProjectXml);
        // A Blazor Web App contains Razor components; a bare Microsoft.NET.Sdk.Web project is a Web API.
        File.WriteAllText(Path.Combine(_tempDir, "Home.razor"), "<h1>Home</h1>\n");

        _mockFileSystem = new Mock<IFileSystem>();
        _mockLogger = new Mock<ILogger<ValidateSyncfusionBlazorToolkitStep>>();
        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns("Syncfusion Blazor Toolkit");
        _mockScaffolder.Setup(s => s.Name).Returns("blazor-syncfusion-toolkit");
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

    // Messages the step logged at error level, so a failing assertion explains itself.
    private string LoggedErrors() => string.Join(" | ", _mockLogger.Invocations
        .Where(i => i.Method.Name == "Log" && i.Arguments[0] is LogLevel.Error)
        .Select(i => i.Arguments[2]?.ToString()));

    private ValidateSyncfusionBlazorToolkitStep CreateStep()
    {
        return new ValidateSyncfusionBlazorToolkitStep(
            _mockFileSystem.Object,
            _mockLogger.Object,
            _testTelemetryService)
        {
            Project = _projectPath,
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
    public async Task ExecuteAsync_NormalizesRelativeProjectPath_ToFullPath()
    {
        // A relative --project value such as "App.csproj" used to pass
        // File.Exists but leave Path.GetDirectoryName returning empty.
        // The fix normalizes the path to a full path before storing it
        // on the settings.
        // We simulate the relative input by switching the test process's
        // working directory to _tempDir via Directory.SetCurrentDirectory
        // so Path.GetFullPath("Test.csproj") resolves to our test file.
        string projectFileName = "Test.csproj";
        string fullPath = Path.Combine(_tempDir, projectFileName);
        File.WriteAllText(fullPath, WebProjectXml);

        string originalCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
        try
        {
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            _mockFileSystem.Setup(fs => fs.FileExists(It.IsRegex("Test\\.csproj$"))).Returns(true);
            _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

            var step = new ValidateSyncfusionBlazorToolkitStep(
                _mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
            {
                Project = projectFileName,
            };

            bool result = await step.ExecuteAsync(_context, CancellationToken.None);
            Assert.True(result, LoggedErrors());
            Assert.True(_context.Properties.TryGetValue(
                nameof(SyncfusionBlazorToolkitSettings), out var settingsObj));
            var settings = Assert.IsType<SyncfusionBlazorToolkitSettings>(settingsObj);
            Assert.Equal(fullPath, settings.Project);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_ForMauiHybridProject()
    {
        string mauiProject = Path.Combine(_tempDir, "MauiApp.csproj");
        File.WriteAllText(mauiProject,
            "<Project Sdk=\"Microsoft.NET.Sdk.Maui\">\n" +
            "<UseMaui>true</UseMaui>\n" +
            "</Project>");

        _mockFileSystem.Setup(fs => fs.FileExists(mauiProject)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        var step = new ValidateSyncfusionBlazorToolkitStep(
            _mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = mauiProject,
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_ForWebApiThatUsesTheWebSdk()
    {
        // Microsoft.NET.Sdk.Web is also the SDK of Web API / minimal API apps.
        string apiDir = Path.Combine(_tempDir, "Api");
        Directory.CreateDirectory(apiDir);
        string apiProject = Path.Combine(apiDir, "Api.csproj");
        File.WriteAllText(apiProject, WebProjectXml);
        File.WriteAllText(Path.Combine(apiDir, "Program.cs"), "var b = WebApplication.CreateBuilder(args);\nb.Build().Run();\n");

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns<string>(File.Exists);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        var step = new ValidateSyncfusionBlazorToolkitStep(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = apiProject,
        };

        Assert.False(await step.ExecuteAsync(_context, CancellationToken.None));
        Assert.False(_context.Properties.ContainsKey(nameof(SyncfusionBlazorToolkitSettings)));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenTheProjectCannotBeEvaluated()
    {
        // An unreadable/unevaluable project is rejected explicitly, not classified as "unknown" and let through.
        File.WriteAllText(_projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><Import Project=\"missing.props\" /></Project>");
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns<string>(File.Exists);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        Assert.False(await CreateStep().ExecuteAsync(_context, CancellationToken.None));
        Assert.Contains("could not determine the project type", LoggedErrors());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_ForNonBlazorProject()
    {
        string consoleProject = Path.Combine(_tempDir, "ConsoleApp.csproj");
        File.WriteAllText(consoleProject, "<Project Sdk=\"Microsoft.NET.Sdk\">\n</Project>");

        _mockFileSystem.Setup(fs => fs.FileExists(consoleProject)).Returns(true);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        var step = new ValidateSyncfusionBlazorToolkitStep(
            _mockFileSystem.Object, _mockLogger.Object, _testTelemetryService)
        {
            Project = consoleProject,
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReadsHostingFromEntryPointAndGlobalRenderModeFromAppRazor()
    {
        // Hosting support (what Program.cs registers) and the global render mode (App.razor)
        // are separate facts; neither is inferred from the other.
        File.WriteAllText(_projectPath, WebProjectXml);
        File.WriteAllText(Path.Combine(_tempDir, "Program.cs"),
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "builder.Services.AddRazorComponents().AddInteractiveServerComponents();\n" +
            "var app = builder.Build();\n" +
            "app.MapRazorComponents<App>().AddInteractiveServerRenderMode();\n" +
            "app.Run();\n");
        string components = Path.Combine(_tempDir, "Components");
        Directory.CreateDirectory(components);
        File.WriteAllText(Path.Combine(components, "App.razor"), "<Routes @rendermode=\"InteractiveServer\" />\n");
        File.WriteAllText(Path.Combine(components, "Home.razor"), "<h1>Home</h1>\n");

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns<string>(File.Exists);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        bool result = await CreateStep().ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result, LoggedErrors());

        var settings = (SyncfusionBlazorToolkitSettings)_context.Properties[nameof(SyncfusionBlazorToolkitSettings)]!;
        Assert.Equal(BlazorAppKind.WebApp, settings.AppKind);
        Assert.Equal(BlazorHostingModes.Server, settings.Hosting.Supported);
        Assert.Equal("InteractiveServer", settings.Hosting.GlobalRenderMode);
    }

    [Fact]
    public async Task ExecuteAsync_NoGlobalRenderMode_DoesNotMeanNoInteractivity()
    {
        File.WriteAllText(Path.Combine(_tempDir, "Program.cs"),
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents();\n" +
            "var app = builder.Build();\n" +
            "app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AddInteractiveWebAssemblyRenderMode();\n" +
            "app.Run();\n");
        File.WriteAllText(Path.Combine(_tempDir, "Home.razor"), "<h1>Home</h1>\n");

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns<string>(File.Exists);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        bool result = await CreateStep().ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result, LoggedErrors());

        var settings = (SyncfusionBlazorToolkitSettings)_context.Properties[nameof(SyncfusionBlazorToolkitSettings)]!;
        Assert.Equal("None", settings.Hosting.GlobalRenderMode);
        Assert.Equal(BlazorHostingModes.Server | BlazorHostingModes.WebAssembly, settings.Hosting.Supported);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsInstalledPackageVersionBeforeTheRun()
    {
        File.WriteAllText(_projectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>" +
            "<ItemGroup><PackageReference Include=\"Syncfusion.Blazor.Toolkit\" Version=\"1.0.2\" /></ItemGroup></Project>");
        File.WriteAllText(Path.Combine(_tempDir, "Home.razor"), "<h1>Home</h1>\n");
        File.WriteAllText(Path.Combine(_tempDir, "Program.cs"),
            "var builder = WebApplication.CreateBuilder(args);\nvar app = builder.Build();\napp.Run();\n");

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns<string>(File.Exists);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        bool result = await CreateStep().ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result, LoggedErrors());

        var settings = (SyncfusionBlazorToolkitSettings)_context.Properties[nameof(SyncfusionBlazorToolkitSettings)]!;
        Assert.Equal("1.0.2", settings.Server.PackageVersionBefore);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenImportedPropsReferenceMissingClient()
    {
        // The ProjectReference is declared in an imported .props file, never in
        // the .csproj text. The step must still evaluate it and abort when the
        // referenced client project is missing.
        File.WriteAllText(Path.Combine(_tempDir, "refs.props"),
            "<Project><ItemGroup><ProjectReference Include=\"Missing/Missing.csproj\" /></ItemGroup></Project>");
        File.WriteAllText(_projectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><Import Project=\"refs.props\" /></Project>");

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns<string>(File.Exists);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        var step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.False(_context.Properties.ContainsKey(nameof(SyncfusionBlazorToolkitSettings)));
    }

    [Fact]
    public async Task ExecuteAsync_ResolvesClient_WhenReferenceComesFromImportedProps()
    {
        string clientDir = Path.Combine(_tempDir, "Client");
        Directory.CreateDirectory(clientDir);
        string clientProject = Path.Combine(clientDir, "Client.csproj");
        File.WriteAllText(clientProject,
            "<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(_tempDir, "refs.props"),
            "<Project><ItemGroup><ProjectReference Include=\"Client/Client.csproj\" /></ItemGroup></Project>");
        File.WriteAllText(_projectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk.Web\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>" +
            "<Import Project=\"refs.props\" /></Project>");

        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns<string>(File.Exists);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);

        var step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        var settings = (SyncfusionBlazorToolkitSettings)_context.Properties[nameof(SyncfusionBlazorToolkitSettings)]!;
        Assert.Equal(clientProject, settings.ClientProjectPath);
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