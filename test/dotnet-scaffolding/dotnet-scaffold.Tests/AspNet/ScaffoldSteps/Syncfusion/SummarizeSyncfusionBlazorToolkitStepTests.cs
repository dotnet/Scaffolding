// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
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

    // A server that is fully configured and unchanged by this run.
    private static SyncfusionBlazorToolkitSettings Configured(Action<SyncfusionBlazorToolkitSettings>? tweak = null)
    {
        var settings = new SyncfusionBlazorToolkitSettings
        {
            Project = "C:/Test.csproj",
            AppKind = BlazorAppKind.WebApp,
            ImportsFile = Path.Combine("Components", "_Imports.razor"),
            ImportsApplied = true,
            PackageApplied = true,
            ServicesApplied = true,
            ServicesAlreadyRegistered = true,
            ProgramCsFile = "Program.cs",
        };
        settings.Server.PackageVersionBefore = "2.0.0";
        settings.Server.PackageVersionAfter = "2.0.0";
        tweak?.Invoke(settings);
        return settings;
    }

    private async Task<TestLogger<SummarizeSyncfusionBlazorToolkitStep>> Run(SyncfusionBlazorToolkitSettings settings)
    {
        _context.Properties[nameof(SyncfusionBlazorToolkitSettings)] = settings;
        var logger = new TestLogger<SummarizeSyncfusionBlazorToolkitStep>();
        await new SummarizeSyncfusionBlazorToolkitStep(logger).ExecuteAsync(_context, CancellationToken.None);
        return logger;
    }

    // ---------------------------------------------------------------- no-op vs change

    [Fact]
    public async Task NothingChanged_IsReportedAsNoOp()
    {
        var logger = await Run(Configured());

        Assert.Contains(logger.Entries, e => e.Message.Contains("made no changes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PackageUpgrade_IsNotReportedAsNoOp()
    {
        var logger = await Run(Configured(s => s.Server.PackageVersionBefore = "1.0.2"));

        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("made no changes", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Entries, e => e.Message.Contains("updated Syncfusion.Blazor.Toolkit from 1.0.2 to 2.0.0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task NewPackageReference_IsReportedAsAdded()
    {
        var logger = await Run(Configured(s => s.Server.PackageVersionBefore = null));

        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("made no changes", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Entries, e => e.Message.Contains("added Syncfusion.Blazor.Toolkit 2.0.0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ChangedClient_IsNotReportedAsNoOp_EvenWhenServerWasAlreadyConfigured()
    {
        var logger = await Run(Configured(s =>
        {
            s.ClientProjectPath = "C:/Test.Client/Test.Client.csproj";
            s.Layout = SyncfusionBlazorToolkitLayout.AutoServer;
            s.Client.PackageVersionBefore = null;
            s.Client.PackageVersionAfter = "2.0.0";
            s.Client.ImportsInserted = true;
        }));

        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("made no changes", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Entries, e => e.Message.Contains("Test.Client.csproj", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Entries, e => e.Message.Contains("added Syncfusion.Blazor.Toolkit 2.0.0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UnchangedClient_AndServer_IsStillANoOp()
    {
        var logger = await Run(Configured(s =>
        {
            s.ClientProjectPath = "C:/Test.Client/Test.Client.csproj";
            s.Client.PackageVersionBefore = "2.0.0";
            s.Client.PackageVersionAfter = "2.0.0";
        }));

        Assert.Contains(logger.Entries, e => e.Message.Contains("made no changes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task InsertedRegistration_UsesTheRecordedOutcome()
    {
        var logger = await Run(Configured(s =>
        {
            s.ServicesAlreadyRegistered = false;
            s.Server.ServicesInserted = true;
        }));

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Updated Program.cs", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("made no changes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task InsertedImports_IsNotANoOp()
    {
        var logger = await Run(Configured(s => s.Server.ImportsInserted = true));

        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("made no changes", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Entries, e => e.Message.Contains("added @using Syncfusion.Blazor.Toolkit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DoesNotClaimRegistrationWasAdded_WhenItWasSkipped()
    {
        var logger = await Run(Configured(s =>
        {
            s.ServicesApplied = false;
            s.ServicesAlreadyRegistered = false;
        }));

        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Updated Program.cs", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("NOT registered", StringComparison.OrdinalIgnoreCase));
    }

    // ---------------------------------------------------------------- hosting guidance

    [Fact]
    public async Task PerPageInteractivity_IsNotDescribedAsStaticSsr()
    {
        // Server hosting registered, no global render mode: pages opt in individually.
        var logger = await Run(Configured(s => s.Hosting = new BlazorHostingFacts(
            BlazorHostingModes.Server, BlazorHostingModes.Server, "None")));

        string hosting = HostingLine(logger);
        Assert.Contains("Interactive Server hosting is registered", hosting);
        Assert.Contains("pages choose their own", hosting);
        Assert.DoesNotContain("static server rendering", hosting, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StandaloneWasm_IsNotGivenServerSideSsrGuidance()
    {
        var logger = await Run(Configured(s => s.AppKind = BlazorAppKind.StandaloneWasm));

        string hosting = HostingLine(logger);
        Assert.Contains("Standalone Blazor WebAssembly", hosting);
        Assert.DoesNotContain("AddInteractiveServer", hosting);
        Assert.DoesNotContain("static server rendering", hosting, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClientProjectReference_IsNotProofOfAuto()
    {
        // A WebAssembly client is referenced but only WebAssembly hosting is registered:
        // InteractiveAuto must not be promised.
        var logger = await Run(Configured(s =>
        {
            s.ClientProjectPath = "C:/Test.Client/Test.Client.csproj";
            s.Hosting = new BlazorHostingFacts(BlazorHostingModes.WebAssembly, BlazorHostingModes.WebAssembly, "None");
        }));

        string hosting = HostingLine(logger);
        Assert.Contains("InteractiveWebAssembly is available", hosting);
        Assert.Contains("InteractiveAuto also needs", hosting);
        Assert.DoesNotContain("InteractiveServer, InteractiveWebAssembly and InteractiveAuto are available", hosting);
    }

    [Fact]
    public async Task BothHostingModes_AllowAuto()
    {
        var both = BlazorHostingModes.Server | BlazorHostingModes.WebAssembly;
        var logger = await Run(Configured(s => s.Hosting = new BlazorHostingFacts(both, both, "InteractiveAuto")));

        string hosting = HostingLine(logger);
        Assert.Contains("InteractiveAuto are available", hosting);
        Assert.Contains("The app-wide render mode is InteractiveAuto", hosting);
    }

    [Fact]
    public async Task ServicesWithoutEndpoint_DoNotCountAsSupported()
    {
        // Component services alone (no AddInteractive*RenderMode on the endpoint) are not usable hosting.
        var logger = await Run(Configured(s => s.Hosting = new BlazorHostingFacts(
            BlazorHostingModes.Server, BlazorHostingModes.None, "None")));

        Assert.Contains("No interactive hosting is registered", HostingLine(logger));
    }

    [Fact]
    public async Task NoInteractiveHosting_SuggestsRegistrationsWithoutChangingAnything()
    {
        var logger = await Run(Configured());

        string hosting = HostingLine(logger);
        Assert.Contains("No interactive hosting is registered", hosting);
        Assert.Contains("AddInteractiveServerComponents()", hosting);
    }

    // ---------------------------------------------------------------- misc

    [Fact]
    public async Task LogsStylesNote()
    {
        var logger = await Run(Configured());

        Assert.Contains(logger.Entries, e => e.Message.Contains("No host stylesheet link was added", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task NoSettings_IsNoOp()
    {
        var step = new SummarizeSyncfusionBlazorToolkitStep(NullLogger<SummarizeSyncfusionBlazorToolkitStep>.Instance);
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);
    }

    private static string HostingLine(TestLogger<SummarizeSyncfusionBlazorToolkitStep> logger) =>
        logger.Entries.Single(e => e.Message.StartsWith("  - Hosting:", StringComparison.Ordinal)).Message;

    private class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }

        public record LogEntry(LogLevel Level, string Message);
    }
}
