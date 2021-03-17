// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Sources.Test
{
    internal class MockLogger : ILogger
    {
        private List<string> _warnings = new List<string>();
        private List<string> _errors = new List<string>();
        private List<string> _info = new List<string>();
        private List<string> _trace = new List<string>();

        public void LogMessage(string message, LogMessageLevel level)
        {
            switch (level)
            {
                case LogMessageLevel.Error:
                    _errors.Add(message);
                    break;
                case LogMessageLevel.Information:
                    _info.Add(message);
                    break;
                case LogMessageLevel.Trace:
                    _trace.Add(message);
                    break;
                case LogMessageLevel.Warning:
                    _warnings.Add(message);
                    break;
            }
        }

        public void LogMessage(string message)
        {
            LogMessage(message, LogMessageLevel.Information);
        }

        public List<string> Warnings => _warnings;
        public List<string> Errors => _errors;
        public List<string> Info => _info;
        public List<string> Trace => _trace;
    }
}
