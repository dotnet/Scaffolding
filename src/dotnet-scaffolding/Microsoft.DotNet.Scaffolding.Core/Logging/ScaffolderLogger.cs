using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Microsoft.DotNet.Scaffolding.Core.Logging;

public class ScaffolderLogger : IScaffolderLogger
{
    public void LogError(string message)
    {
        AnsiConsole.MarkupLine($"[red]{message}[/]");
    }

    public void LogInformation(string message)
    {
        AnsiConsole.MarkupLine(message);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        switch (logLevel)
        {
            case LogLevel.Critical:
            case LogLevel.Error:
                AnsiConsole.MarkupLine($"[red]{message}[/]");
                break;
            case LogLevel.Warning:
                AnsiConsole.MarkupLine($"[yellow]{message}[/]");
                break;
            case LogLevel.Information:
                AnsiConsole.MarkupLine(message);
                break;
            case LogLevel.Debug:
            case LogLevel.Trace:
                AnsiConsole.MarkupLine($"[grey]{message}[/]");
                break;
        }

        if (exception != null)
        {
            AnsiConsole.WriteException(exception);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}
