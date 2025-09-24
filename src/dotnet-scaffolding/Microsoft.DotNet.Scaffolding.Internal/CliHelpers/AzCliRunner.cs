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
    public static async Task<AzCliRunner> CreateAsync(string? commandName = null)
    {
        string resolvedCommandName = string.IsNullOrEmpty(commandName) ? await FindAzureCLIPathAsync() : commandName!;
        return new AzCliRunner(resolvedCommandName);
    }

    public static AzCliRunner Create(string? commandName = null)
    {
        // Synchronous fallback, does not resolve CLI path
        return new AzCliRunner(commandName);
    }

    public async Task<(int errorCode, string? stdOut, string? stdErr)> RunAzCliAsync(string arguments)
    {
        string? stdOut = null;
        string? stdErr = null;

        using var outStream = new ProcessOutputStreamReader();
        using var errStream = new ProcessOutputStreamReader();

        outStream.Capture();
        errStream.Capture();

        var psi = _psi;
        psi?.Arguments = arguments;

        string errorMessage = string.Empty;
        string output = string.Empty;

        Process process = new Process
        {
            StartInfo = psi!,
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
            return (-1, stdOut, stdErr);
        }

        Task taskOut = outStream.BeginRead(process.StandardOutput);
        Task taskErr = errStream.BeginRead(process.StandardError);

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

        await Task.WhenAll(taskOut, taskErr);

        stdOut = outStream.CapturedOutput?.Trim();
        stdErr = errStream.CapturedOutput;

        return (process.ExitCode, stdOut, stdErr);
    }


    /// <summary>
    /// Executes a command that requires user interaction (like 'az login')
    /// </summary>
    /// <returns>Exit code of the process</returns>
    public int ExecuteInteractive()
    {
        // For interactive processes, we shouldn't redirect standard input/output
        _psi?.RedirectStandardInput = false;
        _psi?.RedirectStandardOutput = false;
        _psi?.RedirectStandardError = false;

        // Use shell execute to allow browser windows to be launched
        _psi?.UseShellExecute = true;

        using var process = new Process
        {
            StartInfo = _psi!
        };

        try
        {
            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception)
        {
            return -1;
        }
    }


    internal ProcessStartInfo? _psi = null;
    internal string _commandName = string.Empty;
    private AzCliRunner(string? commandName = null)
    {
        _commandName = commandName ?? string.Empty;
        _psi = new ProcessStartInfo(_commandName)
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8
        };
    }

    private static async Task<string> FindAzureCLIPathAsync()
    {
        try
        {
            // Use "where" on Windows, "which" on Unix
            string whereCommand = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which";
            string searchArg = Environment.OSVersion.Platform == PlatformID.Win32NT ? "az.cmd" : "az";

            var runner = DotnetCliRunner.Create(whereCommand, new[] { searchArg });
            (int exitCode, string? stdOut, _) = await runner.ExecuteAndCaptureOutputAsync();

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
