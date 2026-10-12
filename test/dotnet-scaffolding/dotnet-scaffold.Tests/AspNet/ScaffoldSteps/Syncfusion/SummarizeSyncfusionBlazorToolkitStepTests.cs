// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Extensions.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Scaffold.Tests.AspNet.ScaffoldSteps.Syncfusion;

public class SummarizeSyncfusionBlazorToolkitStepTests
{
    private readonly ScaffolderContext _context;
    private readonly Mock<IScaffolder> _mockScaffolder;

    public SummarizeSyncfusionBlazorToolkitStepTests()
    {
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns("Syncfusion Blazor Toolkit");
        _mockScaffolder.Setup(s => s.Name).Returns("blazor-syncfusion-toolkit");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_LogsHostingGuidance()
    {
        _context.Properties[nameof(SyncfusionBlazorToolkitSettings)] = new SyncfusionBlazorToolkitSettings
        {
            Project = "C:/Test.csproj",
            ImportsFile = Path.Combine("Components", "_Imports.razor"),
            DetectedRenderMode = "InteractiveServer",
        };
        var logger = new TestLogger<SummarizeSyncfusionBlazorToolkitStep>();
        var step = new SummarizeSyncfusionBlazorToolkitStep(logger);

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Contains(logger.Entries, e => e.Message.Contains("InteractiveServer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_ReportsIdempotentReRun_WhenAlreadyApplied()
    {
        _context.Properties[nameof(SyncfusionBlazorToolkitSettings)] = new SyncfusionBlazorToolkitSettings
        {
            Project = "C:/Test.csproj",
            ImportsFile = Path.Combine("Components", "_Imports.razor"),
            ImportsFileSkipped = false,
            ImportsApplied = true,
            PackageAlreadyReferenced = true,
            PackageApplied = true,
            ServicesAlreadyRegistered = true,
            ServicesApplied = true,
        };
        var logger = new TestLogger<SummarizeSyncfusionBlazorToolkitStep>();
        var step = new SummarizeSyncfusionBlazorToolkitStep(logger);

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Contains(logger.Entries, e => e.Message.Contains("already fully configured", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_LogsSkippedStyles_WithInformation()
    {
        _context.Properties[nameof(SyncfusionBlazorToolkitSettings)] = new SyncfusionBlazorToolkitSettings
        {
            Project = "C:/Test.csproj",
            PackageApplied = true,
            ServicesApplied = true,
            ImportsApplied = true,
            ImportsFile = Path.Combine("Components", "_Imports.razor"),
        };
        var logger = new TestLogger<SummarizeSyncfusionBlazorToolkitStep>();
        var step = new SummarizeSyncfusionBlazorToolkitStep(logger);

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Contains(logger.Entries,
            e => e.Message.Contains("no external stylesheet", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_NoSettings_IsNoOp()
    {
        var step = new SummarizeSyncfusionBlazorToolkitStep(NullLogger<SummarizeSyncfusionBlazorToolkitStep>.Instance);
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotClaimProgramCsUpdated_WhenRegistrationSkipped()
    {
        // Review comment 9: "Never report Program.cs as updated when
        // registration was silently skipped."
        _context.Properties[nameof(SyncfusionBlazorToolkitSettings)] = new SyncfusionBlazorToolkitSettings
        {
            Project = "C:/Test.csproj",
            PackageApplied = true,
            // ServicesApplied = false (registration was skipped by the shared step).
            ServicesApplied = false,
            ServicesAlreadyRegistered = false,
            ImportsApplied = true,
            ImportsFile = Path.Combine("Components", "_Imports.razor"),
        };
        var logger = new TestLogger<SummarizeSyncfusionBlazorToolkitStep>();
        var step = new SummarizeSyncfusionBlazorToolkitStep(logger);

        await step.ExecuteAsync(_context, CancellationToken.None);

        // The summary must surface a warning, not claim Program.cs was updated.
        Assert.DoesNotContain(logger.Entries,
            e => e.Level == LogLevel.Information && e.Message.Contains("Updated Program.cs", System.StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Entries,
            e => e.Level == LogLevel.Warning && e.Message.Contains("NOT registered", System.StringComparison.OrdinalIgnoreCase));
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