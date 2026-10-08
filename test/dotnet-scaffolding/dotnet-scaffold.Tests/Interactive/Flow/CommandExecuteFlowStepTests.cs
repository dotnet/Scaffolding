// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Flow;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32.SafeHandles;
using Moq;
using Spectre.Console.Flow;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Interactive.Flow;

public class CommandExecuteFlowStepTests
{
    [Theory]
    [InlineData(ScaffolderCatagory.AspNet, true)]
    [InlineData(ScaffolderCatagory.AspNet, false)]
    [InlineData(ScaffolderCatagory.Aspire, true)]
    [InlineData(ScaffolderCatagory.Aspire, false)]
    public async Task Execution_PropagatesCommandResultWithoutRetrying(ScaffolderCatagory category, bool succeeds)
    {
        var builder = Host.CreateScaffoldBuilder();
        var step = new TestStep { Succeeds = succeeds };
        builder.Services.AddSingleton(step);
        builder.AddScaffolder(category, "test").WithStep<TestStep>();
        var runner = builder.Build();
        using var services = Assert.IsType<ServiceProvider>(builder.ServiceProvider);
        var telemetry = new Mock<ITelemetryService>();
        var executeStep = new CommandExecuteFlowStep(telemetry.Object, runner);
        FlowStepResult? fallbackResult = null;
        var observedStep = new Mock<IFlowStep>();
        observedStep.SetupGet(s => s.Id).Returns(executeStep.Id);
        observedStep.SetupGet(s => s.DisplayName).Returns(executeStep.DisplayName);
        observedStep.Setup(s => s.ValidateUserInputAsync(It.IsAny<IFlowContext>(), It.IsAny<CancellationToken>()))
            .Returns((IFlowContext context, CancellationToken token) => executeStep.ValidateUserInputAsync(context, token));
        observedStep.Setup(s => s.RunAsync(It.IsAny<IFlowContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (IFlowContext context, CancellationToken token) =>
            {
                fallbackResult = await executeStep.RunAsync(context, token);
                return fallbackResult;
            });
        var properties = new Dictionary<string, object>
        {
            [FlowContextProperties.ComponentObj] = new DotNetToolInfo
            {
                PackageName = "Microsoft.dotnet-scaffold",
                Command = "dotnet-scaffold",
                Version = "1.0.0"
            },
            [FlowContextProperties.CommandObj] = new CommandInfo
            {
                Name = "test",
                DisplayName = "Test",
                DisplayCategories = [category == ScaffolderCatagory.AspNet ? "Entra ID" : "Aspire"],
                Parameters = []
            }
        };
        var flow = new FlowRunner([observedStep.Object], properties, nonInteractive: false)
        {
            ShowSelectedOptions = false
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        builder.AddHandler(async (_, _) => await flow.RunAsync(timeout.Token));

        int exitCode = await RunWithConsoleAsync(() => runner.RunAsync([]));

        Assert.Equal(succeeds ? 0 : 1, exitCode);
        if (succeeds)
        {
            Assert.Null(fallbackResult);
        }
        else
        {
            Assert.Equal(FlowStepState.Failure, Assert.IsType<FlowStepResult>(fallbackResult).State);
            Assert.Contains("exit code: 1", fallbackResult!.Message);
        }
        Assert.Equal(1, step.ExecutionCount);
        telemetry.Verify(t => t.TrackEvent(
            It.IsAny<string>(),
            It.Is<IReadOnlyDictionary<string, string>>(p => p["Result"] == (succeeds ? "Success" : "Failure")),
            It.IsAny<IReadOnlyDictionary<string, double>>()), Times.Once);
    }

    [Fact]
    public async Task ValidateUserInputAsync_PropagatesExternalToolFailure()
    {
        var builder = Host.CreateScaffoldBuilder();
        var runner = builder.Build();
        using var services = Assert.IsType<ServiceProvider>(builder.ServiceProvider);
        var telemetry = new Mock<ITelemetryService>();
        var executeStep = new CommandExecuteFlowStep(telemetry.Object, runner);
        var properties = new Dictionary<string, object>
        {
            [FlowContextProperties.ComponentObj] = new DotNetToolInfo
            {
                PackageName = "test-tool",
                Command = ScaffoldCliHelper.GetDotNetPath(),
                Version = "1.0.0",
                IsGlobalTool = true
            },
            [FlowContextProperties.CommandObj] = new CommandInfo
            {
                Name = "--unknown-scaffold-test-command",
                DisplayName = "Test",
                DisplayCategories = ["Test"],
                Parameters = []
            }
        };
        var flow = new FlowRunner([executeStep], properties, nonInteractive: true);

        var result = await executeStep.ValidateUserInputAsync(flow.Context, CancellationToken.None);

        Assert.Equal(FlowStepState.Failure, result.State);
        Assert.Contains("Command exit code:", result.Message);
        Assert.Same(result, await executeStep.RunAsync(flow.Context, CancellationToken.None));
        telemetry.Verify(t => t.TrackEvent(
            It.IsAny<string>(),
            It.Is<IReadOnlyDictionary<string, string>>(p => p["Result"] == "Failure"),
            It.IsAny<IReadOnlyDictionary<string, double>>()), Times.Once);
    }

    private static async Task<int> RunWithConsoleAsync(Func<Task<int>> run)
    {
        if (!OperatingSystem.IsWindows())
        {
            return await run();
        }

        // FlowRunner calls Console.Clear even with redirected output. Use a private buffer, not the parent console's screen.
        IntPtr input = GetStdHandle(-10);
        IntPtr output = GetStdHandle(-11);
        IntPtr error = GetStdHandle(-12);
        bool allocated = false;
        SafeFileHandle consoleOutput = CreateConsoleScreenBuffer(0xC0000000, 3, IntPtr.Zero, 1, IntPtr.Zero);
        try
        {
            if (consoleOutput.IsInvalid)
            {
                consoleOutput.Dispose();
                if (!AllocConsole())
                {
                    throw new Win32Exception();
                }
                allocated = true;
                ShowWindow(GetConsoleWindow(), 0);
                consoleOutput = CreateConsoleScreenBuffer(0xC0000000, 3, IntPtr.Zero, 1, IntPtr.Zero);
            }
            if (consoleOutput.IsInvalid || !SetStdHandle(-11, consoleOutput.DangerousGetHandle()))
            {
                throw new Win32Exception();
            }

            return await run();
        }
        finally
        {
            consoleOutput.Dispose();
            bool detached = !allocated || FreeConsole();
            bool restored = SetStdHandle(-10, input) & SetStdHandle(-11, output) & SetStdHandle(-12, error);
            if (!detached || !restored)
            {
                throw new Win32Exception("Unable to restore console handles after the interactive flow test.");
            }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetStdHandle(int handle, IntPtr value);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeFileHandle CreateConsoleScreenBuffer(uint access, uint share, IntPtr security, uint flags, IntPtr data);

    private sealed class TestStep : ScaffoldStep
    {
        public bool Succeeds { get; init; }
        public int ExecutionCount { get; private set; }

        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            if (++ExecutionCount > 1)
            {
                throw new InvalidOperationException("The scaffolder must not be retried automatically after a failure.");
            }
            return Task.FromResult(Succeeds);
        }
    }
}
