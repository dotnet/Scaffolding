// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.DotNet.Scaffolding.Shared
{
    public class ConsoleLogger : ILogger
    {
        private static bool isTrace = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("codegen_trace"));
        private object _syncObject = new object();

        public bool IsTracing => isTrace;

        public ConsoleLogger()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        public void LogMessage(string message)
        {
            LogMessage(message, LogMessageLevel.Information);
        }

        public virtual void LogMessage(string message, LogMessageLevel level)
        {
            lock (_syncObject)
            {
                if (level == LogMessageLevel.Error)
                {
                    Console.Error.WriteLine(message);
                }
                else if (level == LogMessageLevel.Trace && isTrace)
                {
                    Console.Out.WriteLine($"[Trace]: {message}");
                }
                else if (level == LogMessageLevel.Information)
                {
                    Console.Out.WriteLine(message);
                }
            }
        }
    }
}
