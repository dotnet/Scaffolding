// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.DotNet.Scaffolding.Core.Logging;

/// <summary>
/// A custom console formatter that writes only the log message to the console, omitting log level, category, and other metadata.
/// </summary>
internal class CleanConsoleFormatter : ConsoleFormatter
{
    private const string BrightRedColor = "\x1B[1;31m";
    private const string BrightYellowColor = "\x1B[1;33m";
    private const string ResetColor = "\x1B[0m";

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanConsoleFormatter"/> class.
    /// </summary>
    public CleanConsoleFormatter()
        : base(nameof(CleanConsoleFormatter))
    {
    }

    /// <summary>
    /// Writes the log entry message to the console output, omitting log level, category, and other metadata.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="logEntry">The log entry to write.</param>
    /// <param name="scopeProvider">The external scope provider.</param>
    /// <param name="textWriter">The text writer to write the log message to.</param>
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null)
        {
            return;
        }

        // Write ANSI codes through the logging writer, not the global console.
        string? color = Console.IsOutputRedirected ? null : logEntry.LogLevel switch
        {
            LogLevel.Error or LogLevel.Critical => BrightRedColor,
            LogLevel.Warning => BrightYellowColor,
            _ => null
        };

        if (color is not null)
        {
            textWriter.Write(color);
        }

        textWriter.WriteLine(message);

        if (color is not null)
        {
            textWriter.Write(ResetColor);
        }
    }
}
