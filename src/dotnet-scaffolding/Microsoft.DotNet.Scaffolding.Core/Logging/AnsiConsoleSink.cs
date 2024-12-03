// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;
using Spectre.Console;

namespace Microsoft.DotNet.Scaffolding.Core.Logging;

public class AnsiConsoleSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        Debugger.Launch();
        //should be "enabled", "disabled", "", or null. Empty or null will indicate that a tool is not launched by dotnet-scaffold.
        bool isRedirectedToDotnetScaffold = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_SCAFFOLD_TELEMETRY_STATE"));
        var message = logEvent.RenderMessage();
        var formattedMessage = FormatMessage(logEvent.Level, message);
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

    private string FormatMessage(LogEventLevel level, string message)
    {
        return level switch
        {
            LogEventLevel.Verbose => $"[gray]{message}[/]",
            LogEventLevel.Debug => $"[gray]{message}[/]",
            LogEventLevel.Information => $"[green]{message}[/]",
            LogEventLevel.Warning => $"[yellow]{message}[/]",
            LogEventLevel.Error => $"[red]{message}[/]",
            LogEventLevel.Fatal => $"[bold red]{message}[/]",
            _ => message
        };
    }
}
