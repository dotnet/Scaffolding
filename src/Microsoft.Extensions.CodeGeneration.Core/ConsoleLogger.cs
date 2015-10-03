// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.CodeGeneration
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
            Console.WriteLine(message);
        }
    }
}