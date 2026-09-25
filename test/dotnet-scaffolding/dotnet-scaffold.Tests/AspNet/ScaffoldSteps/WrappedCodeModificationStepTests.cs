// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
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
}
