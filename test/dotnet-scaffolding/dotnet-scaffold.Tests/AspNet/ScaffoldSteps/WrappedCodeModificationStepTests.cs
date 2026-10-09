// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class WrappedCodeModificationStepTests
{
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public WrappedCodeModificationStepTests()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        _mockScaffolder = new Mock<IScaffolder>();
        
        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();

        // Act
        WrappedCodeModificationStep step = new WrappedCodeModificationStep(
            NullLogger<WrappedCodeModificationStep>.Instance,
            mockTelemetryService.Object)
        {
            CodeChangeOptions = [],
            ProjectPath = string.Empty
        };

        // Assert
        Assert.NotNull(step);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenNoCodeModifierConfigProvided()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        WrappedCodeModificationStep step = new WrappedCodeModificationStep(
            NullLogger<WrappedCodeModificationStep>.Instance,
            mockTelemetryService.Object)
        {
            CodeChangeOptions = [],
            ProjectPath = string.Empty
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetry()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        WrappedCodeModificationStep step = new WrappedCodeModificationStep(
            NullLogger<WrappedCodeModificationStep>.Instance,
            mockTelemetryService.Object)
        {
            CodeChangeOptions = [],
            ProjectPath = string.Empty
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
        // Verify telemetry was tracked
        mockTelemetryService.Verify(
            ts => ts.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<System.Collections.Generic.IReadOnlyDictionary<string, string>>(),
                It.IsAny<System.Collections.Generic.IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("""{"FileBlock":"missing.razor"}""", false, "missing.razor")]
    [InlineData("""{"FileBlock":"block.razor","Block":"text"}""", false, "cannot be combined")]
    [InlineData("""{"FileBlock":"block.razor","MultiLineBlock":["text"]}""", false, "cannot be combined")]
    [InlineData("""{"FileBlock":"block.razor"}""", true, "requires a file-based configuration")]
    public async Task ExecuteAsync_RejectsInvalidFileBlock(string snippet, bool inlineConfig, string expectedDiagnostic)
    {
        var directory = Path.Combine(Path.GetTempPath(), nameof(WrappedCodeModificationStepTests), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        try
        {
            var configPath = Path.Combine(directory, "changes.json");
            var projectPath = Path.Combine(directory, "TestProject.csproj");
            var config = $$"""
                {"Files":[{"FileName":"Program.cs","Replacements":[{{snippet}}]}]}
                """;
            File.WriteAllText(configPath, config);
            File.WriteAllText(projectPath, "<Project />");
            var logger = new Mock<ILogger<WrappedCodeModificationStep>>();
            var step = new WrappedCodeModificationStep(logger.Object, Mock.Of<ITelemetryService>())
            {
                CodeModifierConfigPath = inlineConfig ? null : configPath,
                CodeModifierConfigJsonText = inlineConfig ? config : null,
                CodeChangeOptions = [],
                ProjectPath = projectPath
            };

            Assert.False(await step.ExecuteAsync(_context));
            Assert.Contains(logger.Invocations, invocation =>
                invocation.Method.Name == nameof(ILogger.Log) &&
                Equals(invocation.Arguments[0], LogLevel.Error) &&
                invocation.Arguments[2].ToString()!.Contains(expectedDiagnostic));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
    [Fact]
    public async Task ExecuteAsync_UpdatesWebAssemblyRoutesWithAuthorizeRouteView()
    {
        string testDirectory = Path.Combine(
            Path.GetTempPath(),
            nameof(WrappedCodeModificationStepTests),
            Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        try
        {
            string projectPath = Path.Combine(testDirectory, "TestClient.csproj");
            string routesPath = Path.Combine(testDirectory, "Routes.razor");
            File.WriteAllText(
                projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
                  <PropertyGroup>
                    <TargetFramework>net11.0</TargetFramework>
                  </PropertyGroup>
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(testDirectory, "Program.cs"),
                """
                using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

                var builder = WebAssemblyHostBuilder.CreateDefault(args);
                await builder.Build().RunAsync();
                """);
            File.WriteAllText(
                Path.Combine(testDirectory, "_Imports.razor"),
                "@using Microsoft.AspNetCore.Components.Forms");
            File.WriteAllText(
                routesPath,
                """
                <Router AppAssembly="typeof(Program).Assembly">
                    <Found Context="routeData">
                        <RouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)" />
                        <FocusOnNavigate RouteData="routeData" Selector="h1" />
                    </Found>
                </Router>
                """);

            var telemetryService = new Mock<ITelemetryService>();
            var step = new WrappedCodeModificationStep(
                NullLogger<WrappedCodeModificationStep>.Instance,
                telemetryService.Object)
            {
                CodeChangeOptions = [],
                CodeModifierConfigPath = GetWasmConfigPath(),
                ProjectPath = projectPath
            };

            bool result = await step.ExecuteAsync(_context, CancellationToken.None);

            Assert.True(result);
            string routesContent = File.ReadAllText(routesPath);
            Assert.Contains("<AuthorizeRouteView", routesContent);
            Assert.Contains("<RedirectToLogin />", routesContent);
            Assert.DoesNotContain("<RouteView ", routesContent);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData("net10.0", "WebAssembly")]
    [InlineData("net10.0", "Auto")]
    [InlineData("net11.0", "WebAssembly")]
    [InlineData("net11.0", "Auto")]
    public async Task ExecuteAsync_HostedBlazorApp_WiresClientAuthorization(string targetFramework, string interactivity)
    {
        string root = Path.Combine(Path.GetTempPath(), nameof(WrappedCodeModificationStepTests), Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        try
        {
            var created = await ScaffoldCliHelper.RunDotNetAsync(
                root, "new", "blazor", "-n", "TestProject", "-o", root,
                "-f", targetFramework, "-int", interactivity, "-ai", "--no-restore");
            Assert.True(created.ExitCode == 0, $"dotnet new failed: {created.Output}\n{created.Error}");
            string serverDirectory = Path.Combine(root, "TestProject");
            string serverProjectPath = Path.Combine(serverDirectory, "TestProject.csproj");
            string clientDirectory = Path.Combine(root, "TestProject.Client");
            string clientProjectPath = Path.Combine(clientDirectory, "TestProject.Client.csproj");
            string routesPath = Path.Combine(clientDirectory, "Routes.razor");
            Assert.Contains("<RouteView ", File.ReadAllText(routesPath));

            var detector = new DetectBlazorWasmStep(
                NullLogger<DetectBlazorWasmStep>.Instance, FileSystem.Instance, Mock.Of<ITelemetryService>())
            {
                ProjectPath = serverProjectPath
            };
            Assert.True(await detector.ExecuteAsync(_context));
            Assert.Equal(clientProjectPath, _context.Properties["BlazorWasmClientProjectPath"]);

            string wasmConfigPath = GetWasmConfigPath(targetFramework);
            foreach (var (projectPath, configPath) in new[]
            {
                (serverProjectPath, Path.Combine(Path.GetDirectoryName(wasmConfigPath)!, "blazorEntraChanges.json")),
                (clientProjectPath, wasmConfigPath)
            })
            {
                var step = new WrappedCodeModificationStep(
                    NullLogger<WrappedCodeModificationStep>.Instance, Mock.Of<ITelemetryService>())
                {
                    CodeChangeOptions = [],
                    CodeModifierConfigPath = configPath,
                    ProjectPath = projectPath
                };
                Assert.True(await step.ExecuteAsync(_context));
            }

            var model = new EntraIdModel
            {
                ProjectInfo = new ProjectInfo(serverProjectPath),
                BaseOutputPath = serverDirectory,
                EntraIdNamespace = "TestProject"
            };
            var templateStep = new WrappedTextTemplatingStep(
                NullLogger<WrappedTextTemplatingStep>.Instance, Mock.Of<ITelemetryService>())
            {
                TextTemplatingProperties = EntraIdHelper.GetTextTemplatingProperties(
                    [Path.Combine("BlazorEntraId", "RedirectToLogin.tt")], model, clientProjectPath)
            };
            Assert.True(await templateStep.ExecuteAsync(_context));

            string routes = File.ReadAllText(routesPath);
            Assert.Contains("<AuthorizeRouteView", routes);
            Assert.Contains("<NotAuthorized>", routes);
            Assert.Contains("<RedirectToLogin />", routes);
            Assert.DoesNotContain("<RouteView ", routes);
            Assert.Contains("authentication/login?returnUrl=",
                File.ReadAllText(Path.Combine(clientDirectory, "RedirectToLogin.razor")));
            Assert.Contains("AddAuthenticationStateDeserialization",
                File.ReadAllText(Path.Combine(clientDirectory, "Program.cs")));
            Assert.Contains("AddAuthenticationStateSerialization",
                File.ReadAllText(Path.Combine(serverDirectory, "Program.cs")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string GetWasmConfigPath(string targetFramework = "net11.0")
    {
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.GetFullPath(Path.Combine(
            assemblyDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "dotnet-scaffolding",
            "dotnet-scaffold",
            "AspNet",
            "Templates",
            targetFramework,
            "CodeModificationConfigs",
            "blazorWasmEntraChanges.json"));
    }
}
