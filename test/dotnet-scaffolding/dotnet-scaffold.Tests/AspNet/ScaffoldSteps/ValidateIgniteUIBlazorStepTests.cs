// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class ValidateIgniteUIBlazorStepTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ILogger<ValidateIgniteUIBlazorStep>> _mockLogger;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly ScaffolderContext _context;
    private readonly string _testProjectPath;

    public ValidateIgniteUIBlazorStepTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();
        _mockLogger = new Mock<ILogger<ValidateIgniteUIBlazorStep>>();
        _testTelemetryService = new TestTelemetryService();
        _testProjectPath = Path.Combine("test", "project", "TestProject.csproj");

        var mockScaffolder = new Mock<IScaffolder>();
        mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(mockScaffolder.Object);
    }

    private ValidateIgniteUIBlazorStep CreateStep()
        => new(_mockFileSystem.Object, _mockLogger.Object, _testTelemetryService);

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectIsEmpty()
    {
        var step = CreateStep();
        step.Project = string.Empty;
        step.Package = "Lite";

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
        Assert.Equal("Failure", _testTelemetryService.TrackedEvents[0].Properties["Result"]);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectDoesNotExist()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(false);
        var step = CreateStep();
        step.Project = _testProjectPath;
        step.Package = "Lite";

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Grid")]
    [InlineData("Both")]
    public async Task ExecuteAsync_ReturnsFalse_WhenPackageIsInvalid(string? package)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        var step = CreateStep();
        step.Project = _testProjectPath;
        step.Package = package;

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
        Assert.False(_context.Properties.ContainsKey("IgniteUIBlazorSettings"));
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var step = CreateStep();
        step.Project = _testProjectPath;
        step.Package = "All";
        step.Theme = "material";
        step.ThemeVariant = "dark";
        step.Prerelease = true;

        Assert.Equal(_testProjectPath, step.Project);
        Assert.Equal("All", step.Package);
        Assert.Equal("material", step.Theme);
        Assert.Equal("dark", step.ThemeVariant);
        Assert.True(step.Prerelease);
    }

    [Fact]
    public void Properties_DefaultToNullAndFalse()
    {
        var step = CreateStep();

        Assert.Null(step.Project);
        Assert.Null(step.Package);
        Assert.Null(step.Theme);
        Assert.Null(step.ThemeVariant);
        Assert.False(step.Prerelease);
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
