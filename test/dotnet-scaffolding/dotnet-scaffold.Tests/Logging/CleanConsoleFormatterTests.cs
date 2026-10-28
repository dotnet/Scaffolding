// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Logging;

public class CleanConsoleFormatterTests
{
    [Theory]
    [InlineData(LogLevel.Information, false, null)]
    [InlineData(LogLevel.Warning, false, "\x1B[1m\x1B[33m")]
    [InlineData(LogLevel.Error, false, "\x1B[1m\x1B[31m")]
    [InlineData(LogLevel.Critical, false, "\x1B[1m\x1B[31m")]
    [InlineData(LogLevel.Information, true, null)]
    [InlineData(LogLevel.Warning, true, null)]
    [InlineData(LogLevel.Error, true, null)]
    [InlineData(LogLevel.Critical, true, null)]
    public void Write_UsesLogLevelColorUnlessOutputIsRedirected(LogLevel level, bool isOutputRedirected, string? color)
    {
        var formatter = new CleanConsoleFormatter();
        using var writer = new StringWriter();
        var entry = CreateEntry(level, "Invalid --model 'Program'");

        formatter.Write(entry, writer, isOutputRedirected);

        string reset = color is null ? string.Empty : "\x1B[0m";
        Assert.Equal($"{color}Invalid --model 'Program'{Environment.NewLine}{reset}", writer.ToString());
    }

    [Fact]
    public void Write_UsesConsoleRedirectionState()
    {
        var formatter = new CleanConsoleFormatter();
        using var actual = new StringWriter();
        using var expected = new StringWriter();
        var entry = CreateEntry(LogLevel.Error, "Invalid model");

        formatter.Write(entry, scopeProvider: null, actual);
        formatter.Write(entry, expected, Console.IsOutputRedirected);

        Assert.Equal(expected.ToString(), actual.ToString());
    }

    [Theory]
    [InlineData(LogLevel.Warning, ConsoleColor.Yellow)]
    [InlineData(LogLevel.Error, ConsoleColor.Red)]
    [InlineData(LogLevel.Critical, ConsoleColor.Red)]
    public void Write_ProducesColorsRecognizedByWindowsFallbackParser(LogLevel level, ConsoleColor expectedColor)
    {
        var formatter = new CleanConsoleFormatter();
        using var writer = new StringWriter();
        var entry = CreateEntry(level, "Invalid model");
        formatter.Write(entry, writer, isOutputRedirected: false);

        using var parsed = new StringWriter();
        Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite = (message, start, length, background, foreground) =>
        {
            Assert.Equal(expectedColor, foreground);
            parsed.Write(message.AsSpan(start, length));
        };

        // Exercise the real Windows fallback parser without changing process-global console state.
        var parserType = typeof(ConsoleFormatter).Assembly.GetType("Microsoft.Extensions.Logging.Console.AnsiParser", throwOnError: true)!;
        var parser = Activator.CreateInstance(parserType, onParseWrite)!;
        var parse = parserType.GetMethod("Parse", new[] { typeof(string) })!;
        parse.Invoke(parser, new object[] { writer.ToString() });

        Assert.Equal($"Invalid model{Environment.NewLine}", parsed.ToString());
    }

    private static LogEntry<string> CreateEntry(LogLevel level, string message)
        => new(level, "category", new EventId(1), message, null, (state, _) => state);
}
