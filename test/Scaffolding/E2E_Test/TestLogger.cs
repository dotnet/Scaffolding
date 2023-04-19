// Copyright (c) .NET Foundation. All rights reserved.

using System;
using System.Text;
using Microsoft.DotNet.Scaffolding.Shared;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class TestLogger : ConsoleLogger
    {
        private ITestOutputHelper _output;

        public TestLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        public override void LogMessage(string message, LogMessageLevel level)
        {
            _output.WriteLine($"{level}:: {message}");
        }
    }
}
