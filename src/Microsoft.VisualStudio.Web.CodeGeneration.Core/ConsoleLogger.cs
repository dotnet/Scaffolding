// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

﻿using System;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
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
                Console.Error.WriteLine(message);
            }
            else if (level == LogMessageLevel.Trace)
            {
                Console.Out.WriteLine(message);
            }
            else
            {
                Console.Out.WriteLine(message);
            }
        }
    }
}
