// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.DotNet.Scaffolding.Internal.CliHelpers;

/// <summary>
/// To run 'dotnet' or any processes and capture consequent stdout and stderr (using 'ExecuteAndCaptureOutput'
/// </summary>
internal class AzCliRunner
{
    public static AzCliRunner Create(string? commandName = null)
    {
        return new AzCliRunner(commandName);
    }

    public int RunAzCli(string arguments, out string? stdOut, out string? stdErr)
    {
        using var outStream = new ProcessOutputStreamReader();
        using var errStream = new ProcessOutputStreamReader();

        outStream.Capture();
        errStream.Capture();

        var psi = _psi;
        psi.Arguments = arguments;

        string errorMessage = string.Empty;
        string output = string.Empty;

        Process process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            stdOut = string.Empty;
            stdErr = ex.Message;
            return -1;
        }

        var taskOut = outStream.BeginRead(process.StandardOutput);
        var taskErr = errStream.BeginRead(process.StandardError);

        // Wait for process to exit with a timeout (e.g., 30 seconds)
        const int timeoutMilliseconds = 30000;
        bool exited = process.WaitForExit(timeoutMilliseconds);

        if (!exited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore exceptions from killing the process
            }
        }

        taskOut.Wait();
        taskErr.Wait();

        stdOut = outStream.CapturedOutput?.Trim();
        stdErr = errStream.CapturedOutput;

        return process.ExitCode;
    }

    internal ProcessStartInfo _psi;

    internal string _commandName;
    private AzCliRunner(string? commandName = null)
    {
        _commandName = string.IsNullOrEmpty(commandName) ? FindAzureCLIPath() : commandName;

        _psi = new ProcessStartInfo(_commandName)
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8
        };
    }

    private static string FindAzureCLIPath()
    {
        try
        {
            // Use "where" on Windows, "which" on Unix
            string whereCommand = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which";
            string searchArg = Environment.OSVersion.Platform == PlatformID.Win32NT ? "az.cmd" : "az";

            var runner = DotnetCliRunner.Create(whereCommand, new[] { searchArg });
            int exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out var _);

            if (exitCode == 0 && !string.IsNullOrEmpty(stdOut))
            {
                // Return the first line (first match)
                string[] paths = stdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (paths.Length > 0)
                {
                    return paths[0];
                }
            }

            return String.Empty;
        }
        catch (Exception)
        {
            return String.Empty;
        }
    }

}
