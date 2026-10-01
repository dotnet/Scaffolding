// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Command;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Services;
using Moq;
using Spectre.Console.Cli;
using Spectre.Console.Flow;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Interactive.Flow;

public class BaseCommandTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public async Task RunFlowAsync_PreservesFlowExitCode(int expectedExitCode)
    {
        var flow = new Mock<IFlow>();
        flow.Setup(f => f.RunAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(expectedExitCode));
        var provider = new Mock<IFlowProvider>();
        provider.Setup(p => p.GetFlow(
                It.IsAny<IEnumerable<IFlowStep>>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .Returns(flow.Object);

        Assert.Equal(expectedExitCode, await new TestCommand(provider.Object).RunTestFlowAsync());
    }

    [Fact]
    public async Task RunFlowAsync_ReportsExceptionAndReturnsPortableFailure()
    {
        var flow = new Mock<IFlow>();
        flow.Setup(f => f.RunAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(Task.FromException<int>(new InvalidOperationException("test failure"))));
        var provider = new Mock<IFlowProvider>();
        provider.Setup(p => p.GetFlow(
                It.IsAny<IEnumerable<IFlowStep>>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
            .Returns(flow.Object);
        using var error = new StringWriter();
        var originalError = Console.Error;
        try
        {
            Console.SetError(error);
            int exitCode = await new TestCommand(provider.Object).RunTestFlowAsync();

            Assert.Equal(1, exitCode);
            Assert.Contains("test failure", error.ToString());
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    private sealed class TestCommand(IFlowProvider provider)
        : BaseCommand<TestCommand.Settings>(provider, Mock.Of<ITelemetryService>())
    {
        public sealed class Settings : CommandSettings
        {
        }

        public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
            => Task.FromResult(0);

        public async Task<int> RunTestFlowAsync()
            => await RunFlowAsync([], new Settings(), Mock.Of<IRemainingArguments>());
    }
}
