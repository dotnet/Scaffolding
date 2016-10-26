// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration.E2E_Test
{
    public class E2ETestBase
    {
        public static string BaseLineFilesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Baseline");

        public const string E2ESkipReason = "Disabling E2E test";
        public  const string codegeneratorToolName = "aspnet-codegenerator";

        protected string TestProjectPath { get; set; }
        protected ITestOutputHelper Output { get; set; }
        

        public E2ETestBase(ITestOutputHelper output)
        {
            Output = output;
        }

        protected void Scaffold(string[] args)
        {
            new CommandFactory()
                .Create("dotnet", args)
                .ForwardStdOut()
                .ForwardStdErr()
                .Execute();
        }

        protected void VerifyFileAndContent(string generatedFilePath, string baselineFile)
        {
            Console.WriteLine($"Checking if file is generated at {generatedFilePath}");
            Assert.True(File.Exists(generatedFilePath));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: This is currently to fix the tests on Non windows machine. 
                // The baseline files need to be converted to Unix line endings
                var expectedContents = File.ReadAllText(Path.Combine(BaseLineFilesDirectory, baselineFile));
                var actualContents = File.ReadAllText(generatedFilePath);
                Assert.Equal(expectedContents, actualContents);
            }
            return;
        }

        protected void VerifyFoldersCreated(string folderPath)
        {
            Console.WriteLine($"Verifying folder exists: {folderPath}");
            Assert.True(Directory.Exists(folderPath));
        }
    }
}
