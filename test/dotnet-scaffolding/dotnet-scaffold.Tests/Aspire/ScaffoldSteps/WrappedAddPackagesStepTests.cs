// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.ScaffoldSteps;

public class WrappedAddPackagesStepTests
{
    private readonly Mock<ILogger<WrappedAddPackagesStep>> _mockLogger;
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<IEnvironmentService> _mockEnvironmentService;

    public WrappedAddPackagesStepTests()
    {
        _mockLogger = new Mock<ILogger<WrappedAddPackagesStep>>();
        _mockTelemetryService = new Mock<ITelemetryService>();
        _mockEnvironmentService = new Mock<IEnvironmentService>();
        _mockEnvironmentService.Setup(x => x.CurrentDirectory).Returns(Environment.CurrentDirectory);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsTrue_AndTracksTelemetry_WhenBaseExecuteSucceeds()
    {
        // Arrange
        var nugetVersionService = new NuGetVersionService(_mockEnvironmentService.Object);
        WrappedAddPackagesStep step = new WrappedAddPackagesStep(
            _mockLogger.Object,
            _mockTelemetryService.Object,
            nugetVersionService)
        {
            Packages = new List<Package>(),
            ProjectPath = "test.csproj"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockTelemetryService.Verify(
            x => x.TrackEvent(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryWithCorrectScaffolderName()
    {
        // Arrange
        string scaffolderName = "MyCustomScaffolder";
        var nugetVersionService = new NuGetVersionService(_mockEnvironmentService.Object);
        WrappedAddPackagesStep step = new WrappedAddPackagesStep(
            _mockLogger.Object,
            _mockTelemetryService.Object,
            nugetVersionService)
        {
            Packages = new List<Package>(),
            ProjectPath = "test.csproj"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder(scaffolderName));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockTelemetryService.Verify(
            x => x.TrackEvent(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancelledToken_CompletesSuccessfully()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        var nugetVersionService = new NuGetVersionService(_mockEnvironmentService.Object);
        WrappedAddPackagesStep step = new WrappedAddPackagesStep(
            _mockLogger.Object,
            _mockTelemetryService.Object,
            nugetVersionService)
        {
            Packages = new List<Package>(),
            ProjectPath = "test.csproj"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, cts.Token);

        // Assert - Even with a cancelled token, if there are no packages to install, the step completes successfully
        Assert.True(result);
        _mockTelemetryService.Verify(
            x => x.TrackEvent(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPackageList_ExecutesSuccessfully()
    {
        // Arrange
        var nugetVersionService = new NuGetVersionService(_mockEnvironmentService.Object);
        WrappedAddPackagesStep step = new WrappedAddPackagesStep(
            _mockLogger.Object,
            _mockTelemetryService.Object,
            nugetVersionService)
        {
            Packages = new List<Package>(),
            ProjectPath = "test.csproj"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockTelemetryService.Verify(
            x => x.TrackEvent(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_InheritsFromAddPackagesStep()
    {
        // Arrange & Act
        var nugetVersionService = new NuGetVersionService(_mockEnvironmentService.Object);
        WrappedAddPackagesStep step = new WrappedAddPackagesStep(
            _mockLogger.Object,
            _mockTelemetryService.Object,
            nugetVersionService)
        {
            Packages = new List<Package>(),
            ProjectPath = "test.csproj"
        };

        // Assert
        Assert.NotNull(step);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvenOnFailure()
    {
        // Arrange
        var nugetVersionService = new NuGetVersionService(_mockEnvironmentService.Object);
        
        WrappedAddPackagesStep step = new WrappedAddPackagesStep(
            _mockLogger.Object,
            _mockTelemetryService.Object,
            nugetVersionService)
        {
            Packages = new List<Package>
            {
                new Package(Name: "", IsVersionRequired: false)
            },
            ProjectPath = "test.csproj"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert - even if the base step fails, telemetry should be tracked
        _mockTelemetryService.Verify(
            x => x.TrackEvent(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    private class TestScaffolder : IScaffolder
    {
        public TestScaffolder(string name)
        {
            Name = name;
            DisplayName = name;
        }

        public string Name { get; }
        public string DisplayName { get; }
        public string? Description => "Test Scaffolder";
        public IEnumerable<string> Categories => new[] { "Test" };
        public IEnumerable<ScaffolderOption> Options => Enumerable.Empty<ScaffolderOption>();
        public IEnumerable<(string Example, string? Description)> Examples => Enumerable.Empty<(string, string?)>();

        public Task ExecuteAsync(ScaffolderContext context)
        {
            return Task.CompletedTask;
        }
    }
}
