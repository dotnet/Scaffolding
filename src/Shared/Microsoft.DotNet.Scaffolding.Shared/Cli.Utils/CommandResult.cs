// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
