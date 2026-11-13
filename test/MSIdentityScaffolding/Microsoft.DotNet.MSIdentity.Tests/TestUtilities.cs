// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    internal static class TestUtilities
    {
        private static readonly TimeSpan ProcessTimeout = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Create the test project
        /// </summary>
        /// <param name="testOutput">Output stream to write more information about the test</param>
        /// <param name="command">Command to run</param>
        /// <param name="folder">Folder in which to run the command</param>
        /// <param name="postFix">Additionnal command appended to the command</param>
        public static void RunProcess(ITestOutputHelper testOutput, string command, string folder, string postFix = "")
        {
            RunProcess(testOutput, command, folder, ProcessTimeout, postFix);
        }

        internal static void RunProcess(ITestOutputHelper testOutput, string command, string folder, TimeSpan timeout, string postFix = "")
        {
            Directory.CreateDirectory(folder);
            ProcessStartInfo processStartInfo = new ProcessStartInfo("dotnet", command.Replace("dotnet ", string.Empty) + postFix);
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            Environment.GetEnvironmentVariables();
            processStartInfo.WorkingDirectory = folder;
            using Process process = Process.Start(processStartInfo);
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            bool exited = process.WaitForExit((int)timeout.TotalMilliseconds);
            if (!exited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException) when (process.HasExited)
                {
                }

                process.WaitForExit();
            }

            string output = outputTask.GetAwaiter().GetResult();
            testOutput.WriteLine(output);
            string errors = errorTask.GetAwaiter().GetResult();
            testOutput.WriteLine(errors);
            Assert.True(
                exited,
                $"'{command + postFix}' timed out after {timeout}.{Environment.NewLine}{errors}");
            Assert.True(
                process.ExitCode == 0,
                $"'{command + postFix}' exited with code {process.ExitCode}.{Environment.NewLine}{errors}");
        }
    }
}
