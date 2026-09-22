// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
using Moq;
using Spectre.Console.Flow;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Interactive.Flow;

public class CommandExecuteFlowStepTests
{
    [Theory]
    [InlineData(ScaffolderCatagory.AspNet, true, true)]
    [InlineData(ScaffolderCatagory.AspNet, false, true)]
    [InlineData(ScaffolderCatagory.Aspire, true, true)]
    [InlineData(ScaffolderCatagory.Aspire, false, true)]
    [InlineData(ScaffolderCatagory.AspNet, true, false)]
    [InlineData(ScaffolderCatagory.AspNet, false, false)]
    [InlineData(ScaffolderCatagory.Aspire, true, false)]
    [InlineData(ScaffolderCatagory.Aspire, false, false)]
    public async Task Execution_PropagatesCommandResultWithoutRetrying(ScaffolderCatagory category, bool succeeds, bool runThroughFlow)
    {
        var builder = Host.CreateScaffoldBuilder();
        var step = new TestStep { Succeeds = succeeds };
        builder.Services.AddSingleton(step);
        builder.AddScaffolder(category, "test").WithStep<TestStep>();
        var runner = builder.Build();
        using var services = Assert.IsType<ServiceProvider>(builder.ServiceProvider);
        var telemetry = new Mock<ITelemetryService>();
        var executeStep = new CommandExecuteFlowStep(telemetry.Object, runner);
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
        var flow = new FlowRunner([executeStep], properties, nonInteractive: true)
        {
            ShowSelectedOptions = false
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        if (runThroughFlow)
        {
            builder.AddHandler(async (_, _) => await flow.RunAsync(timeout.Token));
            int exitCode = await runner.RunAsync([]);

            Assert.Equal(succeeds, exitCode == 0);
        }
        else
        {
            var result = await executeStep.ValidateUserInputAsync(flow.Context, timeout.Token);
            Assert.Equal(succeeds ? FlowStepState.Success : FlowStepState.Failure, result.State);
            if (!succeeds)
            {
                Assert.Contains("exit code: 1", result.Message);
            }
        }
        Assert.Equal(1, step.ExecutionCount);
        telemetry.Verify(t => t.TrackEvent(
            It.IsAny<string>(),
            It.Is<IReadOnlyDictionary<string, string>>(p => p["Result"] == (succeeds ? "Success" : "Failure")),
            It.IsAny<IReadOnlyDictionary<string, double>>()), Times.Once);
    }

    [Theory]
    [InlineData("--version", true)]
    [InlineData("--unknown-scaffold-test-command", false)]
    public async Task ValidateUserInputAsync_PropagatesExternalToolResult(string command, bool succeeds)
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
                Name = command,
                DisplayName = "Test",
                DisplayCategories = ["Test"],
                Parameters = []
            }
        };
        var flow = new FlowRunner([executeStep], properties, nonInteractive: true);

        var result = await executeStep.ValidateUserInputAsync(flow.Context, CancellationToken.None);

        Assert.Equal(succeeds ? FlowStepState.Success : FlowStepState.Failure, result.State);
        if (!succeeds)
        {
            Assert.Contains("Command exit code:", result.Message);
        }
        telemetry.Verify(t => t.TrackEvent(
            It.IsAny<string>(),
            It.Is<IReadOnlyDictionary<string, string>>(p => p["Result"] == (succeeds ? "Success" : "Failure")),
            It.IsAny<IReadOnlyDictionary<string, double>>()), Times.Once);
    }

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
