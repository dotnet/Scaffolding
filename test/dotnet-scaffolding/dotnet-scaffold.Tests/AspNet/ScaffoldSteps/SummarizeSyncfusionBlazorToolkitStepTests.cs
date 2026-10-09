// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class SummarizeSyncfusionBlazorToolkitStepTests
{
    private readonly ScaffolderContext _context;
    private readonly Mock<IScaffolder> _mockScaffolder;

    public SummarizeSyncfusionBlazorToolkitStepTests()
    {
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns("Syncfusion Blazor Toolkit");
        _mockScaffolder.Setup(s => s.Name).Returns("syncfusion-blazor-toolkit");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_LogsInteractiveRenderModeGuidance()
    {
        _context.Properties[nameof(SyncfusionBlazorToolkitSettings)] = new SyncfusionBlazorToolkitSettings
        {
            Project = "Test.csproj",
            ImportsFile = "Components/_Imports.razor",
        };
        var logger = new TestLogger<SummarizeSyncfusionBlazorToolkitStep>();
        var step = new SummarizeSyncfusionBlazorToolkitStep(logger);

        await step.ExecuteAsync(_context, CancellationToken.None);

        // The "Next steps" block should mention interactive render mode.
        Assert.Contains(logger.Entries, e => e.Message.Contains("interactive render mode", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_ReportsIdempotentReRun_WhenAlreadyConfigured()
    {
        _context.Properties[nameof(SyncfusionBlazorToolkitSettings)] = new SyncfusionBlazorToolkitSettings
        {
            Project = "Test.csproj",
            ImportsFile = "Components/_Imports.razor",
            ImportsFileSkipped = false,
            PackageAlreadyReferenced = true,
            ServicesAlreadyRegistered = true,
        };
        var logger = new TestLogger<SummarizeSyncfusionBlazorToolkitStep>();
        var step = new SummarizeSyncfusionBlazorToolkitStep(logger);

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Contains(logger.Entries, e => e.Message.Contains("already fully configured", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_LogsSkippedTheme_WithInformation()
    {
        // With Syncfusion.Blazor.Toolkit 2.0.0+ no external stylesheet
        // is required, so a missing theme host is no longer a warning.
        // The summary step should still surface the absence at
        // Information level so users know that the detection was
        // attempted.
        _context.Properties[nameof(SyncfusionBlazorToolkitSettings)] = new SyncfusionBlazorToolkitSettings
        {
            Project = "Test.csproj",
        };
        var logger = new TestLogger<SummarizeSyncfusionBlazorToolkitStep>();
        var step = new SummarizeSyncfusionBlazorToolkitStep(logger);

        await step.ExecuteAsync(_context, CancellationToken.None);

        // Should mention the absent host file / no-stylesheet message.
        Assert.Contains(logger.Entries,
            e => e.Message.Contains("no external stylesheet", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_NoSettings_IsNoOp()
    {
        // No settings in context: the step should not throw and should report success.
        var step = new SummarizeSyncfusionBlazorToolkitStep(NullLogger<SummarizeSyncfusionBlazorToolkitStep>.Instance);
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);
    }

    private class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception, Func<TState, System.Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }

        public record LogEntry(LogLevel Level, string Message);
    }
}
