// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class WrappedAddPackagesStepTests
{
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public WrappedAddPackagesStepTests()
    {
        _mockScaffolder = new Mock<IScaffolder>();
        
        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsTrue_WhenNoPackagesToAdd()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        mockEnvironmentService.Setup(e => e.CurrentDirectory).Returns(System.IO.Directory.GetCurrentDirectory());
        NuGetVersionService nugetVersionService = new NuGetVersionService(mockEnvironmentService.Object);
        
        WrappedAddPackagesStep step = new WrappedAddPackagesStep(
            NullLogger<WrappedAddPackagesStep>.Instance,
            mockTelemetryService.Object,
            nugetVersionService)
        {
            Packages = Array.Empty<string>(),
            ProjectPath = string.Empty
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExecuteAsync_UsesCancellationToken()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        mockEnvironmentService.Setup(e => e.CurrentDirectory).Returns(System.IO.Directory.GetCurrentDirectory());
        NuGetVersionService nugetVersionService = new NuGetVersionService(mockEnvironmentService.Object);
        
        WrappedAddPackagesStep step = new WrappedAddPackagesStep(
            NullLogger<WrappedAddPackagesStep>.Instance,
            mockTelemetryService.Object,
            nugetVersionService)
        {
            Packages = Array.Empty<string>(),
            ProjectPath = string.Empty
        };

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        // Act
        bool result = await step.ExecuteAsync(_context, cancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        Mock<ITelemetryService> mockTelemetryService = new Mock<ITelemetryService>();
        Mock<IEnvironmentService> mockEnvironmentService = new Mock<IEnvironmentService>();
        mockEnvironmentService.Setup(e => e.CurrentDirectory).Returns(System.IO.Directory.GetCurrentDirectory());
        NuGetVersionService nugetVersionService = new NuGetVersionService(mockEnvironmentService.Object);

        // Act
        WrappedAddPackagesStep step = new WrappedAddPackagesStep(
            NullLogger<WrappedAddPackagesStep>.Instance,
            mockTelemetryService.Object,
            nugetVersionService)
        {
            Packages = Array.Empty<string>(),
            ProjectPath = string.Empty
        };

        // Assert
        Assert.NotNull(step);
    }
}
