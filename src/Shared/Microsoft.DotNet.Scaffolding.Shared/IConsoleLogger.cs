namespace Microsoft.DotNet.MSIdentity.Shared
{
    public interface IConsoleLogger
    {
        void LogMessage(string message, LogMessageType level, bool removeNewLine = false);
        void LogMessage(string message, bool removeNewLine = false);
        void LogJsonMessage(JsonResponse jsonMessage);
        void LogFailure(string failureMessage, string commandName = null);
    }

    public enum LogMessageType
    {
        Error,
        Information
    }
}
