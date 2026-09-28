// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
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

    private static string GetWasmConfigPath()
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
            "net11.0",
            "CodeModificationConfigs",
            "blazorWasmEntraChanges.json"));
    }
}
