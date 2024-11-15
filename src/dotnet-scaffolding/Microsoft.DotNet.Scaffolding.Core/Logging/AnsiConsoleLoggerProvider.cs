// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Microsoft.DotNet.Scaffolding.Core.Logging;

public class AnsiConsoleLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minLogLevel;
    private readonly ConcurrentDictionary<string, AnsiConsoleLogger> _loggers = new();

    public AnsiConsoleLoggerProvider(LogLevel minLogLevel)
    {
        _minLogLevel = minLogLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new AnsiConsoleLogger(name, _minLogLevel));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

