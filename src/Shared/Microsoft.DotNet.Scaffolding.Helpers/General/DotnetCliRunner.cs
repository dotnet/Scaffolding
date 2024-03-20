// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;

namespace Microsoft.DotNet.Scaffolding.Helpers.General
{
    public class DotnetCliRunner
    {
        public static DotnetCliRunner CreateDotNet(string commandName, IEnumerable<string> args)
        {
            return Create(DotNetMuxer.MuxerPathOrDefault(), new[] { commandName }.Concat(args));
        }

        public static DotnetCliRunner Create(string commandName, IEnumerable<string> args)
        {
            return new DotnetCliRunner(commandName, args);
        }

        public int ExecuteAndCaptureOutput(out string? stdOut, out string? stdErr)
        {
            using var outStream = new ProcessOutputStreamReader();
            using var errStream = new ProcessOutputStreamReader();

            outStream.Capture();
            errStream.Capture();

            _psi.RedirectStandardOutput = true;
            _psi.RedirectStandardError = true;

            using var process = new Process
            {
                StartInfo = _psi
            };

            process.EnableRaisingEvents = true;

            process.Start();

            var taskOut = outStream.BeginRead(process.StandardOutput);
            var taskErr = errStream.BeginRead(process.StandardError);

            process.WaitForExit();

            taskOut.Wait();
            taskErr.Wait();

            stdOut = outStream.CapturedOutput;
            stdErr = errStream.CapturedOutput;

            return process.ExitCode;
        }

        internal ProcessStartInfo _psi;
        private DotnetCliRunner(string commandName, IEnumerable<string> args)
        {
            _psi = new ProcessStartInfo
            {
                FileName = commandName,
                Arguments = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args),
                UseShellExecute = false,
            };
        }
    }
}
