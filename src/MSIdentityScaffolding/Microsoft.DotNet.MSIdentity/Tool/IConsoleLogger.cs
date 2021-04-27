using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.MSIdentity.Tool
{
    public interface IConsoleLogger
    {
        void LogMessage(string? message, LogMessageType level, bool removeNewLine = false);
        void LogMessage(string? message, bool removeNewLine = false);
        void LogJsonMessage(JsonResponse jsonMessage);
    }

    public enum LogMessageType
    {
        Error,
        Information
    }
}
