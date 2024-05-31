// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

internal interface ILogger
{
    void LogMessage(string message, LogMessageType level, bool removeNewLine = false);
    void LogMessage(string message, bool removeNewLine = false);
    void LogJsonMessage(string? state = null, object? content = null, string? output = null);
    void LogFailureAndExit(string failureMessage);
}

internal enum LogMessageType
{
    Error,
    Information,
    Trace
}
