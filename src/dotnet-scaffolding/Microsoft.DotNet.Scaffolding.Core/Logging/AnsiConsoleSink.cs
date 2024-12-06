// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Serilog.Core;
using Serilog.Events;
using Spectre.Console;

namespace Microsoft.DotNet.Scaffolding.Core.Logging;

public class AnsiConsoleSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        //LAUNCHED_BY_DOTNET_SCAFFOLD should be "true", or some other value. "true" indicates being launched by dotnet-scaffold.
        bool isLaunchedByDotnetScaffold = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ScaffolderConstants.LAUNCHED_BY_DOTNET_SCAFFOLD));
        var formattedMessage = FormatMessage(logEvent.Level, logEvent.RenderMessage());
        if (isLaunchedByDotnetScaffold && (Console.IsOutputRedirected || Console.IsErrorRedirected))
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
