// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        static E2ETestBase()
        {
            MsBuildProjectSetupHelper.InstallGlobalTool();
        }

        protected void Scaffold(string[] args, string testProjectPath)
        {
            var thisAssembly = GetType().GetTypeInfo().Assembly.GetName().Name;
            var muxerPath = DotNetMuxer.MuxerPathOrDefault();
            Output.WriteLine($"Executing {muxerPath} {string.Join(" ", args)}");
            var exitCode = Command.Create(muxerPath, args.Concat(new [] {"--no-build"}))
                .WithEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true")
                .InWorkingDirectory(testProjectPath)
                .OnOutputLine(l => Output.WriteLine(l))
                .OnErrorLine(l => Output.WriteLine(l))
                .Execute()
                .ExitCode;

            Assert.True(0 == exitCode, $"Scaffold command failed with exit code {exitCode}");
        }

        protected void VerifyFileAndContent(string generatedFilePath, string baselineFile)
        {
            Output.WriteLine($"Checking if file is generated at {generatedFilePath}");
            Assert.True(File.Exists(generatedFilePath));
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
