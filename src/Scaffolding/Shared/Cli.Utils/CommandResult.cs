// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.Extensions.Internal
{
    public struct CommandResult
    {
        public static readonly CommandResult Empty = new CommandResult();
        public ProcessStartInfo StartInfo { get; }
        public int ExitCode { get; }

        public CommandResult(ProcessStartInfo startInfo, int exitCode)
        {
            StartInfo = startInfo;
            ExitCode = exitCode;
        }
    }
}