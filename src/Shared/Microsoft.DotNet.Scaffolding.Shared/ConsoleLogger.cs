// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.DotNet.MSIdentity.Shared
{
    internal class ConsoleLogger : IConsoleLogger
    {
        private readonly bool _jsonOutput;
        private readonly bool _silent;

        private string CommandName { get; }

        public ConsoleLogger(string commandName = null, bool jsonOutput = false, bool silent = false)
        {
            CommandName = commandName ?? string.Empty;
            _jsonOutput = jsonOutput;
            _silent = silent;
            Console.OutputEncoding = Encoding.UTF8;
        }

        public void LogMessage(string message, LogMessageType level, bool removeNewLine = false)
        {
            //if json output is enabled, don't write to console at all.
            if (!_silent && !_jsonOutput)
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

        public void LogJsonMessage(string state = null, object content = null, string output = null)
        {
            if (!_silent)
            {
                if (_jsonOutput)
                {
                    var jsonMessage = new JsonResponse(CommandName, state, content, output);
                    Console.WriteLine(jsonMessage.ToJsonString());
                }
                else
                {
                    if (state == State.Fail)
                    {
                        LogMessage(output, LogMessageType.Error);
                    }
                    else
                    {
                        LogMessage(output);
                    }
                }
            }
        }

        public void LogMessage(string message, bool removeNewLine = false)
        {
            if (!_silent && !_jsonOutput)
            {
                LogMessage(message, LogMessageType.Information, removeNewLine);
            }
        }

        public void LogFailureAndExit(string failureMessage)
        {
            if (_jsonOutput)
            {
                LogJsonMessage(State.Fail, output: failureMessage);
            }
            else
            {
                LogMessage(failureMessage, LogMessageType.Error);
            }

            Environment.Exit(1);
        }
    }
}
