// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
