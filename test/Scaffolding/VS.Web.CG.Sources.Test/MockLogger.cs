// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Shared;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    internal class MockLogger : ILogger
    {
        private List<string> _errors = new List<string>();
        private List<string> _info = new List<string>();
        private List<string> _trace = new List<string>();

        public void LogMessage(string message, LogMessageType level)
        {
            switch (level)
            {
                case LogMessageType.Error:
                    _errors.Add(message);
                    break;
                case LogMessageType.Information:
                    _info.Add(message);
                    break;
                case LogMessageType.Trace:
                    _trace.Add(message);
                    break;
            }
        }

        public void LogMessage(string message)
        {
            LogMessage(message, LogMessageType.Information);
        }

        public void LogMessage(string message, LogMessageType level, bool removeNewLine = false)
        {
            LogMessage(message, level);
        }

        public void LogMessage(string message, bool removeNewLine = false)
        {
            LogMessage(message);
        }

        public void LogJsonMessage(string state = null, object content = null, string output = null)
        {
            //throw new System.NotImplementedException();
        }

        public void LogFailureAndExit(string failureMessage)
        {
            //throw new System.NotImplementedException();
        }

        public List<string> Errors => _errors;
        public List<string> Info => _info;
        public List<string> Trace => _trace;
    }
}
