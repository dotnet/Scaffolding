using System;
using System.Text;

namespace Microsoft.DotNet.MSIdentity.Shared
{
    internal class ConsoleLogger : IConsoleLogger
    {
        private bool _jsonOutput;
        private bool _silent;

        public ConsoleLogger(bool jsonOutput = false, bool silent = false)
        {
            _jsonOutput = jsonOutput;
            _silent = silent;
            Console.OutputEncoding = Encoding.UTF8;
        }

        public void LogMessage(string message, LogMessageType level, bool removeNewLine = false)
        {
            if (_silent)
            {
                return;
            }

            //if json output is enabled, don't write to console at all.
            if (!_jsonOutput)
            {
                switch (level)
                {
                    case LogMessageType.Error:
                        if (removeNewLine)
                        {
                            Console.Error.Write(message);
                        }
                        else
                        {
                            Console.Error.WriteLine(message);
                        }
                        break;
                    case LogMessageType.Information:
                        if (removeNewLine)
                        {
                            Console.Write(message);
                        }
                        else
                        {
                            Console.WriteLine(message);
                        }
                        break;
                }
            }
        }

        public void LogJsonMessage(JsonResponse jsonMessage)
        {
            if (_silent)
            {
                return;
            }

            if (_jsonOutput)
            {
                Console.WriteLine(jsonMessage.ToJsonString());
            }
        }

        public void LogMessage(string message, bool removeNewLine = false)
        {
            if (_silent)
            {
                return;
            }

            LogMessage(message, LogMessageType.Information, removeNewLine);
        }
    }
}
