// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class TestUtilitiesTests
    {
        private readonly ITestOutputHelper _testOutput;

        public TestUtilitiesTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        [Fact]
        public void RunProcess_DrainsLargeRedirectedOutput()
        {
            string testFolder = Path.Combine(Path.GetTempPath(), nameof(TestUtilitiesTests), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testFolder);

            try
            {
                string messages = string.Concat(Enumerable.Repeat(
                    "<Message Text=\"This output verifies that redirected process streams are drained concurrently.\" Importance=\"High\" />",
                    2048));
                File.WriteAllText(
                    Path.Combine(testFolder, "LargeOutput.proj"),
                    $"<Project><Target Name=\"WriteLargeOutput\">{messages}</Target></Project>");

                TestUtilities.RunProcess(
                    _testOutput,
                    "dotnet msbuild LargeOutput.proj --target:WriteLargeOutput",
                    testFolder);
            }
            finally
            {
                Directory.Delete(testFolder, recursive: true);
            }
        }

        [Fact]
        public void RunProcess_TerminatesProcessAfterTimeout()
        {
            string testFolder = Path.Combine(Path.GetTempPath(), nameof(TestUtilitiesTests), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testFolder);

            try
            {
                File.WriteAllText(
                    Path.Combine(testFolder, "Timeout.csproj"),
                    "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>");
                File.WriteAllText(
                    Path.Combine(testFolder, "Program.cs"),
                    "using System;\nusing System.IO;\nusing System.Threading;\nFile.WriteAllText(args[0], Environment.ProcessId.ToString());\nThread.Sleep(Timeout.Infinite);");

                TestUtilities.RunProcess(
                    _testOutput,
                    "dotnet build Timeout.csproj --nologo",
                    testFolder);

                Exception exception = Record.Exception(() => TestUtilities.RunProcess(
                    _testOutput,
                    "dotnet bin/Debug/net11.0/Timeout.dll process.pid",
                    testFolder,
                    TimeSpan.FromSeconds(2)));

                Assert.NotNull(exception);
                Assert.Contains("timed out after", exception.Message);

                int processId = int.Parse(File.ReadAllText(Path.Combine(testFolder, "process.pid")));
                try
                {
                    using Process childProcess = Process.GetProcessById(processId);
                    Assert.True(childProcess.HasExited, $"Child process {processId} is still running after the timeout.");
                }
                catch (ArgumentException)
                {
                }
            }
            finally
            {
                Directory.Delete(testFolder, recursive: true);
            }
        }
    }
}