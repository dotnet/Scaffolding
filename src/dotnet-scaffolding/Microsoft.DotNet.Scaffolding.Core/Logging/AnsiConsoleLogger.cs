// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Core.Logging;

using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Diagnostics;

public class AnsiConsoleLogger : ILogger
{
    private readonly string _name;
    private readonly LogLevel _minLogLevel;

    public AnsiConsoleLogger(string name, LogLevel minLogLevel)
    {
        _name = name;
        _minLogLevel = minLogLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLogLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        //should be "enabled", "disabled", "", or null. Empty or null will indicate that a tool is not launched by dotnet-scaffold.
        bool isRedirectedToDotnetScaffold = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_SCAFFOLD_TELEMETRY_STATE"));
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var formattedMessage = FormatMessage(logLevel, message);
        //adding another sanity check to check if console output is being redirected to dotnet-scaffold.
        if (isRedirectedToDotnetScaffold && (Console.IsOutputRedirected || Console.IsErrorRedirected))
        {
            // Output plain markup for main app to handle
            AnsiConsole.WriteLine(formattedMessage);
        }
        else
        {
            // Render colored output directly
            AnsiConsole.MarkupLine(formattedMessage);
        }
    }

    private string FormatMessage(LogLevel logLevel, string message)
    {
        return logLevel switch
        {
            LogLevel.Information => $"[green]{message}[/]",
            LogLevel.Warning => $"[yellow]{message}[/]",
            LogLevel.Error => $"[red]{message}[/]",
            LogLevel.Critical => $"[bold red]{message}[/]",
            _ => message
        };
    }
}
