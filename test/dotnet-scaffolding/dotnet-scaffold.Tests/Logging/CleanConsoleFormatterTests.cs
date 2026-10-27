// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Logging;

public class CleanConsoleFormatterTests
{
    [Theory]
    [InlineData(LogLevel.Information, null)]
    [InlineData(LogLevel.Warning, "\x1B[1;33m")]
    [InlineData(LogLevel.Error, "\x1B[1;31m")]
    [InlineData(LogLevel.Critical, "\x1B[1;31m")]
    public void Write_UsesLogLevelColorUnlessOutputIsRedirected(LogLevel level, string? color)
    {
        var formatter = new CleanConsoleFormatter();
        using var writer = new StringWriter();
        var entry = CreateEntry(level, "Invalid --model 'Program'");

        formatter.Write(entry, scopeProvider: null, writer);

        color = Console.IsOutputRedirected ? null : color;
        string reset = color is null ? string.Empty : "\x1B[0m";
        Assert.Equal($"{color}Invalid --model 'Program'{Environment.NewLine}{reset}", writer.ToString());
    }

    private static LogEntry<string> CreateEntry(LogLevel level, string message)
        => new(level, "category", new EventId(1), message, null, (state, _) => state);
}
