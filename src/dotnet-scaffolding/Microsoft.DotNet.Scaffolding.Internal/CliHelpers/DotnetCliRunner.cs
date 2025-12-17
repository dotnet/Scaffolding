// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;

namespace Microsoft.DotNet.Scaffolding.Internal.CliHelpers;

/// <summary>
/// To run 'dotnet' or any processes and capture consequent stdout and stderr (using 'ExecuteAndCaptureOutput'
/// </summary>
internal class DotnetCliRunner
{
    private const string DotnetCommandName = "dotnet";

    public static DotnetCliRunner CreateDotNet(string commandName, IEnumerable<string> args, IDictionary<string, string>? environmentVariables = null)
    {
        return Create(DotnetCommandName, new[] { commandName }.Concat(args), environmentVariables);
    }

    public static DotnetCliRunner Create(string commandName, IEnumerable<string> args, IDictionary<string, string>? environmentVariables = null)
    {
        return new DotnetCliRunner(commandName, args, environmentVariables);
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

        try
        {
            process.Start();
        }
        catch (Exception e)
        {
            stdErrCallback(e.Message);
            return -1;
        }

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

        try
        {
            process.Start();
        }
        catch (Exception e)
        {
            stdOut = string.Empty;
            stdErr = e.Message;
            return -1;
        }

        var taskOut = outStream.BeginRead(process.StandardOutput);
        var taskErr = errStream.BeginRead(process.StandardError);

        process.WaitForExit();

        taskOut.Wait();
        taskErr.Wait();

        stdOut = outStream.CapturedOutput;
        stdErr = errStream.CapturedOutput;

        return process.ExitCode;
    }

    public static string FindAzureCLIPath()
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

            return "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    internal ProcessStartInfo _psi;
    private DotnetCliRunner(string commandName, IEnumerable<string> args, IDictionary<string, string>? environmentVariables = null)
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
        //TODO add the environment variables below using the 'environmentVariables' parameter.
        _psi.Environment.Remove("MSBuildSDKsPath");
        _psi.Environment.Remove("MSBuildExtensionsPath");
        _psi.Environment.Remove("MSBUILD_EXE_PATH");
        if (environmentVariables is not null && environmentVariables.Any())
        {
            foreach (var envVar in environmentVariables)
            {
                _psi.Environment.Add(envVar.Key, envVar.Value);
            }
        }
    }
}
