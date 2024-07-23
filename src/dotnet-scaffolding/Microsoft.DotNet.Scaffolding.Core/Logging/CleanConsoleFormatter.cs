// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.DotNet.Scaffolding.Core.Logging;

internal class CleanConsoleFormatter : ConsoleFormatter
{
    public CleanConsoleFormatter()
        :base(nameof(CleanConsoleFormatter))
    {

    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

        if (message is not null)
        {
            textWriter.WriteLine(message);
        }
    }
}
