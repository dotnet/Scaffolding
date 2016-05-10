// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    //This needs to be perhaps internal.
    public class ConsoleLogger : ILogger
    {
        public void LogMessage(string message)
        {
            LogMessage(message, LogMessageLevel.Information);
        }

        public void LogMessage(string message, LogMessageLevel level)
        {
            if (level == LogMessageLevel.Error)
            {
                Reporter.Error.WriteLine(message);
            }
            else if (level == LogMessageLevel.Trace)
            {
                Reporter.Verbose.WriteLine(message);
            }
            else
            {
                Reporter.Output.WriteLine(message);
            }
        }
    }
}