// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    internal static class TestUtilities
    {
        /// <summary>
        /// Create the test project
        /// </summary>
        /// <param name="testOutput">Output stream to write more information about the test</param>
        /// <param name="command">Command to run</param>
        /// <param name="folder">Folder in which to run the command</param>
        /// <param name="postFix">Additionnal command appended to the command</param>
        public static void RunProcess(ITestOutputHelper testOutput, string command, string folder, string postFix = "")
        {
            Directory.CreateDirectory(folder);
            ProcessStartInfo processStartInfo = new ProcessStartInfo("dotnet", command.Replace("dotnet ", string.Empty) + postFix);
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            Environment.GetEnvironmentVariables();
            processStartInfo.WorkingDirectory = folder;
            Process process = Process.Start(processStartInfo);
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            testOutput.WriteLine(output);
            string errors = process.StandardError.ReadToEnd();
            testOutput.WriteLine(errors);
            Assert.Equal(string.Empty, errors);
        }
    }
}
