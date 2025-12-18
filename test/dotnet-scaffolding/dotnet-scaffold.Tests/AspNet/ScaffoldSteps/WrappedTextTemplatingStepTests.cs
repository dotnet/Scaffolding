// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

public class WrappedTextTemplatingStepTests
{
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public WrappedTextTemplatingStepTests()
    {
        _mockScaffolder = new Mock<IScaffolder>();
        
        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenNoTemplatesToProcess()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        WrappedTextTemplatingStep step = new WrappedTextTemplatingStep(
            NullLogger<WrappedTextTemplatingStep>.Instance,
            mockTelemetryService.Object)
        {
            TextTemplatingProperties = []
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_UsesCancellationToken()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        WrappedTextTemplatingStep step = new WrappedTextTemplatingStep(
            NullLogger<WrappedTextTemplatingStep>.Instance,
            mockTelemetryService.Object)
        {
            TextTemplatingProperties = []
        };

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        // Act
        bool result = await step.ExecuteAsync(_context, cancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();

        // Act
        WrappedTextTemplatingStep step = new WrappedTextTemplatingStep(
            NullLogger<WrappedTextTemplatingStep>.Instance,
            mockTelemetryService.Object)
        {
            TextTemplatingProperties = []
        };

        // Assert
        Assert.NotNull(step);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetry()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        WrappedTextTemplatingStep step = new WrappedTextTemplatingStep(
            NullLogger<WrappedTextTemplatingStep>.Instance,
            mockTelemetryService.Object)
        {
            TextTemplatingProperties = []
        };

        ScaffolderContext testContext = new ScaffolderContext(_mockScaffolder.Object);

        // Act
        bool result = await step.ExecuteAsync(testContext, CancellationToken.None);

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
}
