// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.DotNet.Scaffolding.Shared
{
    public interface ILogger
    {
        void LogMessage(string message, LogMessageLevel level);

        void LogMessage(string message);
    }
}