// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class TestLogger : ILogger
    {
        private ITestOutputHelper _output;

        public TestLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        public void LogFailureAndExit(string failureMessage)
        {
            throw new System.NotImplementedException();
        }

        public void LogJsonMessage(string state = null, object content = null, string output = null)
        {
            throw new System.NotImplementedException();
        }

        public void LogMessage(string message, LogMessageType level, bool removeNewLine = false)
        {
            _output.WriteLine($"{level}:: {message}");
        }

        public void LogMessage(string message, bool removeNewLine = false)
        {
            throw new System.NotImplementedException();
        }
    }
}
