// Copyright (c) .NET Foundation. All rights reserved.

using System;

namespace Microsoft.DotNet.Scaffolding.Shared
{
    public interface ILogger
    {
        void LogMessage(string message, LogMessageLevel level);

        void LogMessage(string message);
    }
}
