// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.Scaffolding.Shared;

namespace Microsoft.Extensions.Internal
{
    internal class Command
    {
        private readonly Process _process;
        private bool _running = false;
        private Action<string> _stdErrorHandler;
        private Action<string> _stdOutHandler;
        private static readonly ConsoleLogger _logger = new ConsoleLogger ();

        internal static Command CreateDotNet(string commandName, IEnumerable<string> args)
        {
            return Create(DotNetMuxer.MuxerPathOrDefault(), new[] { commandName }.Concat(args));
        }

        internal static Command Create(string commandName, IEnumerable<string> args)
        {
            return new Command(commandName, args);
        }

        //TODO; fix to have a nullable Command and not resure scaffolding's.  Tracked https://github.com/dotnet/Scaffolding/issues/1549
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private Command(string commandName, IEnumerable<string> args)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            var psi = new ProcessStartInfo
            {
                FileName = commandName,
                Arguments = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            _process = new Process
            {
                StartInfo = psi
            };
        }

        public Command InWorkingDirectory(string workingDir)
        {
            if (string.IsNullOrEmpty(workingDir))
            {
                throw new ArgumentException(nameof(workingDir));
            }

            _process.StartInfo.WorkingDirectory = workingDir;
            return this;
        }

        public Command WithEnvironmentVariable(string name, string value)
        {
            _process.StartInfo.EnvironmentVariables[name] = value;
            return this;
        }

        public override string ToString()
        {
            var psi = _process .StartInfo;
            return $"{ psi .FileName } { psi .Arguments }";
        }

        public CommandResult Execute()
        {
            ThrowIfRunning();
            Command._logger.LogMessage(
                $"Executing external command:\n{ this }", LogMessageLevel .Trace
            );
            _running = true;
            _process.EnableRaisingEvents = true;

            _process.OutputDataReceived += OnOutputReceived;
            _process.ErrorDataReceived += OnErrorReceived;

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _process.WaitForExit();

            var exitCode = _process.ExitCode;

            _process.OutputDataReceived -= OnOutputReceived;
            _process.ErrorDataReceived -= OnErrorReceived;

            return new CommandResult(
                _process.StartInfo,
                exitCode);
        }

        private void OnErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                _stdErrorHandler?.Invoke(e.Data);
            }
        }

        private void OnOutputReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                _stdOutHandler?.Invoke(e.Data);
            }
        }

        public Command OnOutputLine(Action<string> handler)
        {
            ThrowIfRunning();
            _stdOutHandler = handler;
            return this;
        }

        public Command OnErrorLine(Action<string> handler)
        {
            ThrowIfRunning();
            _stdErrorHandler = handler;
            return this;
        }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        private void ThrowIfRunning([CallerMemberName] string memberName = null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        {
            if (_running)
            {
                throw new InvalidOperationException($"Unable to invoke {memberName} after the command has been run");
            }
        }
    }
}
