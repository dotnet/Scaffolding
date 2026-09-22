// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CommandLine;

public class ScaffoldRunnerTests
{
    [Theory]
    [InlineData(ScaffolderCatagory.AspNet, true, 0)]
    [InlineData(ScaffolderCatagory.AspNet, false, 1)]
    [InlineData(ScaffolderCatagory.Aspire, true, 0)]
    [InlineData(ScaffolderCatagory.Aspire, false, 1)]
    public async Task RunAsync_ReturnsStepResult(ScaffolderCatagory category, bool succeeds, int expectedExitCode)
    {
        var builder = Host.CreateScaffoldBuilder();
        var step = new TestStep { Succeeds = succeeds };
        builder.Services.AddSingleton(step);
        builder.AddScaffolder(category, "test")
            .WithStep<TestStep>();
        var runner = Assert.IsType<ScaffoldRunner>(builder.Build());
        using var services = Assert.IsType<ServiceProvider>(builder.ServiceProvider);

        int exitCode = await runner.RunAsync([category.ToString().ToLowerInvariant(), "test"]);

        Assert.Equal(expectedExitCode, exitCode);
        Assert.Equal(1, step.ExecutionCount);
    }

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, true)]
    [InlineData(false, true, true, true)]
    public async Task ExecuteAsync_PreservesStepAndCallbackOrdering(bool succeeds, bool skipStep, bool continueOnError, bool expectedSuccess)
    {
        var events = new List<string>();
        var logger = new TestLogger();
        List<ScaffoldStep> steps =
        [
            new TestStep { Succeeds = true, OnExecute = () => events.Add("execute1") },
            new TestStep
            {
                Succeeds = succeeds,
                SkipStep = skipStep,
                ContinueOnError = continueOnError,
                OnExecute = () => events.Add("execute2")
            },
            new TestStep { Succeeds = true, OnExecute = () => events.Add("execute3") }
        ];
        List<ScaffoldStepPreparer> preparers = [];
        for (int i = 1; i <= steps.Count; i++)
        {
            int stepNumber = i;
            preparers.Add(new ScaffoldStepPreparer<TestStep>
            {
                PreExecute = _ => events.Add($"pre{stepNumber}"),
                PostExecute = _ => events.Add($"post{stepNumber}")
            });
        }
        var scaffolder = new Scaffolder("test", "Test", [], null, [], steps, preparers, logger);

        bool result = await scaffolder.ExecuteAsync(new ScaffolderContext(scaffolder));

        Assert.Equal(expectedSuccess, result);
        List<string> expectedEvents = ["pre1", "execute1", "post1", "pre2"];
        if (!skipStep)
        {
            expectedEvents.Add("execute2");
        }
        if (expectedSuccess)
        {
            expectedEvents.AddRange(["post2", "pre3", "execute3", "post3"]);
            Assert.Empty(logger.Messages);
        }
        else
        {
            var message = Assert.Single(logger.Messages);
            Assert.Equal(LogLevel.Error, message.Level);
            Assert.Contains("'test'", message.Text);
            Assert.Contains(nameof(TestStep), message.Text);
            Assert.Contains("project or external resources may have been partially modified", message.Text);
        }
        Assert.Equal(expectedEvents, events);
    }

    [Fact]
    public async Task ExecuteAsync_NoSteps_ReturnsSuccess()
    {
        var logger = new TestLogger();
        var scaffolder = new Scaffolder("test", "Test", [], null, [], [], [], logger);

        Assert.True(await scaffolder.ExecuteAsync(new ScaffolderContext(scaffolder)));
        Assert.Empty(logger.Messages);
    }

    [Theory]
    [InlineData("--help", 0)]
    [InlineData("--unknown-option", 1)]
    public async Task RunAsync_HelpAndParseErrors_DoNotExecuteSteps(string argument, int expectedExitCode)
    {
        var builder = Host.CreateScaffoldBuilder();
        var step = new TestStep { Succeeds = true };
        builder.Services.AddSingleton(step);
        builder.AddScaffolder(ScaffolderCatagory.AspNet, "test").WithStep<TestStep>();
        var runner = builder.Build();
        using var services = Assert.IsType<ServiceProvider>(builder.ServiceProvider);

        int exitCode = await runner.RunAsync(["aspnet", "test", argument]);

        Assert.Equal(expectedExitCode, exitCode);
        Assert.Equal(0, step.ExecutionCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public async Task RunAsync_PropagatesRootHandlerExitCode(int expectedExitCode)
    {
        var builder = Host.CreateScaffoldBuilder();
        var runner = builder.Build();
        using var services = Assert.IsType<ServiceProvider>(builder.ServiceProvider);
        builder.AddHandler((_, _) => Task.FromResult(expectedExitCode));

        Assert.Equal(expectedExitCode, await runner.RunAsync([]));
    }

    [Fact]
    public async Task RunAsync_TaskRootHandler_ReturnsSuccess()
    {
        var builder = Host.CreateScaffoldBuilder();
        var runner = builder.Build();
        using var services = Assert.IsType<ServiceProvider>(builder.ServiceProvider);
        bool called = false;
        builder.AddHandler((_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        Assert.Equal(0, await runner.RunAsync([]));
        Assert.True(called);
    }

    [Fact]
    public void AddExitCodeHandler_BeforeBuild_Throws()
    {
        var builder = Host.CreateScaffoldBuilder();

        Assert.Throws<InvalidOperationException>(() => builder.AddHandler((_, _) => Task.FromResult(1)));
    }

    [Fact]
    public async Task Build_WithoutLoggingServices_PreservesExecutionResult()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(new TestStep { Succeeds = false });
        using var services = serviceCollection.BuildServiceProvider();
        var scaffolder = new ScaffoldBuilder("test").WithStep<TestStep>().Build(services);

        Assert.False(await scaffolder.ExecuteAsync(new ScaffolderContext(scaffolder)));
    }

    private sealed class TestStep : ScaffoldStep
    {
        public bool Succeeds { get; init; }
        public Action? OnExecute { get; init; }
        public int ExecutionCount { get; private set; }

        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            OnExecute?.Invoke();
            return Task.FromResult(Succeeds);
        }
    }

    private sealed class TestLogger : ILogger<Scaffolder>
    {
        public List<(LogLevel Level, string Text)> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add((logLevel, formatter(state, exception)));
        }
    }
}
