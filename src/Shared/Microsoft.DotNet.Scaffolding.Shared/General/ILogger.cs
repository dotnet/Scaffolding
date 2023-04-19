// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.Scaffolding.Shared
{
    public interface ILogger
    {
        void LogMessage(string message, LogMessageLevel level);

        void LogMessage(string message);
    }
}