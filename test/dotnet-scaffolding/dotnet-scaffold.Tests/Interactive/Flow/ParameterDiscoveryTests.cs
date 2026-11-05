// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps;
using Moq;
using Spectre.Console;
using Spectre.Console.Flow;
using Spectre.Console.Rendering;
using Xunit;
using static Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps.ParameterDiscovery;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Interactive.Flow;

public class ParameterDiscoveryTests
{
    [Theory]
    [InlineData(80, 25, 0)]
    [InlineData(80, 25, 60)]
    [InlineData(80, 25, 220)]
    [InlineData(120, 30, 0)]
    [InlineData(40, 25, 0)]
    [InlineData(80, 15, 0)]
    public void ApplicationPicker_StartsAtFirstOption_AndHighlightsArrowNavigation(int width, int height, int namePadding)
    {
        var options = CreateApplicationOptions(namePadding);
        var (result, frames) = ShowPicker(options, width, height,
            [ConsoleKey.DownArrow, ConsoleKey.UpArrow, ConsoleKey.Enter]);

        Assert.Same(options[0], result);
        Assert.Equal(3, frames.Count);
        Assert.Matches("\u001b\\[94m(?:> )?Application-00", frames[0]);
        Assert.Matches("\u001b\\[94m(?:> )?Application-01", frames[1]);
        Assert.Matches("\u001b\\[94m(?:> )?Application-00", frames[2]);
        foreach (var frame in frames)
        {
            var applications = Regex.Matches(frame, @"Application-\d\d").Select(match => match.Value).ToList();
            Assert.NotEmpty(applications);
            Assert.Equal(applications.Count, applications.Distinct().Count());
        }
    }

    [Fact]
    public void ApplicationPicker_PaginatesWithoutChangingSelectedValue()
    {
        var options = CreateApplicationOptions();
        var keys = Enumerable.Repeat(ConsoleKey.DownArrow, 25)
            .Concat([ConsoleKey.UpArrow, ConsoleKey.Enter])
            .ToArray();

        var (result, frames) = ShowPicker(options, 80, 25, keys);

        Assert.Same(options[24], result);
        Assert.Contains("\u001b[94m> Application-25", frames[25]);
        Assert.Contains("\u001b[94m> Application-24", frames[26]);
        Assert.DoesNotContain("Application-00", frames[25]);
        Assert.Contains(options[24].Value, frames[26]);
    }

    [Theory]
    [InlineData("Application [test]", "Application [test]", "Application [test]")]
    [InlineData("Model", "Namespace.Model", "Model (Namespace.Model)")]
    [InlineData("None", "", "None (empty to skip parameter)")]
    public void Picker_FormatsLabelsWithoutRepeatingIdenticalValues(string name, string value, string expected)
    {
        var option = new StepOption { Name = name, Value = value };
        var prompt = CreateDiscovery().CreateSelectionPrompt([option], new FlowNavigation());

        var label = prompt.Raw.Converter!(new FlowStepOption<StepOption>(option));

        Assert.Equal(expected, label.RemoveMarkup());
    }

    private static List<StepOption> CreateApplicationOptions(int namePadding = 0) =>
        Enumerable.Range(0, 40)
            .Select(i =>
            {
                var label = $"Application-{i:00}{new string('x', namePadding)} 00000000-0000-0000-0000-{i:000000000000}";
                return new StepOption { Name = label, Value = label };
            })
            .ToList();

    private static ParameterDiscovery CreateDiscovery() =>
        new(
            new Parameter
            {
                DisplayName = "Select Existing Application",
                Name = "application",
                Required = true,
                Type = CliTypes.String
            },
            Mock.Of<IFileSystem>(),
            Mock.Of<IEnvironmentService>());

    private static (StepOption? Result, List<string> Frames) ShowPicker(
        List<StepOption> options, int width, int height, ConsoleKey[] inputKeys)
    {
        using var output = new StringWriter();
        var realConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.Standard,
            Interactive = InteractionSupport.Yes,
            Out = new AnsiConsoleOutput(output)
        });
        realConsole.Profile.Width = width;
        realConsole.Profile.Height = height;
        realConsole.Profile.Capabilities.Unicode = false;
        var frames = new List<string>();
        var keys = new Queue<ConsoleKey>(inputKeys);
        var input = new Mock<IAnsiConsoleInput>();
        input.Setup(x => x.ReadKeyAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                frames.Add(output.ToString());
                output.GetStringBuilder().Clear();
                return Task.FromResult<ConsoleKeyInfo?>(new ConsoleKeyInfo('\0', keys.Dequeue(), false, false, false));
            });
        var console = new Mock<IAnsiConsole>();
        console.SetupGet(x => x.Profile).Returns(realConsole.Profile);
        console.SetupGet(x => x.Cursor).Returns(realConsole.Cursor);
        console.SetupGet(x => x.Input).Returns(input.Object);
        console.SetupGet(x => x.ExclusivityMode).Returns(realConsole.ExclusivityMode);
        console.SetupGet(x => x.Pipeline).Returns(realConsole.Pipeline);
        console.Setup(x => x.Write(It.IsAny<IRenderable>()))
            .Callback<IRenderable>(renderable =>
            {
                output.GetStringBuilder().Clear();
                realConsole.Write(renderable);
            });

        var prompt = CreateDiscovery().CreateSelectionPrompt(
            options, new FlowNavigation { Back = true, Exit = true }, console: console.Object);
        var result = prompt.Raw.Show(console.Object);

        return (result.Value, frames);
    }
}
