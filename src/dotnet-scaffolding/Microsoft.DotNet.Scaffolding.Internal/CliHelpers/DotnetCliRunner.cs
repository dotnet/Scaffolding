// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;

namespace Microsoft.DotNet.Scaffolding.Internal.CliHelpers;

/// <summary>
/// To run 'dotnet' or any processes and capture consequent stdout and stderr (using 'ExecuteAndCaptureOutput'
/// </summary>
internal class DotnetCliRunner
{
    public static DotnetCliRunner CreateDotNet(string commandName, IEnumerable<string> args)
    {
        return Create("dotnet", new[] { commandName }.Concat(args));
    }

    public static DotnetCliRunner Create(string commandName, IEnumerable<string> args)
    {
        return new DotnetCliRunner(commandName, args);
    }

    public int ExecuteWithCallbacks(Action<string> stdOutCallback, Action<string> stdErrCallback)
    {
        using var process = new Process()
        {
            StartInfo = _psi,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                stdOutCallback(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                stdErrCallback(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return process.ExitCode;
    }

    public int ExecuteAndCaptureOutput(out string? stdOut, out string? stdErr)
    {
        using var outStream = new ProcessOutputStreamReader();
        using var errStream = new ProcessOutputStreamReader();

        outStream.Capture();
        errStream.Capture();

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
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        // Clear MSBuild related environment variables so it doesn't interfere with dotnet calls
        // automatic lookups via global.json, etc.
        _psi.Environment.Remove("MSBuildSDKsPath");
        _psi.Environment.Remove("MSBuildExtensionsPath");
        _psi.Environment.Remove("MSBUILD_EXE_PATH");
    }
}
