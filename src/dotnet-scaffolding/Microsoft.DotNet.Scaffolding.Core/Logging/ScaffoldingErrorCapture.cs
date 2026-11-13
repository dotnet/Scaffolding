// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.DotNet.Scaffolding.Core.Logging;

internal sealed class ScaffoldingErrorCapture : ILoggerProvider
{
    private readonly AsyncLocal<Capture?> _current = new();

    public Capture BeginCapture()
    {
        var capture = new Capture(this, _current.Value);
        _current.Value = capture;
        return capture;
    }

    public ILogger CreateLogger(string categoryName) => new CaptureLogger(this);

    public void Dispose()
    {
    }

    internal sealed class Capture(ScaffoldingErrorCapture owner, Capture? previous) : IDisposable
    {
        internal ConcurrentQueue<string> Messages { get; } = new();

        public string Message => string.Join(Environment.NewLine, Messages);

        public void Dispose() => owner._current.Value = previous;
    }

    private sealed class CaptureLogger(ScaffoldingErrorCapture owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => NullLogger.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error && logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel) && owner._current.Value is { } capture)
            {
                string message = formatter(state, exception);
                if (exception is not null)
                {
                    message = $"{message}{Environment.NewLine}{exception}";
                }
                capture.Messages.Enqueue(message);
            }
        }
    }
}
