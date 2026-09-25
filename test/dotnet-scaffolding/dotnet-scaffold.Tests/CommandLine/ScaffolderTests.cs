// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CommandLine;

public class ScaffolderTests
{
    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, true)]
    public async Task ExecuteAsync_PreservesStepAndCallbackOrdering(bool succeeds, bool skipStep, bool continueOnError, bool expectedSuccess)
    {
        var events = new List<string>();
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
        var scaffolder = new Scaffolder("test", "Test", [], null, [], steps, preparers, NullLogger<Scaffolder>.Instance);

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
        }
        Assert.Equal(expectedEvents, events);
    }

    private sealed class TestStep : ScaffoldStep
    {
        public bool Succeeds { get; init; }
        public Action? OnExecute { get; init; }

        public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
        {
            OnExecute?.Invoke();
            return Task.FromResult(Succeeds);
        }
    }
}
