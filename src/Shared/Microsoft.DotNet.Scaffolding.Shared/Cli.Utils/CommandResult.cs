// Copyright (c) .NET Foundation. All rights reserved.

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
