// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    public class E2ETestBase
    {
#if RELEASE
        public const string Configuration = "Release";
#else
        public const string Configuration = "Debug";
#endif

        public const string E2ESkipReason = "Disabling E2E test";
        public const string codegeneratorToolName = "aspnet-codegenerator";

        protected string TestProjectPath { get; set; }
        protected ITestOutputHelper Output { get; set; }

        public E2ETestBase(ITestOutputHelper output)
        {
            Output = output;
        }

        protected void Scaffold(string[] args, string testProjectPath)
        {
            var muxerPath = DotNetMuxer.MuxerPathOrDefault();

            var invocationArgs = new[]
            {
                "aspnet-codegenerator"
            }.Concat(args);

            Output.WriteLine($"Executing {muxerPath} {string.Join(" ", invocationArgs)}");

            var exitCode = Command.Create(muxerPath, invocationArgs.Concat(new[] { "--no-build" }))
                .WithEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true")
                .OnOutputLine(l => Output.WriteLine(l))
                .OnErrorLine(l => Output.WriteLine(l))
                .Execute()
                .ExitCode;
        }

        protected void VerifyFileAndContent(string generatedFilePath, string baselineFile)
        {
            Output.WriteLine($"Checking if file is generated at {generatedFilePath}");
            Assert.True(File.Exists(generatedFilePath), $"file [{generatedFilePath}] does not exist");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: This is currently to fix the tests on Non windows machine.
                // The baseline files need to be converted to Unix line endings
                var assembly = GetType().GetTypeInfo().Assembly;
                using (var resourceStream = assembly.GetManifestResourceStream($"E2E_Test.compiler.resources.{baselineFile.Replace('\\', '.')}"))
                using (var reader = new StreamReader(resourceStream))
                {
                    var expectedContents = reader.ReadToEnd();
                    expectedContents = NormalizeLineEndings(expectedContents);
                    var actualContents = NormalizeLineEndings(File.ReadAllText(generatedFilePath));
                    Assert.Equal(expectedContents, actualContents);
                }
            }
            return;
        }

        private string NormalizeLineEndings(string expectedContents)
        {
            if (string.IsNullOrEmpty(expectedContents))
            {
                return expectedContents;
            }
            const string token = "___newline___";

            expectedContents = expectedContents.Replace(Environment.NewLine, token);
            expectedContents = expectedContents.Replace("\n", token);
            expectedContents = expectedContents.Replace("\r", token);
            return expectedContents.Replace(token, Environment.NewLine);
        }

        protected void VerifyFoldersCreated(string folderPath)
        {
            Output.WriteLine($"Verifying folder exists: {folderPath}");
            Assert.True(Directory.Exists(folderPath));
        }
    }
}
