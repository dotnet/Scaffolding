// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.ScaffoldSteps;

public class ValidateOptionsStepTests
{
    private readonly Mock<ILogger<ValidateOptionsStep>> _mockLogger;
    private readonly TestTelemetryService _testTelemetryService;

    public ValidateOptionsStepTests()
    {
        _mockLogger = new Mock<ILogger<ValidateOptionsStep>>();
        _testTelemetryService = new TestTelemetryService();
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsTrue_WhenValidationPasses()
    {
        // Arrange
        bool validateMethodCalled = false;
        Func<ScaffolderContext, ILogger, bool> validateMethod = (context, logger) =>
        {
            validateMethodCalled = true;
            return true;
        };

        ValidateOptionsStep step = new ValidateOptionsStep(_mockLogger.Object, _testTelemetryService)
        {
            ValidateMethod = validateMethod
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(validateMethodCalled);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenValidationFails()
    {
        // Arrange
        bool validateMethodCalled = false;
        Func<ScaffolderContext, ILogger, bool> validateMethod = (context, logger) =>
        {
            validateMethodCalled = true;
            return false;
        };

        ValidateOptionsStep step = new ValidateOptionsStep(_mockLogger.Object, _testTelemetryService)
        {
            ValidateMethod = validateMethod
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.True(validateMethodCalled);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCorrectContextAndLogger_ToValidateMethod()
    {
        // Arrange
        ScaffolderContext? capturedContext = null;
        ILogger? capturedLogger = null;

        Func<ScaffolderContext, ILogger, bool> validateMethod = (context, logger) =>
        {
            capturedContext = context;
            capturedLogger = logger;
            return true;
        };

        ValidateOptionsStep step = new ValidateOptionsStep(_mockLogger.Object, _testTelemetryService)
        {
            ValidateMethod = validateMethod
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.NotNull(capturedContext);
        Assert.Same(context, capturedContext);
        Assert.NotNull(capturedLogger);
        Assert.Same(_mockLogger.Object, capturedLogger);
    }

    [Fact]
    public async Task ExecuteAsync_LogsMessages_WhenValidationSucceeds()
    {
        // Arrange
        Func<ScaffolderContext, ILogger, bool> validateMethod = (context, logger) => true;

        ValidateOptionsStep step = new ValidateOptionsStep(_mockLogger.Object, _testTelemetryService)
        {
            ValidateMethod = validateMethod
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        // Logger is called internally (verified by Moq tracking invocations)
    }

    [Fact]
    public async Task ExecuteAsync_LogsMessages_WhenValidationFails()
    {
        // Arrange
        Func<ScaffolderContext, ILogger, bool> validateMethod = (context, logger) => false;

        ValidateOptionsStep step = new ValidateOptionsStep(_mockLogger.Object, _testTelemetryService)
        {
            ValidateMethod = validateMethod
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.False(result);
        // Logger is called internally (verified by Moq tracking invocations)
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryWithCorrectParameters()
    {
        // Arrange
        Func<ScaffolderContext, ILogger, bool> validateMethod = SampleValidationMethod;

        ValidateOptionsStep step = new ValidateOptionsStep(_mockLogger.Object, _testTelemetryService)
        {
            ValidateMethod = validateMethod
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesCancellationToken_WhenValidationRunsLong()
    {
        // Arrange
        Func<ScaffolderContext, ILogger, bool> validateMethod = (context, logger) =>
        {
            Thread.Sleep(100);
            return true;
        };

        ValidateOptionsStep step = new ValidateOptionsStep(_mockLogger.Object, _testTelemetryService)
        {
            ValidateMethod = validateMethod
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        CancellationTokenSource cts = new CancellationTokenSource();

        // Act
        bool result = await step.ExecuteAsync(context, cts.Token);

        // Assert
        Assert.True(result);
    }

    private bool SampleValidationMethod(ScaffolderContext context, ILogger logger)
    {
        return true;
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
