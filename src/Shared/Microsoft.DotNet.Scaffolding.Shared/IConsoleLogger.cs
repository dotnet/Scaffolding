namespace Microsoft.DotNet.MSIdentity.Shared
{
    public interface IConsoleLogger
    {
        void LogMessage(string message, LogMessageType level, bool removeNewLine = false);
        void LogMessage(string message, bool removeNewLine = false);
        void LogJsonMessage(string state = null, object content = null, string output = null);
        void LogFailureAndExit(string failureMessage);
    }

    public enum LogMessageType
    {
        Error,
        Information
    }
}
